using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using TripleDetection.Application.Services;
using TripleDetection.Application.VmServices;
using TripleDetection.Presentation.ViewModels.Detection;
using TripleDetection.Presentation.Navigation;
using MessageBox = System.Windows.MessageBox;

namespace TripleDetection
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly NavigationService _navigationService;
        private readonly LoggingService _logService;
        private readonly DispatcherTimer _clockTimer;

        private bool _isNavExpanded = true;
        private readonly Dictionary<string, Button> _navButtons = new Dictionary<string, Button>();
        private readonly Dictionary<string, UserControl> _openViews = new Dictionary<string, UserControl>();

        public MainWindow(
            MainViewModel viewModel,
            NavigationService navigationService,
            LoggingService logService)
        {
            InitializeComponent();

            DataContext = viewModel;
            _viewModel = viewModel;
            _navigationService = navigationService;
            _logService = logService;

            // Initialize navigation buttons
            _navButtons["Dashboard"] = btnNavDashboard;
            _navButtons["Detection"] = btnNavDetection;
            _navButtons["Products"] = btnNavProducts;
            _navButtons["Tasks"] = btnNavTasks;
            _navButtons["Audit"] = btnNavAudit;
            _navButtons["Users"] = btnNavUsers;
            _navButtons["Settings"] = btnNavSettings;

            // Setup clock timer
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += (s, e) => txtTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _clockTimer.Start();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "startup.log"),
                $"\n[{DateTime.Now:HH:mm:ss}] MainWindow Window_Loaded fired");

            _logService.Log("Application started");

            // Navigate to Dashboard by default
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "startup.log"),
                $"\n[{DateTime.Now:HH:mm:ss}] About to call NavigateTo(Dashboard)");

            NavigateTo("Dashboard");

            System.IO.File.AppendAllText(
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "startup.log"),
                $"\n[{DateTime.Now:HH:mm:ss}] NavigateTo(Dashboard) completed");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _clockTimer.Stop();

            var result = MessageBox.Show("确认退出系统？", "确认",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                _clockTimer.Start();
                return;
            }

            foreach (var view in _openViews.Values)
            {
                if (view is IDisposable disposable)
                    disposable.Dispose();
            }
            _openViews.Clear();

            var vmService = TripleDetection.Presentation.App.Services.GetRequiredService<VmIntegrationService>();
            vmService.Cleanup();

            _logService.Log("User logged out");
        }

        private void BtnNav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                NavigateTo(tag);
            }
        }

        private void NavigateTo(string pageName)
        {
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "startup.log"),
                $"\n[{DateTime.Now:HH:mm:ss}] NavigateTo({pageName}) called");

            // Update navigation button styles
            foreach (var kvp in _navButtons)
            {
                kvp.Value.Style = kvp.Key == pageName
                    ? (Style)FindResource("NavButtonActiveStyle")
                    : (Style)FindResource("NavButtonStyle");
            }

            // Check if view is already open (Tab behavior)
            if (_openViews.TryGetValue(pageName, out var existingView))
            {
                MainContentRegion.Content = existingView;
                _logService.Log($"Navigated to: {pageName} (existing)");
                return;
            }

            // Create new view using DI
            var services = TripleDetection.Presentation.App.Services;
            UserControl view = pageName switch
            {
                "Dashboard" => services.GetRequiredService<TripleDetection.Presentation.Views.App.DashboardView>(),
                "Detection" => services.GetRequiredService<TripleDetection.Presentation.Views.Detection.DetectionView>(),
                "Products" => services.GetRequiredService<TripleDetection.Presentation.Views.Production.ProductListView>(),
                "Tasks" => services.GetRequiredService<TripleDetection.Presentation.Views.Production.TaskListView>(),
                "Audit" => services.GetRequiredService<TripleDetection.Presentation.Views.Audit.AuditLogView>(),
                "Users" => services.GetRequiredService<TripleDetection.Presentation.Views.Auth.UserManagementView>(),
                "Settings" => services.GetRequiredService<TripleDetection.Presentation.Views.SettingsView>(),
                _ => services.GetRequiredService<TripleDetection.Presentation.Views.App.DashboardView>()
            };

            _openViews[pageName] = view;
            MainContentRegion.Content = view;
            _logService.Log($"Navigated to: {pageName} (new)");
        }

        private void BtnToggleNav_Click(object sender, RoutedEventArgs e)
        {
            _isNavExpanded = !_isNavExpanded;
            if (_isNavExpanded)
            {
                navColumn.Width = new GridLength(200);
                btnToggleNav.Content = "◀";
            }
            else
            {
                navColumn.Width = new GridLength(48);
                btnToggleNav.Content = "▶";
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("确认退出系统？", "确认",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _logService.Log("User logged out");
                System.Windows.Application.Current.Shutdown();
            }
        }
    }
}