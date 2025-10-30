using log4net;
using RaywattApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Interop;

namespace RaywattApp.Services
{
    public class UsbDetectionService : IDisposable
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(UsbDetectionService));

        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;

        private IntPtr _windowHandle;
        private HwndSource _hwndSource;
        private bool _disposed = false;

        public event Action DeviceArrived;
        public event Action DeviceRemoved;

        public UsbDetectionService()
        {
            _log.Debug("UsbDetectionService");
        }

        public void RegisterUsbDetection()
        {
            try
            {
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    var windowHelper = new WindowInteropHelper(mainWindow);
                    _windowHandle = windowHelper.Handle;

                    if (_windowHandle != IntPtr.Zero)
                    {
                        _hwndSource = HwndSource.FromHwnd(_windowHandle);
                        if (_hwndSource != null)
                        {
                            _hwndSource.AddHook(WndProc);
                            _log.Debug("USB device notification registered successfully");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error registering USB detection: {ex.Message}", ex);
            }
        }

        public void UnregisterUsbDetection()
        {
            try
            {
                if (_hwndSource != null)
                {
                    _hwndSource.RemoveHook(WndProc);
                    _hwndSource = null;
                    _log.Debug("USB device notification unregistered");
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error unregistering USB detection: {ex.Message}", ex);
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            ProcessWindowMessage(msg, wParam, lParam);
            return IntPtr.Zero;
        }

        private void ProcessWindowMessage(int msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_DEVICECHANGE)
            {
                switch (wParam.ToInt32())
                {
                    case DBT_DEVICEARRIVAL:
                        _log.Debug("USB Drive detected - Device arrival event");
                        OnDeviceArrived();
                        break;
                    case DBT_DEVICEREMOVECOMPLETE:
                        _log.Debug("USB Drive removed - Device removal event");
                        OnDeviceRemoved();
                        break;
                }
            }
        }

        private void OnDeviceArrived()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                DeviceArrived?.Invoke();
            });
        }

        private void OnDeviceRemoved()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                DeviceRemoved?.Invoke();
            });
        }

        public List<UpdateItem> GetUsbFirmwareItems()
        {
            try
            {
                var usbDrives = DriveInfo.GetDrives()
                    .Where(drive => drive.DriveType == DriveType.Removable && drive.IsReady)
                    .ToList();

                _log.Debug($"Found {usbDrives.Count} USB drives");

                if (!usbDrives.Any())
                {
                    return new List<UpdateItem>();
                }

                var usbDrive = usbDrives.First();
                _log.Debug($"Using USB drive: {usbDrive.Name}");

                string firmwarePath = Path.Combine(usbDrive.RootDirectory.FullName, "Firmware");

                if (!Directory.Exists(firmwarePath))
                {
                    _log.Warn($"Firmware folder not found at: {firmwarePath}");
                    return new List<UpdateItem>();
                }

                _log.Debug($"Firmware folder found at: {firmwarePath}");

                var firmwareItems = new List<UpdateItem>();
                var directories = Directory.GetDirectories(firmwarePath);

                foreach (var dir in directories)
                {
                    try
                    {
                        string dirName = Path.GetFileName(dir);
                        var binFiles = Directory.GetFiles(dir, "*.bin");

                        if (binFiles.Length == 0)
                        {
                            _log.Warn($"No .bin files found in {dirName}, skipping");
                            continue;
                        }

                        // 첫 번째 .bin 파일의 전체 경로 사용
                        string binFilePath = binFiles[0];
                        _log.Debug($"Found firmware binary: {binFilePath}");

                        firmwareItems.Add(new UpdateItem(
                            dirName,
                            binFilePath,
                            File.GetLastWriteTime(binFilePath)
                        ));
                    }
                    catch (Exception ex)
                    {
                        _log.Warn($"Error processing directory {dir}: {ex.Message}");
                    }
                }

                _log.Debug($"Total firmware items found: {firmwareItems.Count}");
                return firmwareItems;
            }
            catch (Exception ex)
            {
                _log.Error($"Error getting USB firmware items: {ex.Message}", ex);
                return new List<UpdateItem>();
            }
        }

        public string GetUsbDriveName()
        {
            try
            {
                var usbDrives = DriveInfo.GetDrives()
                    .Where(drive => drive.DriveType == DriveType.Removable && drive.IsReady)
                    .ToList();

                string driveName = usbDrives.FirstOrDefault()?.Name ?? "";
                _log.Debug($"USB drive name: {driveName}");
                return driveName;
            }
            catch (Exception ex)
            {
                _log.Error($"Error getting USB drive name: {ex.Message}", ex);
                return "";
            }
        }

        public bool HasUsbDrive()
        {
            try
            {
                var usbDrives = DriveInfo.GetDrives()
                    .Where(drive => drive.DriveType == DriveType.Removable && drive.IsReady)
                    .ToList();

                bool hasUsb = usbDrives.Any();
                _log.Debug($"Has USB drive: {hasUsb}");
                return hasUsb;
            }
            catch (Exception ex)
            {
                _log.Error($"Error checking for USB drive: {ex.Message}", ex);
                return false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    UnregisterUsbDetection();
                }
                _disposed = true;
            }
        }

        ~UsbDetectionService()
        {
            Dispose(false);
        }
    }
}