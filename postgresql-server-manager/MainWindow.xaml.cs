using System;
using System.Diagnostics;
using System.Security.Principal;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32; // For registry access

namespace postgresql_server_manager
{
    public partial class MainWindow : Window
    {
        private const string ServiceName = "postgresql-x64-17";
        private const string RegistryKeyPath = @"SYSTEM\CurrentControlSet\Services\postgresql-x64-17";
        private const string RegistryValueName = "Start";

        public MainWindow()
        {
            InitializeComponent();
            EnsureAdminRights();
            UpdateServiceStatus();
            InitializeAutoStartCheckBox();
        }

        private void EnsureAdminRights()
        {
            var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            var isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

            if (!isAdmin)
            {
                var startInfo = new ProcessStartInfo()
                {
                    FileName = Process.GetCurrentProcess().MainModule.FileName,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                try
                {
                    Process.Start(startInfo);
                    Application.Current.Shutdown();
                }
                catch (Exception)
                {
                    Application.Current.Shutdown();
                }
            }
        }

        private void StartService_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ServiceController sc = new ServiceController(ServiceName);
                if (sc.Status != ServiceControllerStatus.Running)
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                }

                UpdateServiceStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting service: {ex.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void StopService_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ServiceController sc = new ServiceController(ServiceName);
                if (sc.Status != ServiceControllerStatus.Stopped)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                }

                UpdateServiceStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error stopping service: {ex.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void UpdateServiceStatus()
        {
            try
            {
                ServiceController sc = new ServiceController(ServiceName);
                StatusLabel.Content = $"Status: {sc.Status}";

                switch (sc.Status)
                {
                    case ServiceControllerStatus.Running:
                        StatusDot.Fill = Brushes.Green;
                        break;
                    case ServiceControllerStatus.Stopped:
                        StatusDot.Fill = Brushes.Red;
                        break;
                    default:
                        StatusDot.Fill = Brushes.Yellow;
                        break;
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Content = "Status: Unknown";
                StatusDot.Fill = Brushes.Yellow;
                MessageBox.Show($"Error retrieving service status: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeAutoStartCheckBox()
        {
            try
            {
                // Check the registry to see if autostart is enabled
                using (var key = Registry.LocalMachine.OpenSubKey(RegistryKeyPath))
                {
                    if (key != null)
                    {
                        int startValue = (int)key.GetValue(RegistryValueName, 0);
                        AutoStartCheckBox.IsChecked = startValue == 2; // 2 means AutoStart enabled
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking auto-start setting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AutoStartCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SetAutoStart(true);
        }

        private void AutoStartCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SetAutoStart(false);
        }

        private void SetAutoStart(bool enable)
        {
            try
            {
                using (var key = Registry.LocalMachine.CreateSubKey(RegistryKeyPath))
                {
                    if (key != null)
                    {
                        // Set the start value to 2 (Automatic) or 3 (Manual)
                        key.SetValue(RegistryValueName, enable ? 2 : 3, RegistryValueKind.DWord);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting auto-start: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
