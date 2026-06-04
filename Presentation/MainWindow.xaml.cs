using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Prism.Ioc;
using TripleDetection.Application.Services;
using TripleDetection.Application.VmServices;
using TripleDetection.Domain.Repositories;
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
        private readonly IContainerProvider _container;
        private readonly DispatcherTimer _clockTimer;
        private DispatcherTimer _ioStatusTimer;
        private IIODeviceService _ioService;

        private bool _isNavExpanded = true;
        private readonly Dictionary<string, Button> _navButtons = new Dictionary<string, Button>();
        private readonly Dictionary<string, TabItem> _openTabs = new Dictionary<string, TabItem>();
        private TabItem _dashboardTab;

        public MainWindow(
            MainViewModel viewModel,
            NavigationService navigationService,
            LoggingService logService,
            IContainerProvider container)
        {
            InitializeComponent();

            DataContext = viewModel;
            _viewModel = viewModel;
            _navigationService = navigationService;
            _logService = logService;
            _container = container;

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

            // Setup IO status timer
            _ioService = _container.Resolve<IIODeviceService>();
            _ioStatusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _ioStatusTimer.Tick += (s, args) => UpdateIOStatus();
            _ioStatusTimer.Start();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "startup.log"),
                $"\n[{DateTime.Now:HH:mm:ss}] MainWindow Window_Loaded fired");

            _logService.Log("Application started");

            // Initialize dashboard tab (fixed, not closeable)
            InitializeDashboardTab();
        }

        private void UpdateIOStatus()
        {
            if (_ioService == null) return;
            txtIOStatus.Text = _ioService.IsConnected ? "IO: 已连接" : "IO: 未连接";
            txtIOStatus.Foreground = _ioService.IsConnected
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(72, 187, 120))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 62, 62));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _clockTimer.Stop();
            _ioStatusTimer?.Stop();

            var result = MessageBox.Show("确认退出系统？", "确认",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                _clockTimer.Start();
                return;
            }

            foreach (var tab in _openTabs.Values)
            {
                if (tab.Content is IDisposable disposable)
                    disposable.Dispose();
            }
            _openTabs.Clear();
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

            // Check if tab is already open
            if (_openTabs.TryGetValue(pageName, out var existingTab))
            {
                mainTabControl.SelectedItem = existingTab;
                _logService.Log($"Navigated to: {pageName} (existing tab)");
                return;
            }

            // Create new view using DI
            UserControl view;
            string tabHeader = pageName;
            switch (pageName)
            {
                case "Dashboard":
                    view = _container.Resolve<TripleDetection.Presentation.Views.App.DashboardView>();
                    tabHeader = "仪表盘";
                    break;
                case "Detection":
                    view = _container.Resolve<TripleDetection.Presentation.Views.Detection.DetectionView>();
                    tabHeader = "检测执行";
                    break;
                case "Products":
                    view = _container.Resolve<TripleDetection.Presentation.Views.Production.ProductListView>();
                    tabHeader = "产品管理";
                    break;
                case "Tasks":
                    view = _container.Resolve<TripleDetection.Presentation.Views.Production.TaskListView>();
                    tabHeader = "任务管理";
                    break;
                case "Audit":
                    view = _container.Resolve<TripleDetection.Presentation.Views.Audit.AuditLogView>();
                    tabHeader = "审计日志";
                    break;
                case "Users":
                    view = _container.Resolve<TripleDetection.Presentation.Views.Auth.UserManagementView>();
                    tabHeader = "用户管理";
                    break;
                case "Settings":
                    view = _container.Resolve<TripleDetection.Presentation.Views.SettingsView>();
                    tabHeader = "系统配置";
                    break;
                default:
                    view = _container.Resolve<TripleDetection.Presentation.Views.App.DashboardView>();
                    tabHeader = "仪表盘";
                    break;
            }

            // Create tab item with close button
            var tabItem = new TabItem
            {
                Header = CreateTabHeader(tabHeader, pageName),
                Content = view
            };
            // Mark as closeable (not Dashboard)
            tabItem.Tag = pageName;

            mainTabControl.Items.Add(tabItem);
            _openTabs[pageName] = tabItem;
            mainTabControl.SelectedItem = tabItem;
            _logService.Log($"Navigated to: {pageName} (new tab)");
        }

        private FrameworkElement CreateTabHeader(string title, string pageName)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var textBlock = new TextBlock
            {
                Text = title,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetColumn(textBlock, 0);
            grid.Children.Add(textBlock);

            // Add close button for non-Dashboard tabs
            if (pageName != "Dashboard")
            {
                var closeBtn = new Button
                {
                    Content = "×",
                    Width = 18,
                    Height = 18,
                    FontSize = 14,
                    Padding = new Thickness(0),
                    Margin = new Thickness(0),
                    Background = System.Windows.Media.Brushes.Transparent,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(160, 174, 192)),
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = pageName
                };
                closeBtn.Click += TabCloseButton_Click;
                Grid.SetColumn(closeBtn, 1);
                grid.Children.Add(closeBtn);
            }

            return grid;
        }

        private void TabCloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string pageName)
            {
                CloseTab(pageName);
            }
        }

        private void CloseTab(string pageName)
        {
            if (_openTabs.TryGetValue(pageName, out var tabItem))
            {
                mainTabControl.Items.Remove(tabItem);
                _openTabs.Remove(pageName);
                _logService.Log($"Tab closed: {pageName}");
            }
        }

        private void InitializeDashboardTab()
        {
            var dashboardView = _container.Resolve<TripleDetection.Presentation.Views.App.DashboardView>();
            _dashboardTab = new TabItem
            {
                Header = CreateTabHeader("仪表盘", "Dashboard"),
                Content = dashboardView,
                Tag = "Dashboard"
            };
            mainTabControl.Items.Add(_dashboardTab);
            _openTabs["Dashboard"] = _dashboardTab;
            mainTabControl.SelectedItem = _dashboardTab;
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