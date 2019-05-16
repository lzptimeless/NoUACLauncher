using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NoUACLauncher
{
    public static class SkipUACHelper
    {
        /* Key points:
         * 1.The best schedule task name is "application + current system user name", it is easy
         * to check manually and make user has personal setting respectively.
         * 2.There are some options need change form default value:
         *   -IdleSettings.StopOnIdleEnd = false, no need change it in theory, but change is better
         *   -DisallowStartIfOnBatteries = false, must
         *   -StopIfGoingOnBatteries = false, must
         *   -ExecutionTimeLimit = TimeSpan.Zero, must
         *   -AllowDemandStart = true, must
         *   -AllowHardTerminate = false, no need change it in theory, but change is better
         * 3.The options may be changed by other programs or user itself, we need to ensure the
         * launch path is accurate at least.
         */

        /* 要点:
         * 1.计划任务名最好定义为"程序相关+当前系统用户名"，这样方面查看并且不同系统用户具有不同的配置
         * 2.计划任务默认设置中一些值需要手动修改:
         *   -IdleSettings.StopOnIdleEnd = false 不改也应该没问题，改了更保险
         *   -DisallowStartIfOnBatteries = false 必须
         *   -StopIfGoingOnBatteries = false 必须
         *   -ExecutionTimeLimit = TimeSpan.Zero 必须
         *   -AllowDemandStart = true 必须
         *   -AllowHardTerminate = false 不改也应该没问题，改了更保险
         * 3.计划任务的设置可能被其他程序或用户修改，需要验证启动路径是否是本程序
         */

        private const string SkipUACTaskNameBase = "NoUACLauncherSkipUAC";
        private const string SkipUACTaskDescription = "Run NoUACLauncher without UAC notify";
        private const string SkipUACTaskAuthor = "Lzp";

        public static bool IsSkipUACTaskExist(string launchPath)
        {
            string skipUACTaskName = GetSkipUACTaskName();
            string fullPath = Path.GetFullPath(launchPath).ToLowerInvariant();
            using (TaskService ts = new TaskService())
            {
                foreach (var task in ts.RootFolder.Tasks)
                {
                    if (string.Equals(task.Name, skipUACTaskName, StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var action in task.Definition.Actions)
                        {
                            if (action is ExecAction)
                            {
                                var execAction = action as ExecAction;
                                string execPath = Path.GetFullPath(execAction.Path).ToLowerInvariant();
                                if (string.Equals(fullPath, execPath))
                                    return true;
                            }
                        }
                        return false;
                    }
                }
            }
            return false;
        }

        public static void CreateSkipUACTask(string launchPath)
        {
            if (string.IsNullOrEmpty(launchPath))
                throw new ArgumentException("launchPath", "launchPath can not be null or empty");

            if (!File.Exists(launchPath))
                throw new FileNotFoundException("Launcher can not found: " + launchPath);

            string skipUACTaskName = GetSkipUACTaskName();
            using (TaskService ts = new TaskService())
            {
                TaskDefinition td = ts.NewTask();
                td.RegistrationInfo.Author = SkipUACTaskAuthor;
                td.RegistrationInfo.Description = SkipUACTaskDescription;
                td.RegistrationInfo.Date = DateTime.Now;
                td.RegistrationInfo.Version = new Version(0, 1, 0, 0);

                td.Principal.RunLevel = TaskRunLevel.Highest;

                td.Actions.Add(launchPath, "$(Arg0)", null);

                td.Settings.IdleSettings.StopOnIdleEnd = false;
                td.Settings.DisallowStartIfOnBatteries = false;
                td.Settings.StopIfGoingOnBatteries = false;
                td.Settings.ExecutionTimeLimit = TimeSpan.Zero;
                td.Settings.AllowDemandStart = true;
                td.Settings.AllowHardTerminate = false;

                ts.RootFolder.RegisterTaskDefinition(skipUACTaskName, td, TaskCreation.CreateOrUpdate, null, null, TaskLogonType.InteractiveToken, null);
            }
        }

        public static void DeleteSkipUACTask()
        {
            string skipUACTaskName = GetSkipUACTaskName();
            using (TaskService ts = new TaskService())
            {
                ts.RootFolder.DeleteTask(skipUACTaskName, false);
            }
        }

        public static void RunSkipUACTask(string launchPath, string arugments)
        {
            arugments = arugments ?? string.Empty;// Ensure arguments not be null(Task.Run will failed)

            string skipUACTaskName = GetSkipUACTaskName();
            string fullPath = Path.GetFullPath(launchPath).ToLowerInvariant();
            using (TaskService ts = new TaskService())
            {
                foreach (var task in ts.RootFolder.Tasks)
                {
                    if (string.Equals(task.Name, skipUACTaskName, StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var action in task.Definition.Actions)
                        {
                            if (action is ExecAction)
                            {
                                var execAction = action as ExecAction;
                                string execPath = Path.GetFullPath(execAction.Path).ToLowerInvariant();
                                if (string.Equals(fullPath, execPath))
                                {
                                    task.Run(arugments);
                                    return;
                                }
                            }
                        }
                        throw new KeyNotFoundException("Not found skip uac action: " + fullPath);
                    }
                }
            }

            throw new KeyNotFoundException("Not found skip uac task: " + skipUACTaskName);
        }

        private static string GetSkipUACTaskName()
        {
            return $"{SkipUACTaskNameBase}_{Environment.UserName.Trim().ToLowerInvariant()}";
        }
    }
}
