using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace NoUACLauncher
{
    public static class AutoStartHelper
    {
        public const string AutoStartRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        public const string AutoStartRegistryValue = "NoUACLauncher";

        public static bool IsAutoStartEnabled(string launchPath)
        {
            string fullPath = Path.GetFullPath(launchPath);
            string autoStartValue = null;
            using (var registryKey = Registry.CurrentUser.OpenSubKey(AutoStartRegistryKey,
                RegistryKeyPermissionCheck.Default, RegistryRights.QueryValues))
            {
                if (registryKey == null) return false;

                autoStartValue = registryKey.GetValue(AutoStartRegistryValue, null) as string;
            }

            if (string.IsNullOrWhiteSpace(autoStartValue)) return false;

            string regPath = autoStartValue.Trim().Split(' ')[0];
            string regFullPath = null;
            try
            {
                regFullPath = Path.GetFullPath(regPath);
            }
            catch
            {
                return false;
            }

            return string.Equals(fullPath, regFullPath, StringComparison.OrdinalIgnoreCase);
        }

        public static void EnableAutoStart(string launchPath, string arguments)
        {
            string fullPath = Path.GetFullPath(launchPath);
            string value = fullPath;
            if (!string.IsNullOrWhiteSpace(arguments))
                value = $"{fullPath} {arguments.Trim()}";

            using (var registryKey = Registry.CurrentUser.CreateSubKey(AutoStartRegistryKey))
            {
                registryKey.SetValue(AutoStartRegistryValue, value, RegistryValueKind.String);
            }
        }

        public static void DisableAutoStart()
        {
            using (var registryKey = Registry.CurrentUser.OpenSubKey(AutoStartRegistryKey,
                RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.SetValue | RegistryRights.QueryValues))
            {
                if (registryKey == null) return;

                registryKey.DeleteValue(AutoStartRegistryValue, false);
            }
        }
    }
}
