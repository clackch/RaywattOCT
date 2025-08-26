using System;
using System.Runtime.InteropServices;

namespace RaywattApp.Common.Util
{
    public class Win32Helper
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////// Import
        ////////////////////////////////////////////////////////////////////////////////////////// Static
        //////////////////////////////////////////////////////////////////////////////// Private

        #region 현재 프로세스 구하기 - GetCurrentProcess()

        /// <summary>
        /// 현재 프로세스 구하기
        /// </summary>
        /// <returns>현재 프로세스 핸들</returns>
        [DllImport("kernel32", ExactSpelling = true)]
        private static extern IntPtr GetCurrentProcess();

        #endregion
        #region 프로세스 토큰 열기 - OpenProcessToken(processHandle, desiredAccess, tokenHandle)

        /// <summary>
        /// 프로세스 토큰 열기
        /// </summary>
        /// <param name="processHandle">프로세스 핸들</param>
        /// <param name="desiredAccess">희망 액세스</param>
        /// <param name="tokenHandle">토큰 핸들</param>
        /// <returns>처리 결과</returns>
        [DllImport("advapi32", ExactSpelling = true, SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr processHandle, int desiredAccess, ref IntPtr tokenHandle);

        #endregion
        #region 특권 값 조사하기 - LookupPrivilegeValue(hostName, name, luid)

        /// <summary>
        /// 특권 값 조사하기
        /// </summary>
        /// <param name="hostName">호스트명</param>
        /// <param name="name">명칭</param>
        /// <param name="luid">LUID</param>
        /// <returns>처리 결과</returns>
        [DllImport("advapi32", SetLastError = true)]
        private static extern bool LookupPrivilegeValue(string hostName, string name, ref long luid);

        #endregion
        #region 토큰 특권 조정하기 - AdjustTokenPrivileges(tokenHandle, disableAllPrivileges, newState, bufferLength, previousState, returnLength)

        /// <summary>
        /// 토큰 특권 조정하기
        /// </summary>
        /// <param name="tokenHandle">토큰 핸들</param>
        /// <param name="disableAllPrivileges">모든 특권 비활성화 여부</param>
        /// <param name="newState">새로운 상태</param>
        /// <param name="bufferLength">버퍼 길이</param>
        /// <param name="previousState">이전 상태</param>
        /// <param name="returnLength">반환 길이</param>
        /// <returns>처리 결과</returns>
        [DllImport("advapi32", ExactSpelling = true, SetLastError = true)]
        private static extern bool AdjustTokenPrivileges
        (
            IntPtr tokenHandle,
            bool disableAllPrivileges,
            ref TOKEN_PRIVILEGES newState,
            int bufferLength,
            IntPtr previousState,
            IntPtr returnLength
        );

        #endregion
        #region 윈도우 종료하기 - ExitWindowsEx(flag, reserved)

        /// <summary>
        /// 윈도우 종료하기
        /// </summary>
        /// <param name="flag">플래그</param>
        /// <param name="reserved">예약</param>
        /// <returns>처리 결과</returns>
        [DllImport("user32", ExactSpelling = true, SetLastError = true)]
        private static extern bool ExitWindowsEx(int flag, int reserved);

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////// Structure
        ////////////////////////////////////////////////////////////////////////////////////////// Private

        #region 토큰 특권 - TOKEN_PRIVILEGES

        /// <summary>
        /// 토큰 특권
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct TOKEN_PRIVILEGES
        {
            //////////////////////////////////////////////////////////////////////////////////////////////////// Field
            ////////////////////////////////////////////////////////////////////////////////////////// Public

            #region Field

            /// <summary>
            /// 특권 카운트
            /// </summary>
            public int PrivilegeCount;

            /// <summary>
            /// LUID
            /// </summary>
            public long LUID;

            /// <summary>
            /// 속성
            /// </summary>
            public int Attributes;

            #endregion
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////// Field
        ////////////////////////////////////////////////////////////////////////////////////////// Constant
        //////////////////////////////////////////////////////////////////////////////// Public

        #region Field

        /// <summary>
        /// SE_PRIVILEGE_ENABLED
        /// </summary>
        public const int SE_PRIVILEGE_ENABLED = 0x00000002;

        /// <summary>
        /// TOKEN_QUERY
        /// </summary>
        public const int TOKEN_QUERY = 0x00000008;

        /// <summary>
        /// TOKEN_ADJUST_PRIVILEGES
        /// </summary>
        public const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;

        /// <summary>
        /// SE_SHUTDOWN_NAME
        /// </summary>
        public const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";

        /// <summary>
        /// EWX_LOGOFF
        /// </summary>
        public const int EWX_LOGOFF = 0x00000000;

        /// <summary>
        /// EWX_SHUTDOWN
        /// </summary>
        public const int EWX_SHUTDOWN = 0x00000001;

        /// <summary>
        /// EWX_REBOOT
        /// </summary>
        public const int EWX_REBOOT = 0x00000002;

        /// <summary>
        /// EWX_FORCE
        /// </summary>
        public const int EWX_FORCE = 0x00000004;

        /// <summary>
        /// EWX_POWEROFF
        /// </summary>
        public const int EWX_POWEROFF = 0x00000008;

        /// <summary>
        /// EWX_FORCEIFHUNG
        /// </summary>
        public const int EWX_FORCEIFHUNG = 0x00000010;

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////// Method
        ////////////////////////////////////////////////////////////////////////////////////////// Static
        //////////////////////////////////////////////////////////////////////////////// Public

        #region 윈도우 종료하기 - ExitWindow(flag)

        /// <summary>
        /// 윈도우 종료하기
        /// </summary>
        /// <param name="flag">플래그</param>
        public static void ExitWindow(int flag)
        {
            bool result;

            IntPtr processHandle = GetCurrentProcess();

            IntPtr tokenHandle = IntPtr.Zero;

            result = OpenProcessToken(processHandle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref tokenHandle);

            TOKEN_PRIVILEGES tokenPrivileges;

            tokenPrivileges.PrivilegeCount = 1;
            tokenPrivileges.LUID = 0;
            tokenPrivileges.Attributes = SE_PRIVILEGE_ENABLED;

            result = LookupPrivilegeValue(null, SE_SHUTDOWN_NAME, ref tokenPrivileges.LUID);

            result = AdjustTokenPrivileges(tokenHandle, false, ref tokenPrivileges, 0, IntPtr.Zero, IntPtr.Zero);

            result = ExitWindowsEx(flag, 0);
        }

        #endregion

        #region 셧다운 하기 - Shutdown(false)

        /// <summary>
        /// 셧다운 하기
        /// </summary>
        /// <param name="forced">강제 여부</param>
        public static void Shutdown(bool forced = false)
        {
            if (forced)
            {
                ExitWindow(EWX_SHUTDOWN | EWX_FORCE);
            }
            else
            {
                ExitWindow(EWX_SHUTDOWN);
            }
        }

        #endregion
        #region 리부팅 하기 - Reboot(false)

        /// <summary>
        /// 리부팅 하기
        /// </summary>
        /// <param name="forced">강제 여부</param>
        public static void Reboot(bool forced = false)
        {
            if (forced)
            {
                ExitWindow(EWX_REBOOT | EWX_FORCE);
            }
            else
            {
                ExitWindow(EWX_REBOOT);
            }
        }

        #endregion
        #region 로그 오프 하기 - LogOff(false)

        /// <summary>
        /// 로그 오프 하기
        /// </summary>
        /// <param name="forced">강제 여부</param>
        public static void LogOff(bool forced = false)
        {
            if (forced)
            {
                ExitWindow(EWX_LOGOFF | EWX_FORCE);
            }
            else
            {
                ExitWindow(EWX_LOGOFF);
            }
        }

        #endregion
    }
}
