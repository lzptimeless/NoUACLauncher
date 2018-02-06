using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace NoUACLauncher
{
    /// <summary>
    /// Some UAC functions
    /// 一些UAC辅助函数
    /// </summary>
    public static class UACHelper
    {
        private const string UACRegistryKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
        private const string UACRegistryValue = "EnableLUA";

        private static uint STANDARD_RIGHTS_READ = 0x00020000;
        private static uint TOKEN_QUERY = 0x0008;
        private static uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);

        /// <summary>
        /// 这里返回的Handle是一个伪Handle（常量），用以指代当前进程，不需要释放
        /// The Handle returned is a pseudo Handle(constant), use to indicate a process, no need to release
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();

        /// <summary>
        /// TokenHandle需要通过CloseHandle释放
        /// TokenHandle need release through CloseHandle
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, UInt32 TokenInformationLength, out UInt32 ReturnLength);

        private enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass
        }

        private enum TOKEN_ELEVATION_TYPE
        {
            TokenElevationTypeDefault = 1,
            TokenElevationTypeFull,
            TokenElevationTypeLimited
        }

        private static bool? _isEnviromentSupported;

        public static bool IsUACEnabled()
        {
            CheckEnviroment();

            using (RegistryKey uacKey = Registry.LocalMachine.OpenSubKey(UACRegistryKey, false))
            {
                if (uacKey == null)
                    throw new KeyNotFoundException("Not found registry key: " + UACRegistryKey);

                object uacValue = uacKey.GetValue(UACRegistryValue);
                if (uacValue == null)
                    return false;

                return uacValue.Equals(1);
            }
        }

        public static bool IsProcessElevated()
        {
            CheckEnviroment();

            if (IsUACEnabled())
            {
                IntPtr tokenHandle = IntPtr.Zero;
                IntPtr elevationTypePtr = IntPtr.Zero;
                Int32 win32Err;
                try
                {
                    if (!OpenProcessToken(GetCurrentProcess(), TOKEN_READ, out tokenHandle))
                    {
                        win32Err = Marshal.GetLastWin32Error();
                        throw new ApplicationException("Could not get process token.  Win32 Error Code: " + win32Err.ToString("X"));
                    }

                    TOKEN_ELEVATION_TYPE elevationResult = TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault;

                    int elevationResultSize = Marshal.SizeOf(Enum.GetUnderlyingType(typeof(TOKEN_ELEVATION_TYPE)));
                    uint returnedSize = 0;
                    elevationTypePtr = Marshal.AllocHGlobal(elevationResultSize);

                    bool success = GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevationType, elevationTypePtr, (uint)elevationResultSize, out returnedSize);
                    if (success)
                    {
                        elevationResult = (TOKEN_ELEVATION_TYPE)Marshal.ReadInt32(elevationTypePtr);
                        bool isProcessElevated = elevationResult == TOKEN_ELEVATION_TYPE.TokenElevationTypeFull;
                        return isProcessElevated;
                    }
                    else
                    {
                        win32Err = Marshal.GetLastWin32Error();
                        throw new ApplicationException("Unable to determine the current elevation: " + win32Err.ToString("X"));
                    }
                }
                finally
                {
                    if (tokenHandle != IntPtr.Zero) CloseHandle(tokenHandle);
                    if (elevationTypePtr != IntPtr.Zero) Marshal.FreeHGlobal(elevationTypePtr);
                }
            }
            else
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    bool result = principal.IsInRole(WindowsBuiltInRole.Administrator)
                               || principal.IsInRole(0x200); //Domain Administrator
                    return result;
                }
            }
        }

        private static void CheckEnviroment()
        {
            bool isSupported = false;
            if (_isEnviromentSupported.HasValue)
                isSupported = _isEnviromentSupported.Value;
            else
            {
                var osVersion = Environment.OSVersion;
                isSupported = osVersion.Platform == PlatformID.Win32NT && osVersion.Version.Major >= 6;
                _isEnviromentSupported = isSupported;
            }

            if (!isSupported)
                throw new NotSupportedException($"Can not support below Vista.");
        }
    }
}
