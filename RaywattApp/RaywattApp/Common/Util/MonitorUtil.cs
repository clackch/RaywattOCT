using log4net;
using RaywattApp.ViewModels.Setting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace RaywattApp.Common.Util
{
    /// <summary>
    /// 모니터 개수 변화를 폴링해서
    /// 2대 이상이 되면 DisplaySwitch /clone + 해상도 맞추기를 수행하는 유틸.
    /// 외부에서 AdjustOnce()/AdjustDisplayMode()를 호출해서 사용.
    /// </summary>
    public static class MonitorUtil
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingMaintenanceViewModel));

        // ===== 설정값 =====
        const int ADJUSTMENT_STABILIZE_DELAY_MS = 0;     // 모니터 변경 감지 후 DisplaySwitch 실행 전 대기
        const int AFTER_INTERNAL_SETTLE_DELAY_MS = 3000;  // /internal 후 레지스트리/드라이버 정리 시간
        const int AFTER_CLONE_SETTLE_DELAY_MS = 3000;     // /clone 후 드라이버가 정리하는 시간
        const int POLLING_INTERVAL_MS = 1000;            // (현재 미사용) 모니터 수 체크 주기

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

        // ===== Registry P/Invoke (LastWriteTime 조회용) =====

        private const int KEY_READ = 0x20019;         // STANDARD_RIGHTS_READ | KEY_QUERY_VALUE | KEY_ENUMERATE_SUB_KEYS | KEY_NOTIFY
        private const int KEY_WOW64_64KEY = 0x0100;   // 64bit 레지스트리 뷰 강제
        private static readonly UIntPtr HKEY_LOCAL_MACHINE = (UIntPtr)0x80000002u;

        [StructLayout(LayoutKind.Sequential)]
        private struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RegOpenKeyEx(
            UIntPtr hKey,
            string lpSubKey,
            uint ulOptions,
            int samDesired,
            out IntPtr phkResult);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int RegQueryInfoKey(
            IntPtr hKey,
            StringBuilder lpClass,
            ref uint lpcClass,
            IntPtr lpReserved,
            out uint lpcSubKeys,
            out uint lpcMaxSubKeyLen,
            out uint lpcMaxClassLen,
            out uint lpcValues,
            out uint lpcMaxValueNameLen,
            out uint lpcbMaxValueLen,
            out uint lpcbSecurityDescriptor,
            out FILETIME lpftLastWriteTime);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int RegCloseKey(IntPtr hKey);

        // ===== 외부에서 쓸 공개 API =====

        /// <summary>
        /// 현재 상태에서 한 번만 /clone + 해상도 맞추기 수행.
        /// </summary>
        public static void AdjustOnce()
        {
            AdjustDisplayMode();
        }

        /// <summary>
        /// 현재 모니터 개수 반환.
        /// </summary>
        public static int GetMonitorCountPublic() => GetMonitorCount();

        // ===== 내부 구현부 =====

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

                _log.Debug($"Execute: {path} {argument}");
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

        // Registry: 특정 키의 LastWriteTime 가져오기
        private static DateTime? GetRegistryKeyLastWriteTime(RegistryHive hive, string subKeyPath)
        {
            UIntPtr root = hive switch
            {
                RegistryHive.LocalMachine => HKEY_LOCAL_MACHINE,
                _ => throw new NotSupportedException("지원하지 않는 hive 입니다.")
            };

            IntPtr hKey;
            int sam = KEY_READ | KEY_WOW64_64KEY;

            int rc = RegOpenKeyEx(root, subKeyPath, 0, sam, out hKey);
            if (rc != 0 || hKey == IntPtr.Zero)
                return null;

            try
            {
                uint classLen = 0;
                var ft = new FILETIME();
                uint subKeys, maxSubKeyLen, maxClassLen, values, maxValueNameLen, maxValueLen, sdLen;

                rc = RegQueryInfoKey(
                    hKey,
                    null,
                    ref classLen,
                    IntPtr.Zero,
                    out subKeys,
                    out maxSubKeyLen,
                    out maxClassLen,
                    out values,
                    out maxValueNameLen,
                    out maxValueLen,
                    out sdLen,
                    out ft);

                if (rc != 0)
                    return null;

                long fileTime = ((long)ft.dwHighDateTime << 32) | ft.dwLowDateTime;
                return DateTime.FromFileTimeUtc(fileTime).ToLocalTime();
            }
            finally
            {
                RegCloseKey(hKey);
            }
        }

        // GraphicsDrivers\Configuration 하위에서 최근 N개 서브키 삭제
        private static void DeleteRecentMonitorConfigEntries(int countToDelete = 2)
        {
            const string configBasePath = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Configuration";

            try
            {
                using (var baseKey = Registry.LocalMachine.OpenSubKey(configBasePath, writable: true))
                {
                    if (baseKey == null)
                    {
                        _log.Debug("GraphicsDrivers\\Configuration 키를 열 수 없습니다.");
                        return;
                    }

                    var subKeyNames = baseKey.GetSubKeyNames();
                    if (subKeyNames == null || subKeyNames.Length == 0)
                    {
                        _log.Debug("삭제할 서브키가 없습니다.");
                        return;
                    }

                    var list = new List<(string Name, DateTime? LastWrite)>();

                    foreach (var name in subKeyNames)
                    {
                        string fullPath = configBasePath + "\\" + name;
                        var lw = GetRegistryKeyLastWriteTime(RegistryHive.LocalMachine, fullPath);
                        list.Add((name, lw));
                    }

                    // LastWriteTime 기준 내림차순 정렬 (가장 최근 것이 앞에)
                    list.Sort((a, b) =>
                    {
                        if (!a.LastWrite.HasValue && !b.LastWrite.HasValue) return 0;
                        if (!a.LastWrite.HasValue) return 1;
                        if (!b.LastWrite.HasValue) return -1;
                        return b.LastWrite.Value.CompareTo(a.LastWrite.Value);
                    });

                    int deleted = 0;
                    foreach (var item in list)
                    {
                        if (deleted >= list.Count - 1) break;

                        try
                        {
                            _log.Debug($"Deleting monitor config subkey: {item.Name}, LastWrite={item.LastWrite}");
                            baseKey.DeleteSubKeyTree(item.Name, throwOnMissingSubKey: false);
                            deleted++;
                        }
                        catch (Exception ex)
                        {
                            _log.Debug($"  DeleteSubKeyTree 실패 ({item.Name}): {ex.Message}");
                        }
                    }

                    _log.Debug($"총 {deleted}개의 최근 모니터 설정 서브키를 삭제했습니다.");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _log.Debug("레지스트리 삭제 권한이 없습니다. 관리자 권한으로 실행해야 합니다.");
                _log.Debug(ex.Message);
            }
            catch (Exception ex)
            {
                _log.Debug("DeleteRecentMonitorConfigEntries 중 예외 발생: " + ex);
            }
        }

        // 실제 동작 본체 (/internal + 레지스트리 정리 + /clone + 해상도 맞추기)
        public static void AdjustDisplayMode()
        {
            // 모니터 붙었다/빠졌다 직후 안정화 대기 (현재 0ms)
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

            // Step1: /internal (PC 화면만)
            _log.Debug("  Step1: /internal (PC screen only)...");
            ExecuteDisplaySwitch("/internal");
            Thread.Sleep(AFTER_INTERNAL_SETTLE_DELAY_MS);

            // Step2: GraphicsDrivers\\Configuration 하위 최근 2개 설정 삭제
            _log.Debug("  Step2: Delete recent monitor configuration entries in registry...");
            DeleteRecentMonitorConfigEntries(2);

            // Step3: /clone 으로 복제 모드 전환
            _log.Debug("  Step3: /clone (duplicate)...");
            ExecuteDisplaySwitch("/clone");

            // Step4: 주모니터의 해상도 전체 모니터에 적용
            SetAllMonitorsResolution(w, h);

            // 드라이버/OS가 최적화 끝내도록 추가 대기
            Thread.Sleep(AFTER_CLONE_SETTLE_DELAY_MS);

            _log.Debug("  AdjustDisplayMode finished.\n");
        }
    }
}
