using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using VM.Core;
using VM.PlatformSDKCS;
using TripleDetection.Services;
using TripleDetection.ViewModels;
using MessageBox = System.Windows.MessageBox;

namespace TripleDetection
{
    public partial class MainWindow : Window
    {
        private LoggingService _logService;
        private MainViewModel _viewModel;
        private TabManager _tabManager;
        private ObservableCollection<TabItemViewModel> _tabItems = new ObservableCollection<TabItemViewModel>();

        private bool _isNavExpanded = true;
        private readonly Dictionary<string, System.Windows.Controls.Button> _navButtons = new Dictionary<string, System.Windows.Controls.Button>();

        private readonly string _vmInstallPath;
        private readonly string _localLibsPath;

        public MainWindow()
        {
            _vmInstallPath = ConfigurationManager.AppSettings["VmInstallPath"];
            _localLibsPath = ConfigurationManager.AppSettings["LocalLibsPath"];

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            InitializeComponent();

            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", "Message");
            _logService = new LoggingService(logPath);

            _viewModel = new MainViewModel();
            _logService.OnLogAdded += (s, e) =>
            {
                Dispatcher.Invoke(() => _viewModel.AddLog(e.Message));
            };

            _tabManager = new TabManager();
            _tabManager.ViewOpened += OnViewOpened;
            _tabManager.ViewClosed += OnViewClosed;
            _tabManager.ActiveViewChanged += OnActiveViewChanged;

            tabBar.ItemsSource = _tabItems;

            this.DataContext = _viewModel;

            _navButtons["Dashboard"] = btnNavDashboard;
            _navButtons["Detection"] = btnNavDetection;
            _navButtons["Products"] = btnNavProducts;
            _navButtons["Tasks"] = btnNavTasks;
            _navButtons["Logs"] = btnNavLogs;
            _navButtons["Settings"] = btnNavSettings;
            _navButtons["UserManagement"] = btnNavUserManagement;

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
            // TODO: 初始化数据库 (临时禁用，待解决 DatabaseConfig 引用问题)
            // DatabaseConfig.Initialize();

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

            _tabManager.OpenView("Dashboard");
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
                _tabManager.OpenView(tag);
                UpdateNavButtonStyles(tag);
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

        private void OnViewOpened(object sender, KeyValuePair<string, System.Windows.Controls.UserControl> e)
        {
            var tabItem = new TabItemViewModel
            {
                Tag = e.Key,
                DisplayName = _tabManager.GetViewName(e.Key),
                IsActive = true,
                IsClosable = e.Key != "Dashboard",
                SelectCommand = new RelayCommand(param => SelectTab(param as string)),
                CloseCommand = new RelayCommand(param => CloseTab(param as string))
            };

            foreach (var item in _tabItems)
            {
                item.IsActive = false;
            }

            _tabItems.Add(tabItem);
            MainContent.Content = e.Value;
        }

        private void OnViewClosed(object sender, KeyValuePair<string, System.Windows.Controls.UserControl> e)
        {
            var item = _tabItems.FirstOrDefault(t => t.Tag == e.Key);
            if (item != null)
            {
                _tabItems.Remove(item);
            }
        }

        private void OnActiveViewChanged(object sender, string e)
        {
            foreach (var item in _tabItems)
            {
                item.IsActive = item.Tag == e;
            }

            var views = _tabManager.GetOpenViews();
            if (views.ContainsKey(e))
            {
                MainContent.Content = views[e];
            }

            UpdateNavButtonStyles(e);
            txtStatus.Text = $"当前: {e}";
            _logService.Log($"导航到: {e}");
        }

        private void SelectTab(string tag)
        {
            if (!string.IsNullOrEmpty(tag))
            {
                _tabManager.ActivateView(tag);
            }
        }

        private void CloseTab(string tag)
        {
            if (!string.IsNullOrEmpty(tag) && _tabItems.Count > 1)
            {
                _tabManager.CloseView(tag);
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
            _tabManager.OpenView("Logs");
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
                // 1. 停止所有流程的连续运行
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

                    // 2. 关闭方案（释放相机连接）
                    VmSolution.Instance.CloseSolution();
                }

                _logService.Log("VM 资源已释放");
            }
            catch (Exception ex)
            {
                _logService.Log($"关闭时清理 VM 资源出错: {ex.Message}");
            }

            // 3. 注销事件订阅
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

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter) => _execute(parameter);

        public event EventHandler CanExecuteChanged;
    }
}