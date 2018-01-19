using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NoUACLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const UInt32 ERROR_USER_CANCELLED = 0x80004005;
        //private static string AppUniqueMutexName = "NoUACLauncher";

        protected override void OnStartup(StartupEventArgs e)
        {
            if (!UACHelper.IsProcessElevated())
            {
                // Current process run without elevated
                // Try to run new process through taskschedule
                if (SkipUACHelper.IsSkipUACTaskExist())
                {
                    try
                    {
                        SkipUACHelper.RunSkipUACTask();
                    }
                    catch (Exception ex)
                    {
                        SaveElevateProcessFailedMessage(ex);
                        MessageBox.Show("Run skip uac task failed.", "NoUACLauncher", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = Assembly.GetEntryAssembly().Location;
                    psi.Verb = "runas";

                    try
                    {
                        Process.Start(psi);
                    }
                    catch (Exception ex)
                    {
                        SaveElevateProcessFailedMessage(ex);
                        if (!(ex is Win32Exception && (uint)(ex as Win32Exception).ErrorCode == ERROR_USER_CANCELLED))
                        {
                            MessageBox.Show("Start elevate process failed.", "NoUACLauncher", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }

                Shutdown();
            }

            // Process run with elevated

            base.OnStartup(e);
        }

        private void SaveElevateProcessFailedMessage(Exception ex)
        {

        }
    }
}
