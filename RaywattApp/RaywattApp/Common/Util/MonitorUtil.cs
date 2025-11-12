using log4net;
using RaywattApp.ViewModels.Setting;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace RaywattApp.Common.Util
{
    /// <summary>
    /// 모니터 개수 변화를 폴링해서
    /// 2대 이상이 되면 DisplaySwitch /clone + 해상도 맞추기를 수행하는 유틸.
    /// 외부에서 Start/Stop/AdjustDisplayMode()를 호출해서 사용.
    /// </summary>
    public static class MonitorUtil
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingMaintenanceViewModel));

        // ===== 설정값 =====
        const int ADJUSTMENT_STABILIZE_DELAY_MS = 0;   // 모니터 변경 감지 후 DisplaySwitch 실행 전 대기
        const int AFTER_CLONE_SETTLE_DELAY_MS = 500;     // /clone 후 드라이버가 정리하는 시간
        const int POLLING_INTERVAL_MS = 1000;             // 모니터 수 체크 주기

        static volatile bool _running = false;
        static Thread _pollingThread;
        static readonly object _syncRoot = new object();

        // ===== WinAPI 상수 =====
        const int SM_CMONITORS = 80;
        const int SM_CXSCREEN = 0;
        const int SM_CYSCREEN = 1;

        const int ENUM_CURRENT_SETTINGS = -1;
        const int EDS_NONE = 0;

        const int CDS_UPDATEREGISTRY = 0x00000001;

        const int DISP_CHANGE_SUCCESSFUL = 0;

        const int DM_PELSWIDTH = 0x00080000;
        const int DM_PELSHEIGHT = 0x00100000;

        [Flags]
        enum DisplayDeviceStateFlags : int
        {
            AttachedToDesktop = 0x00000001,
            // 필요하면 추가
        }

        // ===== 구조체/WinAPI 선언 =====

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct DISPLAY_DEVICE
        {
            public int cb;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;

            public DisplayDeviceStateFlags StateFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct DEVMODE
        {
            private const int CCHDEVICENAME = 32;
            private const int CCHFORMNAME = 32;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;

            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;

            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;

            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern bool EnumDisplayDevices(
            string lpDevice, int iDevNum,
            ref DISPLAY_DEVICE lpDisplayDevice, int dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern bool EnumDisplaySettingsEx(
            string lpszDeviceName,
            int iModeNum,
            ref DEVMODE lpDevMode,
            int dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int ChangeDisplaySettingsEx(
            string lpszDeviceName,
            ref DEVMODE lpDevMode,
            IntPtr hwnd,
            int dwflags,
            IntPtr lParam);

        //public static void StartMonitoring()
        //{
        //    lock (_syncRoot)
        //    {
        //        if (_running)
        //            return;

        //        _running = true;
        //        _pollingThread = new Thread(PollingThread)
        //        {
        //            IsBackground = true,
        //            Name = "MonitorAutoClonePolling"
        //        };
        //        _pollingThread.Start();
        //    }
        //}

        //public static void StopMonitoring()
        //{
        //    lock (_syncRoot)
        //    {
        //        if (!_running)
        //            return;

        //        _running = false;
        //    }

        //    try
        //    {
        //        _pollingThread?.Join(2000);
        //    }
        //    catch { /* 무시 */ }
        //}

        /// 현재 상태에서 한 번만 /clone + 해상도 맞추기 수행.
        public static void AdjustOnce()
        {
            AdjustDisplayMode();
        }

        /// 현재 모니터 개수 반환.
        public static int GetMonitorCountPublic() => GetMonitorCount();

        //static void PollingThread()
        //{
        //    int previousCount = GetMonitorCount();
        //    Console.WriteLine($"Initial monitor count: {previousCount}");

        //    AdjustDisplayMode();

        //    while (_running)
        //    {
        //        Thread.Sleep(POLLING_INTERVAL_MS);

        //        int currentCount = GetMonitorCount();
        //        if (currentCount != previousCount)
        //        {
        //            Console.WriteLine($"Monitor count changed {previousCount} -> {currentCount}");
        //            AdjustDisplayMode();
        //            previousCount = currentCount;
        //        }
        //    }

        //    Console.WriteLine("Polling thread terminated.");
        //}

        // 모니터 개수
        static int GetMonitorCount()
        {
            return GetSystemMetrics(SM_CMONITORS);
        }

        // 주 모니터 해상도
        static void GetPrimaryMonitorResolution(out int width, out int height)
        {
            width = GetSystemMetrics(SM_CXSCREEN);
            height = GetSystemMetrics(SM_CYSCREEN);
            _log.Debug($"Primary resolution: {width}x{height}");
        }

        // 모든 활성 모니터를 target 해상도로 시도
        public static void SetAllMonitorsResolution(int targetWidth, int targetHeight)
        {
            _log.Debug($"SetAllMonitorsResolution: {targetWidth}x{targetHeight}");

            int devNum = 0;
            DISPLAY_DEVICE dd = new DISPLAY_DEVICE();
            dd.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));

            while (EnumDisplayDevices(null, devNum, ref dd, 0))
            {
                if ((dd.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0)
                {
                    DEVMODE dm = new DEVMODE();
                    dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));

                    if (EnumDisplaySettingsEx(dd.DeviceName, ENUM_CURRENT_SETTINGS, ref dm, EDS_NONE))
                    {
                        if (dm.dmPelsWidth == targetWidth && dm.dmPelsHeight == targetHeight)
                        {
                            _log.Debug($"  {dd.DeviceName}: already {targetWidth}x{targetHeight}");
                        }
                        else
                        {
                            _log.Debug($"  {dd.DeviceName}: {dm.dmPelsWidth}x{dm.dmPelsHeight} -> {targetWidth}x{targetHeight}");

                            dm.dmPelsWidth = targetWidth;
                            dm.dmPelsHeight = targetHeight;
                            dm.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT;

                            int result = ChangeDisplaySettingsEx(dd.DeviceName, ref dm, IntPtr.Zero, CDS_UPDATEREGISTRY, IntPtr.Zero);

                            if (result == DISP_CHANGE_SUCCESSFUL)
                            {
                                _log.Debug($"    SUCCESS");
                            }
                            else
                            {
                                _log.Debug($"    FAILED: {result}");
                            }
                        }
                    }
                    else
                    {
                        _log.Debug($"  {dd.DeviceName}: EnumDisplaySettingsEx failed");
                    }
                }

                devNum++;
                dd = new DISPLAY_DEVICE();
                dd.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));
            }
        }

        // DisplaySwitch 호출
        public static void ExecuteDisplaySwitch(string argument)
        {
            try
            {
                string path = @"C:\Windows\System32\DisplaySwitch.exe";
                var psi = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = argument,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                                
                using (var p = Process.Start(psi))
                {
                    p?.WaitForExit();
                    if (p != null)
                        _log.Debug($"  ExitCode: {p.ExitCode}");
                }
            }
            catch (Exception ex)
            {
                _log.Debug($"DisplaySwitch failed: {ex.Message}");
            }
        }

        // 실제 동작 본체 (/clone + 해상도 맞추기)
        public static void AdjustDisplayMode()
        {
            // 모니터 붙었다/빠졌다 직후 안정화 대기
            Thread.Sleep(ADJUSTMENT_STABILIZE_DELAY_MS);

            // 1) 현재 주 모니터 해상도 확인
            GetPrimaryMonitorResolution(out int w, out int h);            

            int count = GetMonitorCount();
            _log.Debug($"[AdjustDisplayMode] monitor count: {count}");

            if (count < 2)
            {
                _log.Debug("  < 2 monitors, skip clone.");
                return;
            }
            ExecuteDisplaySwitch("/extend");
            SetAllMonitorsResolution(w, h);
            Thread.Sleep(5000);
            // 2) /clone 으로 복제 모드 전환
            _log.Debug("  Switching to clone (/clone)...");
            ExecuteDisplaySwitch("/clone");

            // 3) 주모니터의 해상도 전체 모니터에 적용
            SetAllMonitorsResolution(w, h);

            // 4) 드라이버/OS가 최적화 끝내도록 추가 대기
            Thread.Sleep(AFTER_CLONE_SETTLE_DELAY_MS);

            _log.Debug("  AdjustDisplayMode finished.\n");
        }
    }
}
