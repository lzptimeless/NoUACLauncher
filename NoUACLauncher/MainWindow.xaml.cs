using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NoUACLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string launcherPath = Assembly.GetEntryAssembly().Location;
            EnableSkipUAC.IsChecked = SkipUACHelper.IsSkipUACTaskExist(launcherPath);
        }

        private void EnableSkipUAC_Checked(object sender, RoutedEventArgs e)
        {
            string launcherPath = Assembly.GetEntryAssembly().Location;
            if (SkipUACHelper.IsSkipUACTaskExist(launcherPath)) return;

            try
            {
                SkipUACHelper.CreateSkipUACTask(launcherPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Create skip uac task failed.\r\n" + ex, "NoUACLauncher", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnableSkipUAC_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                SkipUACHelper.DeleteSkipUACTask();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete skip uac task failed.\r\n" + ex, "NoUACLauncher", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
