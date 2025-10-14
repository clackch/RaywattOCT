using log4net;
using RaywattApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace RaywattApp.Services
{
    public class UsbDetectionService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(UsbDetectionService));

        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        private const int DBT_DEVTYP_VOLUME = 0x00000002;

        public event Action DeviceArrived;
        public event Action DeviceRemoved;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr RegisterDeviceNotification(IntPtr recipient, IntPtr notificationFilter, int flags);

        [StructLayout(LayoutKind.Sequential)]
        private struct DEV_BROADCAST_DEVICEINTERFACE
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
        }

        public UsbDetectionService()
        {
            _log.Debug("UsbDetectionService");
        }

        public void RegisterForDeviceNotification(IntPtr windowHandle)
        {
            _log.Debug("Registering for USB device notifications");

            // USB 장치 알림을 위한 필터 설정
            DEV_BROADCAST_DEVICEINTERFACE deviceInterface = new DEV_BROADCAST_DEVICEINTERFACE
            {
                dbcc_size = Marshal.SizeOf(typeof(DEV_BROADCAST_DEVICEINTERFACE)),
                dbcc_devicetype = DBT_DEVTYP_VOLUME,
                dbcc_reserved = 0
            };

            IntPtr buffer = Marshal.AllocHGlobal(deviceInterface.dbcc_size);
            Marshal.StructureToPtr(deviceInterface, buffer, true);

            IntPtr result = RegisterDeviceNotification(windowHandle, buffer, 0);

            if (result != IntPtr.Zero)
            {
                _log.Debug("USB device notification registration successful");
            }
            else
            {
                _log.Error("Failed to register for USB device notifications");
            }

            Marshal.FreeHGlobal(buffer);
        }

        public void ProcessWindowMessage(int msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_DEVICECHANGE)
            {
                switch (wParam.ToInt32())
                {
                    case DBT_DEVICEARRIVAL:
                        _log.Debug("USB Drive detected - Device arrival event");
                        DeviceArrived?.Invoke();
                        break;
                    case DBT_DEVICEREMOVECOMPLETE:
                        _log.Debug("USB Drive removed - Device removal event");
                        DeviceRemoved?.Invoke();
                        break;
                }
            }
        }

        public List<UpdateItem> GetUsbFirmwareItems()
        {
            try
            {
                // USB 드라이브 찾기
                var usbDrives = DriveInfo.GetDrives()
                    .Where(drive => drive.DriveType == DriveType.Removable && drive.IsReady)
                    .ToList();

                _log.Debug($"Found {usbDrives.Count} USB drives");

                if (!usbDrives.Any())
                {
                    return new List<UpdateItem>();
                }

                // TODO[haeun]: 여러 개 USB 드라이브 중에서 선택하도록 수정
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

                        // 폴더 안에 .bin 파일이 있는지 확인
                        var binFiles = Directory.GetFiles(dir, "*.bin");

                        if (binFiles.Length == 0)
                        {
                            _log.Warn($"No .bin files found in {dirName}, skipping");
                            continue;
                        }

                        // UpdateItem 생성 (폴더명을 버전으로 사용)
                        firmwareItems.Add(new UpdateItem(
                            dirName,                          // version (폴더명)
                            dir,                              // filePath (버전 폴더 경로)
                            Directory.GetLastWriteTime(dir)   // lastModified
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
    }
}