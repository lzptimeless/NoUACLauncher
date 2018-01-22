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
        private const string RunAdminArgument = "-runas";
        private const string RunSkipUACArgument = "-skipuac";
        private const UInt32 ERROR_USER_CANCELLED = 0x80004005;
        //private static string AppUniqueMutexName = "NoUACLauncher";

        protected override void OnStartup(StartupEventArgs e)
        {
            string launchPath = Assembly.GetEntryAssembly().Location;
            if (!UACHelper.IsProcessElevated())
            {
                // Current process run without elevated
                // Try to run new process through taskschedule
                if (SkipUACHelper.IsSkipUACTaskExist(launchPath))
                {
                    try
                    {
                        SkipUACHelper.RunSkipUACTask(launchPath, RunSkipUACArgument);
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
                    psi.Arguments = RunAdminArgument;
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
            else
            {
                // Process run with elevated
                if (e.Args.Contains(RunAdminArgument))
                    MessageBox.Show("Run admin");
                else if (e.Args.Contains(RunSkipUACArgument))
                    MessageBox.Show("Run skip uac");
                else
                    MessageBox.Show("Run elevated");
            }

            base.OnStartup(e);
        }

        private void SaveElevateProcessFailedMessage(Exception ex)
        {

        }
    }
}
