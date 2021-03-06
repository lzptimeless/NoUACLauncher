﻿using Microsoft.Win32.TaskScheduler;
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
        private const string RunAutoStart = "-autostart";
        private const string RunSchedule = "-schedule";
        private const UInt32 ERROR_USER_CANCELLED = 0x80004005;
        //private static string AppUniqueMutexName = "NoUACLauncher";

        public string StartMode { get; private set; }

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
                    psi.Verb = "runas"; // runas代表要向用户请求admin权限

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
                return;
            }
            else
            {
                // Process run with elevated
                if (e.Args.Contains(RunAdminArgument))
                    StartMode = "Run admin"; // 通过向用户请求获得admin权限启动
                else if (e.Args.Contains(RunSkipUACArgument))
                    StartMode = "Run skip uac"; // 通过计划任务获得admin权限启动
                else
                    StartMode = "Run elevated"; // 程序被用户或其他程序以admin权限启动
            }

            base.OnStartup(e);
        }

        private void SaveElevateProcessFailedMessage(Exception ex)
        {

        }
    }
}
