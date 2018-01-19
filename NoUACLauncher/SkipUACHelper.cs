using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NoUACLauncher
{
    public static class SkipUACHelper
    {
        private const string SkipUACTaskName = "NoUACLauncherSkipUAC";
        private const string SkipUACTaskAuthor = "Lzp";

        public static bool IsSkipUACTaskExist()
        {
            using (TaskService ts = new TaskService())
            {
                var skipTasks = ts.RootFolder.EnumerateTasks(task =>
                {
                    return string.Equals(task.Name, SkipUACTaskName, StringComparison.OrdinalIgnoreCase);
                }, false);

                return skipTasks.Any();
            }
        }

        public static void CreateSkipUACTask()
        {
            using (TaskService ts = new TaskService())
            {
                TaskDefinition td = ts.NewTask();
                td.RegistrationInfo.Author = SkipUACTaskAuthor;
                td.RegistrationInfo.Description = "Run NoUACLauncher without UAC notify";
                td.RegistrationInfo.Date = DateTime.Now;
                td.RegistrationInfo.Source = "NoUACLauncher";
                td.RegistrationInfo.Version = new Version(0, 1, 0, 0);

                

                ts.RootFolder.RegisterTaskDefinition(SkipUACTaskName, td);
            }
        }

        public static void DeleteSkipUACTask()
        {
            using (TaskService ts = new TaskService())
            {
                ts.RootFolder.DeleteTask(SkipUACTaskName, false);
            }
        }

        public static void RunSkipUACTask()
        {
            using (TaskService ts = new TaskService())
            {
                var skipTasks = ts.RootFolder.EnumerateTasks(task =>
                {
                    return string.Equals(task.Name, SkipUACTaskName, StringComparison.OrdinalIgnoreCase);
                }, false);

                var skipTask = skipTasks.FirstOrDefault();

                if (skipTask == null)
                    throw new KeyNotFoundException("Not found skip uac task: " + SkipUACTaskName);

                string exePath = Assembly.GetEntryAssembly().Location;
                skipTask.Run(exePath);
            }
        }
    }
}
