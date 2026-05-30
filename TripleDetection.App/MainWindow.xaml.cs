using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Prism.Commands;
using Prism.Regions;
using VM.Core;
using VM.PlatformSDKCS;
using TripleDetection.App.Services.System;
using TripleDetection.ViewModels;
using TripleDetection.ViewModels.Detection;
using TripleDetection.App;
using MessageBox = System.Windows.MessageBox;

namespace TripleDetection
{
    public partial class MainWindow : Window
    {
        private LoggingService _logService;
        private MainViewModel _viewModel;
        private IRegionManager _regionManager;
        private ObservableCollection<TabItemViewModel> _tabItems = new ObservableCollection<TabItemViewModel>();

        private bool _isNavExpanded = true;
        private readonly Dictionary<string, System.Windows.Controls.Button> _navButtons = new Dictionary<string, System.Windows.Controls.Button>();

        private readonly string _vmInstallPath;
        private readonly string _localLibsPath;

        public MainWindow() : this(null)
        {
        }

        public MainWindow(IRegionManager regionManager)
        {
            _vmInstallPath = ConfigurationManager.AppSettings["VmInstallPath"];
            _localLibsPath = ConfigurationManager.AppSettings["LocalLibsPath"];

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            InitializeComponent();

            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            _logService = new LoggingService(logPath);

            _viewModel = new MainViewModel();
            _logService.OnLogAdded += (s, e) =>
            {
                Dispatcher.Invoke(() => _viewModel.AddLog(e.Message));
            };

            // Use Prism's IRegionManager for navigation (resolved from container if not injected)
            _regionManager = regionManager;

            tabBar.ItemsSource = _tabItems;

            this.DataContext = _viewModel;

            _navButtons["Dashboard"] = btnNavDashboard;
            _navButtons["Detection"] = btnNavDetection;
            _navButtons["Products"] = btnNavProducts;
            _navButtons["Tasks"] = btnNavTasks;
            _navButtons["Logs"] = btnNavLogs;
            _navButtons["Settings"] = btnNavSettings;
            _navButtons["UserManagement"] = btnNavUserManagement;
            _navButtons["AuditLog"] = btnNavAuditLog;
            _navButtons["DetectionHistory"] = btnNavDetectionHistory;

            LoadConfiguration();

            VmSolution.OnWorkStatusEvent += VmSolution_OnWorkStatusEvent;
            VmSolution.OnProcessStatusStartEvent += VmSolution_OnProcessStatusStartEvent;
            VmSolution.OnProcessStatusStopEvent += VmSolution_OnProcessStatusStopEvent;
        }

        private System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new System.Reflection.AssemblyName(args.Name);
            string dllName = assemblyName.Name + ".dll";

            string[] vmDlls = new[] { "VM.Core", "VM.PlatformSDKCS", "VMControls.BaseInterface",
                "VMControls.Interface", "VMControls.RenderInterface", "VMControls.Winform.Release",
                "VMControls.WPF.Release", "VM.Framework.Container", "VM.Util", "VM.Utility",
                "Apps.Data", "Apps.ErrorCode", "Apps.Interface", "Apps.Localization", "Apps.Log",
                "Apps.UIData", "Apps.UIHelper", "MVDCore.Net", "MVDImage.Net" };

            if (!Array.Exists(vmDlls, d => d == assemblyName.Name))
                return null;

            string localLibsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _localLibsPath, dllName);
            if (File.Exists(localLibsPath))
            {
                _logService.Log($"Loading {dllName} from local libs");
                return System.Reflection.Assembly.LoadFrom(localLibsPath);
            }

            string vmPath = Path.Combine(_vmInstallPath, "Development", "V4.x", "ComControls", "Assembly", dllName);
            if (File.Exists(vmPath))
            {
                _logService.Log($"Loading {dllName} from VM path");
                return System.Reflection.Assembly.LoadFrom(vmPath);
            }

            _logService.Log($"Error: {dllName} not found in libs or VM path");
            return null;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                DatabaseConfig.Initialize();
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException ?? ex;
                var errMsg = $"数据库初始化失败: {inner.GetType().Name}\n{inner.Message}\n";
                if (inner is System.IO.FileNotFoundException fnf)
                {
                    errMsg += $"FileName: {fnf.FileName}\n";
                    errMsg += $"FusionLog: {fnf.FusionLog}\n";
                }
                errMsg += $"\nStack:\n{inner.StackTrace}";
                System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db_init_error.log"), errMsg);
                MessageBox.Show(errMsg, "数据库初始化错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            var logoPath = ConfigurationManager.AppSettings["SystemLogoPath"];
            var systemName = ConfigurationManager.AppSettings["SystemName"];
            if (!string.IsNullOrEmpty(logoPath))
            {
                try { imgLogo.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(logoPath, UriKind.Relative)); } catch { }
            }
            if (!string.IsNullOrEmpty(systemName))
            {
                txtSystemName.Text = systemName;
            }

            UpdateStatusBar();
            _logService.Log("Application started");

