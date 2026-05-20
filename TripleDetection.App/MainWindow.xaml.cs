using System;
using System.Globalization;
using System.IO;
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

        public MainWindow()
        {
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
            if (dialog.ShowDialog() == DialogResult.OK)
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
                _procedure.ContinuousRunEnable = _procedure.ContinuousRunEnable ^ true;
                _isContinuRun = _isContinuRun ^ true;
            }
            catch (VmException ex)
            {
                _logService.Log($"Failed to run continuous, Error code: 0x{ex.errorCode:X}");
            }
            catch (Exception ex)
            {
                _logService.Log($"Failed to run continuous: {ex.Message}");
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
            if (workStatusInfo.nWorkStatus == 0 && workStatusInfo.nProcessID == 10000)
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (_procedure == null) return;
                        var ioNameInfos = _procedure.ModuResult.GetAllOutputNameInfo();
                        if (ioNameInfos.Count != 0 && ioNameInfos[0].TypeName == IMVS_MODULE_BASE_DATA_TYPE.IMVS_GRAP_TYPE_STRING)
                        {
                            var stringVal = _procedure.ModuResult.GetOutputString(ioNameInfos[0].Name).astStringVal;
                            if (stringVal == null || stringVal.Length == 0) return;
                            string strResult = stringVal[0].strValue;
                            if (strResult != null)
                            {
                                UpdateResult(strResult);
                                _logService.Log($"Process running time: {_procedure.ProcessTime}ms");
                            }
                        }
                    }
                    catch (VmException ex)
                    {
                        _logService.Log($"Failed to get results, Error code: 0x{ex.errorCode:X}");
                    }
                    catch (Exception ex)
                    {
                        _logService.Log($"Failed to get results: {ex.Message}");
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