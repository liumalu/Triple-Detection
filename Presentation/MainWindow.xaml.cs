using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using VM.Core;
using VM.PlatformSDKCS;
using TripleDetection.Application.VmServices;
using TripleDetection.Application.Services;
using TripleDetection.Presentation.ViewModels.Detection;
using TripleDetection.Presentation.Navigation;
using TripleDetection.Presentation.Views.Detection;
using MessageBox = System.Windows.MessageBox;
using ConfigurationManager = System.Configuration.ConfigurationManager;

namespace TripleDetection
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly NavigationService _navigationService;
        private readonly VmIntegrationService _vmService;
        private readonly ImageStorageService _imageStorage;
        private readonly LoggingService _logService;

        private string? _selectedSolPath;
        private bool _isSolutionLoad = false;
        private bool _isContinuRun = false;
        private VmProcedure? _procedure;

        public MainWindow(
            MainViewModel viewModel,
            NavigationService navigationService,
            VmIntegrationService vmService,
            ImageStorageService imageStorage,
            LoggingService logService)
        {
            try
            {
                InitializeComponent();
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "startup.log"),
                    $"\n[{DateTime.Now:HH:mm:ss}] MainWindow constructor started");
                DataContext = viewModel;
                _viewModel = viewModel;
                _navigationService = navigationService;
                _vmService = vmService;
                _imageStorage = imageStorage;
                _logService = logService;

                _logService.OnLogAdded += (s, e) =>
                {
                    Dispatcher.Invoke(() => _viewModel.AddLog(e.Message));
                };

                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "startup.log"),
                    $"\n[{DateTime.Now:HH:mm:ss}] About to register VM events");

                VmSolution.OnWorkStatusEvent += VmSolution_OnWorkStatusEvent;
                VmSolution.OnProcessStatusStartEvent += VmSolution_OnProcessStatusStartEvent;
                VmSolution.OnProcessStatusStopEvent += VmSolution_OnProcessStatusStopEvent;

                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "startup.log"),
                    $"\n[{DateTime.Now:HH:mm:ss}] VM events registered, setting button background");

                btnRender.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(255, 140, 0));

                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "startup.log"),
                    $"\n[{DateTime.Now:HH:mm:ss}] MainWindow constructor complete");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "startup.log"),
                    $"\n[{DateTime.Now:HH:mm:ss}] MainWindow constructor EXCEPTION: {ex}");
                throw;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _logService.Log("Application started");
            _navigationService.SetRegion(MainContentRegion);
            _navigationService.RegisterRoute("Detection", typeof(DetectionView));
            _navigationService.NavigateTo<DetectionView>("Detection");
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
            VmHost.Visibility = Visibility.Visible;
            MainContentRegion.Visibility = Visibility.Collapsed;
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
            VmHost.Visibility = Visibility.Collapsed;
            MainContentRegion.Visibility = Visibility.Visible;
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
            if (dialog.ShowDialog() == true)
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
                _logService.Log($"Continuous run toggled: {beforeToggle} -> {_procedure.ContinuousRunEnable}");
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
                            _logService.Log("VmSolution_OnWorkStatusEvent: ioNameInfos.Count == 0, no output available");
                            return;
                        }
                        _logService.Log($"VmSolution_OnWorkStatusEvent: TypeName={ioNameInfos[0].TypeName}");
                        if (ioNameInfos[0].TypeName != IMVS_MODULE_BASE_DATA_TYPE.IMVS_GRAP_TYPE_STRING)
                        {
                            _logService.Log($"VmSolution_OnWorkStatusEvent: type mismatch, got {ioNameInfos[0].TypeName}");
                            return;
                        }
                        _logService.Log("VmSolution_OnWorkStatusEvent: calling GetOutputString with name=" + ioNameInfos[0].Name);
                        Task.Run(() =>
                        {
                            try
                            {
                                var outputResult = _procedure.ModuResult.GetOutputString(ioNameInfos[0].Name);
                                var stringVal = outputResult.astStringVal;
                                if (stringVal == null || stringVal.Length == 0)
                                {
                                    _logService.Log("VmSolution_OnWorkStatusEvent: stringVal is null or empty");
                                    return;
                                }
                                string strResult = stringVal[0].strValue;
                                if (strResult != null)
                                {
                                    Dispatcher.Invoke(() => UpdateResult(strResult));
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