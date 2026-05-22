using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using VM.Core;
using VM.PlatformSDKCS;
using TripleDetection.Services;
using TripleDetection.ViewModels;
using MessageBox = System.Windows.MessageBox;

namespace TripleDetection
{
    public partial class MainWindow : Window
    {
        private VmIntegrationService _vmService;
        private ImageStorageService _imageStorage;
        private LoggingService _logService;
        private MainViewModel _viewModel;

        private string _solPath;
        private readonly string _okDir;
        private readonly string _ngDir;
        private string _selectedSolPath;
        private bool _isSolutionLoad = false;
        private bool _isContinuRun = false;
        private VmProcedure _procedure;

        private readonly string _vmInstallPath;

        public MainWindow()
        {
            _vmInstallPath = ConfigurationManager.AppSettings["VmInstallPath"];

            // 注册 AssemblyResolve 事件，在 GAC 找不到时从配置的 VM 路径加载
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            InitializeComponent();

            _okDir = ConfigurationManager.AppSettings["OkImageDir"];
            _ngDir = ConfigurationManager.AppSettings["NgImageDir"];
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", "Message");

            _imageStorage = new ImageStorageService(_okDir, _ngDir);
            _logService = new LoggingService(logPath);
            _vmService = new VmIntegrationService(_imageStorage);

            _viewModel = new MainViewModel();
            _logService.OnLogAdded += (s, e) =>
            {
                Dispatcher.Invoke(() => _viewModel.AddLog(e.Message));
            };

            this.DataContext = _viewModel;

            VmSolution.OnWorkStatusEvent += VmSolution_OnWorkStatusEvent;
            VmSolution.OnProcessStatusStartEvent += VmSolution_OnProcessStatusStartEvent;
            VmSolution.OnProcessStatusStopEvent += VmSolution_OnProcessStatusStopEvent;

            btnRender.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(255, 140, 0));
        }

        private System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new System.Reflection.AssemblyName(args.Name);
            string dllName = assemblyName.Name + ".dll";

            // VM 相关的 DLL 从配置的安装路径加载
            string[] vmDlls = new[] { "VM.Core", "VM.PlatformSDKCS", "VMControls.BaseInterface",
                "VMControls.Interface", "VMControls.RenderInterface", "VMControls.Winform.Release",
                "VMControls.WPF.Release", "VM.Framework.Container", "VM.Util", "VM.Utility",
                "Apps.Data", "Apps.ErrorCode", "Apps.Interface", "Apps.Localization", "Apps.Log",
                "Apps.UIData", "Apps.UIHelper", "MVDCore.Net", "MVDImage.Net" };

