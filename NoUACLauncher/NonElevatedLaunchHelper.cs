using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NoUACLauncher
{
    public static class NonElevatedLaunchHelper
    {
        private const UInt32 TOKEN_ASSIGN_PRIMARY = 0x0001;
        private const UInt32 TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const UInt32 TOKEN_DUPLICATE = 0x0002;
        private const UInt32 TOKEN_QUERY = 0x0008;
        private const UInt32 TOKEN_ADJUST_DEFAULT = 0x0080;
        private const UInt32 TOKEN_ADJUST_SESSIONID = 0x0100;
        private const string SE_INCREASE_QUOTA_NAME = "SeIncreaseQuotaPrivilege";
        private const UInt32 SE_PRIVILEGE_ENABLED = 0x00000002;
        private const Int32 ERROR_SUCCESS = 0;
        private const UInt32 PROCESS_QUERY_INFORMATION = 0x0400;

        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFOW
        {
            public UInt32 cb;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpReserved;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpDesktop;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpTitle;
            public UInt32 dwX;
            public UInt32 dwY;
            public UInt32 dwXSize;
            public UInt32 dwYSize;
            public UInt32 dwXCountChars;
            public UInt32 dwYCountChars;
            public UInt32 dwFillAttribute;
            public UInt32 dwFlags;
            public UInt16 wShowWindow;
            public UInt16 cbReserved2;
            public IntPtr lpReserved2;
            /// <summary>
            /// 要释放
            /// Need release
            /// </summary>
            public IntPtr hStdInput;
            /// <summary>
            /// 要释放
            /// Need release
            /// </summary>
            public IntPtr hStdOutput;
            /// <summary>
            /// 要释放
            /// Need release
            /// </summary>
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            /// <summary>
            /// 要释放
            /// Need release
            /// </summary>
            public IntPtr hProcess;
            /// <summary>
            /// 要释放
            /// Need release
            /// </summary>
            public IntPtr hThread;
            public UInt32 dwProcessId;
            public UInt32 dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public UInt32 LowPart;
            public Int32 HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public UInt32 Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_PRIVILEGES
        {
            public UInt32 PrivilegeCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1, ArraySubType = UnmanagedType.Struct)]
            public LUID_AND_ATTRIBUTES[] Privileges;

            public static TOKEN_PRIVILEGES Create()
            {
                TOKEN_PRIVILEGES obj = new TOKEN_PRIVILEGES();
                obj.Privileges = new LUID_AND_ATTRIBUTES[1];
                obj.PrivilegeCount = 1;
                return obj;
            }
        }

        private enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }

        private enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }

        /// <summary>
        /// TokenHandle需要通过CloseHandle释放
        /// TokenHandle need release through CloseHandle
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, ref IntPtr TokenHandle);

        /// <summary>
        /// lpStartupInfo里面的Handle要通过CloseHandle释放，lpProcessInformation里面的Handle要通过CloseHandle释放
        /// Handle in lpStartupInfo must release through CloseHandle, Handles in lpProcessInformation must
        /// release through CloseHandle
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CreateProcessWithTokenW(IntPtr hToken, UInt32 dwLogonFlags,
            string lpApplicationName, string lpCommandLine, UInt32 dwCreationFlags, IntPtr lpEnvironment,
            string lpCurrentDirectory, ref STARTUPINFOW lpStartupInfo, ref PROCESS_INFORMATION lpProcessInformation);

        /// <summary>
        /// 这里返回的Handle是一个伪Handle（常量），用以指代当前进程，不需要释放
        /// The Handle returned is a pseudo Handle(constant), use to indicate a process, no need to release
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();

        /// <summary>
        /// 没有需要释放的东西
        /// Nothing need to release
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool LookupPrivilegeValueW(string lpSystemName, string lpName, ref LUID lpLuid);

        /// <summary>
        /// 没有需要释放的东西
        /// Nothing need to release
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges,
            ref TOKEN_PRIVILEGES NewState, UInt32 BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// 没有需要释放的东西
        /// Nothing need to release
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetShellWindow();

        /// <summary>
        /// 没有需要释放的东西
        /// Nothing need to release
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern UInt32 GetWindowThreadProcessId(IntPtr hWnd, ref UInt32 lpdwProcessId);

        /// <summary>
        /// 返回的Handle需要用CloseHandle释放
        /// The returned Handle need release through CloseHandle
        /// </summary>
        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(UInt32 dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, UInt32 dwProcessId);

        /// <summary>
        /// phNewToken需要用CloseHandle释放
        /// phNewToken need release through CloseHandle
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DuplicateTokenEx(IntPtr hExistingToken, UInt32 dwDesiredAccess, IntPtr lpTokenAttributes,
            SECURITY_IMPERSONATION_LEVEL ImpersonationLevel, TOKEN_TYPE TokenType, ref IntPtr phNewToken);

        private static bool LaunchWithShellToken(string launchPath, string cmd, string workingDir)
        {
            IntPtr hShellProcess = IntPtr.Zero, hShellProcessToken = IntPtr.Zero, hPrimaryToken = IntPtr.Zero;
            IntPtr hProcessToken = IntPtr.Zero;
            STARTUPINFOW si = new STARTUPINFOW();
            si.cb = (UInt32)Marshal.SizeOf(typeof(STARTUPINFOW));
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

            // hwnd不需要释放
            // hwnd no need to release
            IntPtr hwnd = IntPtr.Zero;
            UInt32 dwPID = 0;
            bool ret;
            Int32 dwLastErr;

            // 本函数的返回值
            // The return value of this function
            bool retval = false;

            try
            {
                // 为当前进程开启SE_INCREASE_QUOTA_NAME权限（调用CreateProcessWithTokenW需要这个权限）
                // Enable the SeIncreaseQuotaPrivilege in your current token (call CreateProcessWithTokenW need this privilege)
                if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES, ref hProcessToken))
                {
                    dwLastErr = Marshal.GetLastWin32Error();
                    throw new ApplicationException("OpenProcessToken failed: 0x" + dwLastErr.ToString("X"));
                }
                else
                {
                    TOKEN_PRIVILEGES tkp = TOKEN_PRIVILEGES.Create();
                    LUID luid = new LUID();
                    if (!LookupPrivilegeValueW(null, SE_INCREASE_QUOTA_NAME, ref luid))
                    {
                        dwLastErr = Marshal.GetLastWin32Error();
                        throw new ApplicationException("LookupPrivilegeValueW failed: 0x" + dwLastErr.ToString("X"));
                    }

                    tkp.Privileges[0].Luid = luid;
                    tkp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
                    AdjustTokenPrivileges(hProcessToken, false, ref tkp, 0, IntPtr.Zero, IntPtr.Zero);
                    dwLastErr = Marshal.GetLastWin32Error();
                    if (ERROR_SUCCESS != dwLastErr)
                        throw new ApplicationException("AdjustTokenPrivileges failed: 0x" + dwLastErr.ToString("X"));
                }

                // 获取Shell的HWND
                // Get an HWND representing the desktop shell 
                hwnd = GetShellWindow();
                if (IntPtr.Zero == hwnd)
                    throw new ApplicationException("No desktop shell is present");

                // 获取Shell的PID
                // Get the Process ID (PID) of the process associated with that window
                GetWindowThreadProcessId(hwnd, ref dwPID);
                if (0 == dwPID)
                    throw new ApplicationException("Unable to get PID of desktop shell");

                // 打开Shell进程对象
                // Open that process
                hShellProcess = OpenProcess(PROCESS_QUERY_INFORMATION, false, dwPID);
                if (hShellProcess == IntPtr.Zero)
                {
                    dwLastErr = Marshal.GetLastWin32Error();
                    throw new ApplicationException("Can't open desktop shell process: 0x" + dwLastErr.ToString("X"));
                }

                // 获取Shell进程的Token
                // Get the access token from that process
                ret = OpenProcessToken(hShellProcess, TOKEN_DUPLICATE, ref hShellProcessToken);
                if (!ret)
                {
                    dwLastErr = Marshal.GetLastWin32Error();
                    throw new ApplicationException("Can't get process token of desktop shell: 0x" + dwLastErr.ToString("X"));
                }

                // 通过Shell进程Token，复制一份Primary Token
                // Make a primary token with that token
                UInt32 dwTokenRights = TOKEN_QUERY | TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_ADJUST_DEFAULT | TOKEN_ADJUST_SESSIONID;
                ret = DuplicateTokenEx(hShellProcessToken, dwTokenRights, IntPtr.Zero, SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                    TOKEN_TYPE.TokenPrimary, ref hPrimaryToken);
                if (!ret)
                {
                    dwLastErr = Marshal.GetLastWin32Error();
                    throw new ApplicationException("Can't get primary token: 0x" + dwLastErr.ToString("X"));
                }

                // 用复制得到的Token启动程序
                // Start the new process with that primary token
                ret = CreateProcessWithTokenW(hPrimaryToken, 0, launchPath, cmd, 0, IntPtr.Zero,
                    workingDir, ref si, ref pi);
                if (!ret)
                {
                    dwLastErr = Marshal.GetLastWin32Error();
                    throw new ApplicationException("CreateProcessWithTokenW failed: 0x" + dwLastErr.ToString("X"));
                }

                retval = true;
            }
            finally
            {
                if (hProcessToken != IntPtr.Zero) CloseHandle(hProcessToken);
                if (hPrimaryToken != IntPtr.Zero) CloseHandle(hPrimaryToken);
                if (hShellProcessToken != IntPtr.Zero) CloseHandle(hShellProcessToken);
                if (hShellProcess != IntPtr.Zero) CloseHandle(hShellProcess);
                if (si.hStdError != IntPtr.Zero) CloseHandle(si.hStdError);
                if (si.hStdInput != IntPtr.Zero) CloseHandle(si.hStdInput);
                if (si.hStdOutput != IntPtr.Zero) CloseHandle(si.hStdOutput);
                if (pi.hThread != IntPtr.Zero) CloseHandle(pi.hThread);
                if (pi.hProcess != IntPtr.Zero) CloseHandle(pi.hProcess);
            }

            return retval;
        }

        public static bool Launch(string launchPath, string cmd, string workingDir)
        {
            if (string.IsNullOrWhiteSpace(launchPath))
                throw new ArgumentException("launchPath can not be null or empty");

            string fullLaunchPath = launchPath;
            try
            {
                fullLaunchPath = Path.GetFullPath(launchPath);
            }
            catch { }

            if (string.IsNullOrWhiteSpace(cmd))
                cmd = $"\"{fullLaunchPath}\"";
            else
                cmd = $"\"{fullLaunchPath}\" {cmd.Trim()}";

            if (string.IsNullOrWhiteSpace(workingDir))
                workingDir = null;
            else
                workingDir = Path.GetFullPath(workingDir);

            if (!UACHelper.IsProcessElevated())
            {
                ProcessStartInfo psi = new ProcessStartInfo(fullLaunchPath, cmd);
                psi.WorkingDirectory = workingDir;
                var p = Process.Start(psi);

                return p != null;
            }

            bool retval = LaunchWithShellToken(fullLaunchPath, cmd, workingDir);
            return retval;
        }
    }
}