            // Navigate to Dashboard using IRegionManager
            if (_regionManager != null)
                _regionManager.RequestNavigate("MainContentRegion", "Dashboard");
            OpenTab("Dashboard", "📊 仪表盘");
            StartStatusBarTimer();
        }

        private void StartStatusBarTimer()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) => UpdateStatusBar();
            timer.Start();
        }

        private void UpdateStatusBar()
        {
            txtCurrentTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            txtCurrentUser.Text = $"当前用户: {txtUsername.Text}";
        }

        private void BtnNav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string tag)
            {
                NavigateToView(tag);
            }
        }

        private void NavigateToView(string tag)
        {
            if (_regionManager != null)
                _regionManager.RequestNavigate("MainContentRegion", tag);

            UpdateNavButtonStyles(tag);
            OpenTab(tag, GetViewDisplayName(tag));
            txtStatus.Text = $"当前: {tag}";
            _logService.Log($"导航到: {tag}");
        }

        private string GetViewDisplayName(string tag)
        {
            switch (tag)
            {
                case "Dashboard": return "📊 仪表盘";
                case "Detection": return "🔍 检测执行";
                case "Products": return "📦 产品管理";
                case "Tasks": return "📋 任务管理";
                case "Logs": return "📝 操作日志";
                case "Settings": return "⚙️ 系统配置";
                case "UserManagement": return "👤 用户权限";
                case "AuditLog": return "📋 审计日志";
                case "DetectionHistory": return "📜 检测记录";
                default: return tag;
            }
        }

        private void OpenTab(string tag, string displayName)
        {
            // Check if tab already exists
            var existing = _tabItems.FirstOrDefault(t => t.Tag == tag);
            if (existing != null)
            {
                // Activate existing tab
                foreach (var item in _tabItems)
                    item.IsActive = item.Tag == tag;
                return;
            }

            // Create new tab item
            foreach (var item in _tabItems)
                item.IsActive = false;

            _tabItems.Add(new TabItemViewModel
            {
                Tag = tag,
                DisplayName = displayName,
                IsActive = true,
                IsClosable = tag != "Dashboard",
                SelectCommand = new DelegateCommand<object>(param => SelectTab(param as string)),
                CloseCommand = new DelegateCommand<object>(param => CloseTab(param as string))
            });
        }

        private void SelectTab(string tag)
        {
            if (!string.IsNullOrEmpty(tag))
            {
                NavigateToView(tag);
            }
        }

        private void CloseTab(string tag)
        {
            if (string.IsNullOrEmpty(tag) || _tabItems.Count <= 1)
                return;

            var item = _tabItems.FirstOrDefault(t => t.Tag == tag);
            if (item != null)
            {
                bool wasActive = item.IsActive;
                _tabItems.Remove(item);

                if (wasActive)
                {
                    // Navigate to first remaining tab
                    var nextTab = _tabItems.FirstOrDefault();
                    if (nextTab != null)
                    {
                        NavigateToView(nextTab.Tag);
                    }
                }
            }
        }

        private void UpdateNavButtonStyles(string activeTag)
        {
            foreach (var kvp in _navButtons)
            {
                kvp.Value.Style = kvp.Key == activeTag
                    ? (System.Windows.Style)FindResource("NavButtonActiveStyle")
                    : (System.Windows.Style)FindResource("NavButtonStyle");
            }
        }

        private void BtnToggleNav_Click(object sender, RoutedEventArgs e)
        {
            _isNavExpanded = !_isNavExpanded;
            if (_isNavExpanded)
            {
                navColumn.Width = new GridLength(200);
                foreach (var btn in _navButtons.Values)
                {
                    btn.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;
                }
            }
            else
            {
                navColumn.Width = new GridLength(48);
                foreach (var btn in _navButtons.Values)
                {
                    btn.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
                }
            }
        }

        private void BtnNotifications_Click(object sender, RoutedEventArgs e)
        {
            NavigateToView("Logs");
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("确认退出系统？", "确认",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _logService.Log("用户退出系统");
                System.Windows.Application.Current.Shutdown();
            }
        }

        private void LoadConfiguration()
        {
            var navExpanded = ConfigurationManager.AppSettings["NavRailExpanded"];
            if (navExpanded == "false")
            {
                _isNavExpanded = false;
                navColumn.Width = new GridLength(48);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (VmSolution.Instance != null)
                {
                    var processList = VmSolution.Instance.GetAllProcedureList();
                    for (int i = 0; i < processList.nNum; i++)
                    {
                        var procedure = VmSolution.Instance[processList.astProcessInfo[i].strProcessName] as VmProcedure;
                        if (procedure?.ContinuousRunEnable == true)
                        {
                            procedure.ContinuousRunEnable = false;
                        }
                    }

                    VmSolution.Instance.CloseSolution();
                }

                _logService.Log("VM 资源已释放");
            }
            catch (Exception ex)
            {
                _logService.Log($"关闭时清理 VM 资源出错: {ex.Message}");
            }

            VmSolution.OnWorkStatusEvent -= VmSolution_OnWorkStatusEvent;
            VmSolution.OnProcessStatusStartEvent -= VmSolution_OnProcessStatusStartEvent;
            VmSolution.OnProcessStatusStopEvent -= VmSolution_OnProcessStatusStopEvent;
        }

        private void VmSolution_OnWorkStatusEvent(ImvsSdkDefine.IMVS_MODULE_WORK_STAUS workStatusInfo)
        {
            _logService.Log($"[Callback] nWorkStatus={workStatusInfo.nWorkStatus}, nProcessID={workStatusInfo.nProcessID}");
        }

        private void VmSolution_OnProcessStatusStartEvent(ImvsSdkDefine.IMVS_STATUS_PROCESS_START_CONTINUOUSLY_INFO statusInfo)
        {
            if (statusInfo.nStatus == 0)
            {
                Dispatcher.Invoke(() =>
                {
                    txtStatus.Text = "连续运行中...";
                    _logService.Log("Start continuous run!");
                });
            }
        }

        private void VmSolution_OnProcessStatusStopEvent(ImvsSdkDefine.IMVS_STATUS_PROCESS_STOP_INFO statusInfo)
        {
            if (statusInfo.nStopAction == 1)
            {
                Dispatcher.Invoke(() =>
                {
                    txtStatus.Text = "已停止";
                    _logService.Log("End Run!");
                });
            }
        }
    }
}