            if (Array.Exists(vmDlls, d => d == assemblyName.Name))
            {
                string vmPath = Path.Combine(_vmInstallPath, "Development", "V4.x", "ComControls", "Assembly", dllName);
                if (File.Exists(vmPath))
                {
                    _logService.Log($"Loading {dllName} from {vmPath}");
                    return System.Reflection.Assembly.LoadFrom(vmPath);
                }
                else
                {
                    _logService.Log($"Assembly {dllName} not found at {vmPath}");
                }
            }
            return null;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ShowRenderControl();
            _logService.Log("Application started");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isSolutionLoad && _procedure != null)
            {
                var result = MessageBox.Show("Save solution or not?", "Information",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _procedure.ContinuousRunEnable = false;
                    VmSolution.Save();
                }
            }

            VmSolution.OnWorkStatusEvent -= VmSolution_OnWorkStatusEvent;
            VmSolution.OnProcessStatusStartEvent -= VmSolution_OnProcessStatusStartEvent;
            VmSolution.OnProcessStatusStopEvent -= VmSolution_OnProcessStatusStopEvent;
        }

        private void ShowRenderControl()
        {
            VmHost.Child = new VMControls.Winform.Release.VmRenderControl();
            if (_procedure != null)
            {
                ((VMControls.Winform.Release.VmRenderControl)VmHost.Child).ModuleSource = _procedure;
            }
            btnRender.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(255, 140, 0));
            btnConfig.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(128, 128, 128));
            _viewModel.IsImageViewActive = true;
        }

        private void ShowMainViewControl()
        {
            if (VmHost.Child is IDisposable disposable)
            {
                disposable.Dispose();
            }
            VmHost.Child = null;
            btnConfig.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(255, 140, 0));
            btnRender.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(128, 128, 128));
            _viewModel.IsImageViewActive = false;
            _logService.Log("Switched to parameter configuration view");
        }

        private void BtnSelectSolu_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "VM Sol File|*.sol*";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _selectedSolPath = dialog.FileName;
                _isSolutionLoad = false;
                _logService.Log("Selected solution: " + _selectedSolPath);
                MessageBox.Show("Solution path: " + _selectedSolPath + "\nNext click Load button!",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnLoadSolu_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedSolPath))
            {
                MessageBox.Show("Please select a solution file first!", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetControlsEnabled(false);

            try
            {
                if (_isSolutionLoad)
                {
                    _isSolutionLoad = false;
                }

                VmSolution.Load(_selectedSolPath);
                _isSolutionLoad = true;

                _logService.Log("Loading solution succeeded!");
                MessageBox.Show("Loading Solution succeeded!", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                comboProcedure.Items.Clear();
                ProcessInfoList processList = VmSolution.Instance.GetAllProcedureList();
                for (int i = 0; i < processList.nNum; i++)
                {
                    comboProcedure.Items.Add(processList.astProcessInfo[i].strProcessName);
                }

                if (comboProcedure.Items.Count > 0)
                {
                    comboProcedure.SelectedIndex = 0;
                    _procedure = VmSolution.Instance[processList.astProcessInfo[0].strProcessName] as VmProcedure;

                    if (_procedure == null)
                    {
                        _logService.Log("Procedure is null, check the solution!");
                        return;
                    }

                    var vmRender = VmHost.Child as VMControls.Winform.Release.VmRenderControl;
                    if (vmRender != null)
                        vmRender.ModuleSource = _procedure;
                }
                else
                {
                    _logService.Log("Number of flows is 0, check the solution!");
                }
            }
            catch (VmException ex)
            {
                _logService.Log($"Failed to load solution, Error code: 0x{ex.errorCode:X}");
                MessageBox.Show($"Failed to load solution: 0x{ex.errorCode:X}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                _logService.Log($"Failed to load solution: {ex.Message}");
                MessageBox.Show($"Failed to load solution: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetControlsEnabled(true);
            }
        }

        private void BtnSaveSolu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                VmSolution.Save();
                _logService.Log("Succeeded to save solution!");
            }
            catch (VmException ex)
            {
                _logService.Log($"Failed to save solution, Error code: 0x{ex.errorCode:X}");
            }
            catch (Exception ex)
            {
                _logService.Log($"Failed to save solution: {ex.Message}");
            }
        }

        private void ComboProcedure_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (comboProcedure.SelectedItem == null) return;

            try
            {
                _procedure = VmSolution.Instance[comboProcedure.SelectedItem.ToString()] as VmProcedure;
                var vmRender = VmHost.Child as VMControls.Winform.Release.VmRenderControl;
                if (vmRender != null)
                    vmRender.ModuleSource = _procedure;

                _logService.Log($"Selected [{comboProcedure.SelectedItem}] succeeded!");
            }
            catch (VmException ex)
            {
                _logService.Log($"Failed to select procedure, Error code: 0x{ex.errorCode:X}");
            }
            catch (Exception ex)
            {
                _logService.Log($"Failed to select procedure: {ex.Message}");
            }
        }

        private void BtnRunOnce_Click(object sender, RoutedEventArgs e)
        {
            if (!_isSolutionLoad || _procedure == null)
            {
                _logService.Log("The procedure does not exist!");
                return;
            }

            try
            {
                _procedure.Run();
                _logService.Log("Run once triggered");
            }
            catch (VmException ex)
            {
                _logService.Log($"Failed to run once, Error code: 0x{ex.errorCode:X}");
            }
            catch (Exception ex)
            {
                _logService.Log($"Failed to run once: {ex.Message}");
            }
        }

        private void BtnContiRun_Click(object sender, RoutedEventArgs e)
        {
            if (!_isSolutionLoad || _procedure == null)
            {
                _logService.Log("The procedure does not exist!");
                return;
            }

            try
            {
                bool beforeToggle = _procedure.ContinuousRunEnable;
                _procedure.ContinuousRunEnable = _procedure.ContinuousRunEnable ^ true;
                _isContinuRun = _procedure.ContinuousRunEnable;

                _logService.Log($"Continuous run toggled: {beforeToggle} -> {_procedure.ContinuousRunEnable}, isContinuRun={_isContinuRun}");

                // 更新按钮文本以反映当前实际状态
                btnContiRun.Content = _isContinuRun ? "停止连续" : "连续运行";
            }
            catch (VmException ex)
            {
                _logService.Log($"Failed to toggle continuous run, Error code: 0x{ex.errorCode:X}");
            }
            catch (Exception ex)
            {
                _logService.Log($"Failed to toggle continuous run: {ex.Message}");
            }
        }

        private void BtnRender_Click(object sender, RoutedEventArgs e)
        {
            ShowRenderControl();
        }

        private void BtnConfig_Click(object sender, RoutedEventArgs e)
        {
            ShowMainViewControl();
        }

        private void BtnLang_Click(object sender, RoutedEventArgs e)
        {
            if (System.Threading.Thread.CurrentThread.CurrentUICulture.Name == "zh-CN")
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");
            }
            else
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-CN");
            }
            _logService.Log("Language switched");
        }

        private void VmSolution_OnWorkStatusEvent(ImvsSdkDefine.IMVS_MODULE_WORK_STAUS workStatusInfo)
        {
            _logService.Log($"[Callback] nWorkStatus={workStatusInfo.nWorkStatus}, nProcessID={workStatusInfo.nProcessID}");
            if (workStatusInfo.nWorkStatus == 0 && workStatusInfo.nProcessID == 10000)
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (_procedure == null)
                        {
                            _logService.Log("VmSolution_OnWorkStatusEvent: _procedure is null");
                            return;
                        }
                        _logService.Log("VmSolution_OnWorkStatusEvent: getting output info...");
                        var ioNameInfos = _procedure.ModuResult.GetAllOutputNameInfo();
                        _logService.Log($"VmSolution_OnWorkStatusEvent: ioNameInfos.Count={ioNameInfos.Count}");

                        if (ioNameInfos.Count == 0)
                        {
                            _logService.Log("VmSolution_OnWorkStatusEvent: no outputs available");
                            return;
                        }

                        _logService.Log($"VmSolution_OnWorkStatusEvent: TypeName={ioNameInfos[0].TypeName}");
                        if (ioNameInfos[0].TypeName != IMVS_MODULE_BASE_DATA_TYPE.IMVS_GRAP_TYPE_STRING)
                        {
                            _logService.Log($"VmSolution_OnWorkStatusEvent: type mismatch, got {ioNameInfos[0].TypeName}");
                            return;
                        }

                        string outputName = ioNameInfos[0].Name;
                        _logService.Log("VmSolution_OnWorkStatusEvent: calling GetOutputString with name=" + outputName);

                        // 直接在 Dispatcher 线程调用，不要 Task.Run，避免跨线程问题
                        var outputResult = _procedure.ModuResult.GetOutputString(outputName);
                        _logService.Log("VmSolution_OnWorkStatusEvent: GetOutputString succeeded");

                        var stringVal = outputResult.astStringVal;
                        if (stringVal == null || stringVal.Length == 0)
                        {
                            _logService.Log("VmSolution_OnWorkStatusEvent: stringVal is null or empty");
                            return;
                        }
                        string strResult = stringVal[0].strValue;
                        if (strResult != null)
                        {
                            UpdateResult(strResult);
                            _logService.Log($"Process running time: {_procedure.ProcessTime}ms");
                        }
                        else
                        {
                            _logService.Log("VmSolution_OnWorkStatusEvent: strResult is null");
                        }
                    }
                    catch (VmException ex)
                    {
                        _logService.Log($"VmSolution_OnWorkStatusEvent VmException: Error code: 0x{ex.errorCode:X}");
                    }
                    catch (Exception ex)
                    {
                        _logService.Log($"VmSolution_OnWorkStatusEvent Exception: {ex.Message}");
                    }
                });
            }
        }

        private void VmSolution_OnProcessStatusStartEvent(ImvsSdkDefine.IMVS_STATUS_PROCESS_START_CONTINUOUSLY_INFO statusInfo)
        {
            if (statusInfo.nStatus == 0)
            {
                Dispatcher.Invoke(() =>
                {
                    _isContinuRun = true;
                    btnContiRun.Content = "停止连续";
                    SetControlsEnabled(false);
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
                    _isContinuRun = false;
                    btnContiRun.Content = "连续运行";
                    SetControlsEnabled(true);
                    _logService.Log("End Run!");
                });
            }
        }

        private void UpdateResult(string strResult)
        {
            var vs = strResult.Split(';');
            if (vs.Length < 4) return;

            if (vs[0] == "1")
            {
                _viewModel.ResultText = "OK";
                _viewModel.ResultBackground = "#00C000";
            }
            else
            {
                _viewModel.ResultText = "NG";
                _viewModel.ResultBackground = "#FF0000";
            }

            string result = $"Results: CodeInfo: {vs[2]}; Number of characters: {vs[1]}; Confidence: {vs[3]}";
            _viewModel.AddResult(result);
            _logService.Log(result);
        }

        private void SetControlsEnabled(bool enabled)
        {
            btnSelectSolu.IsEnabled = enabled;
            btnLoadSolu.IsEnabled = enabled;
            btnSaveSolu.IsEnabled = enabled;
            btnRunOnce.IsEnabled = enabled;
            btnContiRun.IsEnabled = enabled;
            comboProcedure.IsEnabled = enabled;
            btnRender.IsEnabled = enabled;
            btnConfig.IsEnabled = enabled;
        }
    }
}