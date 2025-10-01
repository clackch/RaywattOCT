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
            _log.Debug("UsbDetectionService initialized");
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

        public List<UpdateItem> GetUsbUpdateItems()
        {
            try
            {
                _log.Debug("GetUsbUpdateItems");

                // 사용 가능한 드라이브 중 USB 드라이브 찾기
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

                var directories = usbDrive.RootDirectory.EnumerateDirectories();

                var directoryItems = new List<UpdateItem>();
                foreach (var directory in directories)
                {
                    try
                    {
                        _log.Debug($"Directory found: {directory.FullName}");

                        // 폴더 내 파일 개수 계산 (선택사항)
                        int fileCount = 0;
                        try
                        {
                            fileCount = Directory.GetFiles(directory.FullName, "*", SearchOption.AllDirectories).Length;
                        }
                        catch (Exception ex)
                        {
                            _log.Warn($"Cannot count files in directory {directory.Name}: {ex.Message}");
                        }

                        directoryItems.Add(new UpdateItem(
                            directory.Name,           // moduleAndVersion
                            directory.Name,           // name
                            directory.FullName,       // filePath
                            directory.LastWriteTime   // lastModified
                        ));
                    }
                    catch (Exception ex)
                    {
                        _log.Warn($"Error processing directory {directory.Name}: {ex.Message}");
                    }
                }

                _log.Debug($"Found {directoryItems.Count} directories in USB drive");
                return directoryItems;
            }
            catch (Exception ex)
            {
                _log.Error($"Error getting USB directories: {ex.Message}", ex);
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