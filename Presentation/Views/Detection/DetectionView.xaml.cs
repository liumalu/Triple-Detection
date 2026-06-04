using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Newtonsoft.Json;
using VM.Core;
using TripleDetection.Application.VmServices;
using TripleDetection.Application.Services;
using TripleDetection.Presentation.ViewModels;
using TripleDetection.Presentation.ViewModels.Detection;
using MessageBox = System.Windows.MessageBox;
using TripleDetection.Presentation.Models;
using TaskEntity = TripleDetection.Domain.Entities.ProdTask;
using GlobalVariableModuleCs;
using TripleDetection.Infrastructure.Repositories;
using TripleDetection.Infrastructure.Persistence;
using TripleDetection.Infrastructure.IO;
using IRejectService = TripleDetection.Application.Services.IRejectService;
using SessionManager = TripleDetection.Domain.SessionManager;

namespace TripleDetection.Presentation.Views.Detection
{
    public partial class DetectionView : UserControl, IDisposable
    {
        private bool _isDisposed = false;
        private readonly LoggingService _logService;
        private readonly MainViewModel _viewModel;
        private readonly VmIntegrationService _vmService;
        private readonly ITaskService _taskService;
        private readonly IProductService _productService;
        private readonly IDetectionRecordService _detectionRecordService;
        private readonly IAuditLogService _auditLogService;
        private string _selectedSolPath;
        private bool _isSolutionLoad = false;
        private bool _isContinuRun = false;
        private VmProcedure _procedure;
        private VMControls.Winform.Release.VmRenderControl _vmRender;
        private List<TaskEntity> _taskList = new List<TaskEntity>();
        private TaskEntity _selectedTask;
        private int _okCount = 0;
        private int _ngCount = 0;
        private readonly ModbusTcpIOService _ioService;
        private readonly DeviceControlSettings _deviceSettings;
        private readonly IRejectService _rejectService;

        public DetectionView(
            MainViewModel viewModel,
            LoggingService logService,
            VmIntegrationService vmService,
            ITaskService taskService,
            IProductService productService,
            IDetectionRecordService detectionRecordService,
            IAuditLogService auditLogService,
            ModbusTcpIOService ioService,
            DeviceControlSettings deviceSettings,
            IRejectService rejectService)
        {
            InitializeComponent();
            DataContext = viewModel;
            _viewModel = viewModel;
            _logService = logService;
            _vmService = vmService;
            _taskService = taskService;
            _productService = productService;
            _detectionRecordService = detectionRecordService;
            _auditLogService = auditLogService;
            _ioService = ioService;
            _deviceSettings = deviceSettings;
            _rejectService = rejectService;

            _vmService.OnDetectionResult += VmService_OnDetectionResult;

            LoadTasks();
            SubscribeToLogs();

            this.Unloaded += DetectionView_Unloaded;

            _logService.Log("检测页面已加载");

            InitIOConnection();
        }

        private async void InitIOConnection()
        {
            try
            {
                await _ioService.ConnectAsync(
                    _deviceSettings.ModbusTcpIp,
                    _deviceSettings.ModbusTcpPort);
                _logService.Log($"[DetectionView] IO 模块已连接 {_deviceSettings.ModbusTcpIp}:{_deviceSettings.ModbusTcpPort}");
            }
            catch (Exception ex)
            {
                _logService.Log($"[DetectionView] IO 模块连接失败: {ex.Message}");
                if (_deviceSettings.RequireIOConnectionToStartTask)
                {
                    System.Windows.MessageBox.Show(
                        $"IO 模块连接失败（{_deviceSettings.ModbusTcpIp}:{_deviceSettings.ModbusTcpPort}），检测将无法触发剔除。",
                        "IO 连接异常",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                }
            }
        }

        private void VmService_OnDetectionResult(object sender, DetectionResult result)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateDetectionResult(result);
            });
        }

        private void LoadTasks()
        {
            var tasks = _taskService.GetAll().Where(t => t.Status == Domain.Enums.TaskStatus.Approved).ToList();
            _taskList = tasks;
            cmbTaskSelect.Items.Clear();
            foreach (var task in tasks)
            {
                cmbTaskSelect.Items.Add(task.Name);
            }
            if (tasks.Count > 0)
            {
                _logService.Log($"已加载 {tasks.Count} 个任务");
            }
        }

        private void CmbTask_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbTaskSelect.SelectedItem == null) return;

            var selectedIndex = cmbTaskSelect.SelectedIndex;
            if (selectedIndex < 0 || selectedIndex >= _taskList.Count) return;

            _selectedTask = _taskList[selectedIndex];

            _vmService.SetCurrentTaskContext(_selectedTask.Id, _selectedTask.ProductId, _selectedTask.BatchNumber);

            var product = _productService.GetById(_selectedTask.ProductId);

            var productName = product?.Name ?? "--";
            var solFilePath = product?.SolFilePath ?? "--";

            txtProduct.Text = productName;
            txtBatch.Text = _selectedTask.BatchNumber ?? "--";
            txtProductionDate.Text = _selectedTask.ProductionDate.ToString("yyyy-MM-dd");
            txtExpirationDate.Text = _selectedTask.ExpirationDate.HasValue
                ? _selectedTask.ExpirationDate.Value.ToString("yyyy-MM-dd")
                : "--";
            txtSolFilePath.Text = string.IsNullOrEmpty(solFilePath) ? "--" : solFilePath;

            _logService.Log($"已选择任务: {_selectedTask.Name}");
        }

        private void UpdateDetectionResult(DetectionResult result)
        {
            Dispatcher.Invoke(() =>
            {
                var status = result.IsOK ? "OK" : "NG";

                // 更新本次结果图标
                txtResultIcon.Text = status;
                brsResultIcon.Color = result.IsOK
                    ? System.Windows.Media.Color.FromRgb(0, 192, 0)
                    : System.Windows.Media.Color.FromRgb(255, 0, 0);

                // 更新结果文字
                txtCurrentResult.Text = status;
                txtCurrentResult.Foreground = result.IsOK
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 192, 0))
                    : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));

                txtPassRate.Text = (_okCount + _ngCount) > 0
                    ? ((double)_okCount / (_okCount + _ngCount)).ToString("P2")
                    : "0%";

                if (result.IsOK) _okCount++;
                else _ngCount++;
                txtOkCount.Text = _okCount.ToString();
                txtNgCount.Text = _ngCount.ToString();
                txtTotalCount.Text = (_okCount + _ngCount).ToString();

                var logEntry = $"[{DateTime.Now:HH:mm:ss}] {_selectedTask?.Name}: {status} | 批号={result.BatchNumber} | 生产日期={result.ProductionDate} | 有效期至={result.ExpirationDate} | 耗时={result.ElapsedMs}ms";
                lstDetectionLogs.Items.Insert(0, logEntry);
                if (lstDetectionLogs.Items.Count > 100)
                    lstDetectionLogs.Items.RemoveAt(lstDetectionLogs.Items.Count - 1);

                // Audit log after detection result is received
                var taskForAudit = _selectedTask;
                _auditLogService.Log(SessionManager.CurrentUserId, "DETECTION_RUN", "Detection", 0,
                    JsonConvert.SerializeObject(new {
                        taskId = taskForAudit?.Id ?? 0,
                        taskName = taskForAudit?.Name ?? "",
                        result = status,
                        batchNumber = result.BatchNumber,
                        elapsedMs = result.ElapsedMs
                    }));
            });
        }

        private void SubscribeToLogs()
        {
            _logService.OnLogAdded += OnLogAdded;
        }

        private void OnLogAdded(object sender, LogEntry entry)
        {
            Dispatcher.Invoke(() =>
            {
                lstLogs.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {entry.Message}");
                if (lstLogs.Items.Count > 100)
                    lstLogs.Items.RemoveAt(lstLogs.Items.Count - 1);
            });
        }

        private void InitVmRender()
        {
            if (_vmRender == null)
            {
                _vmRender = new VMControls.Winform.Release.VmRenderControl();
                var host = new System.Windows.Forms.Integration.WindowsFormsHost { Child = _vmRender };
                VmRenderHost.Child = host;
            }
        }

        private GlobalVariableModuleTool GetGlobalVariableTool()
        {
            GlobalVariableModuleTool globalVar = VmSolution.Instance["全局变量1"] as GlobalVariableModuleTool;
            // return the gmt
            if(globalVar == null)
            {
             _logService.Log($"获取全局变量工具: {globalVar}");
            }
            return globalVar;
        }

        private void BtnSelectSol_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "VM Sol File|*.sol*";
            if (dialog.ShowDialog() == true)
            {
                _selectedSolPath = dialog.FileName;
                _isSolutionLoad = false;
                _logService.Log($"已选择方案: {_selectedSolPath}");
                MessageBox.Show("方案路径: " + _selectedSolPath + "\n下一步点击加载按钮!", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnLoadSol_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedSolPath))
            {
                if (_selectedTask != null)
                {
                    var product = _productService.GetById(_selectedTask.ProductId);
                    if (product != null && !string.IsNullOrEmpty(product.SolFilePath))
                    {
                        _selectedSolPath = product.SolFilePath;
                        _logService.Log($"已从任务获取方案路径: {_selectedSolPath}");
                    }
                }

                if (string.IsNullOrEmpty(_selectedSolPath))
                {
                    MessageBox.Show("请先选择方案文件!", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            try
            {
                if (_isSolutionLoad)
                {
                    _isSolutionLoad = false;
                }

                try
                {
                    _vmService.LoadSolution(_selectedSolPath);
                }
                catch (Exception loadEx)
                {
                    _logService.Log($"加载方案异常: {loadEx.GetType().Name} - {loadEx.Message}");
                    MessageBox.Show($"加载方案异常: {loadEx.Message}\n\n方案路径: {_selectedSolPath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                _isSolutionLoad = true;

                _logService.Log("加载方案成功!");
                MessageBox.Show("加载方案成功!", "信息", MessageBoxButton.OK, MessageBoxImage.Information);

                cmbProcedure.Items.Clear();
                foreach (var name in _vmService.GetAllProcedureNames())
                {
                    cmbProcedure.Items.Add(name);
                }

                if (cmbProcedure.Items.Count > 0)
                {
                    cmbProcedure.SelectedIndex = 0;
                    _procedure = _vmService.GetProcedure();
                    if (_procedure == null)
                    {
                        _logService.Log("Procedure 为空，请检查方案!");
                        return;
                    }
                    InitVmRender();
                    _vmRender.ModuleSource = _procedure;
                }
                else
                {
                    _logService.Log("流程数量为0，请检查方案!");
                }
            }
            catch (Exception ex)
            {
                dynamic vmEx = ex;
                if (vmEx.errorCode != null)
                {
                    _logService.Log($"加载方案失败, 错误码: 0x{vmEx.errorCode:X}");
                    MessageBox.Show($"加载方案失败: 0x{vmEx.errorCode:X}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    _logService.Log($"加载方案失败: {ex.Message}");
                    MessageBox.Show($"加载方案失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnSaveSol_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTask == null)
            {
                MessageBox.Show("请先选择任务!", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_isSolutionLoad)
            {
                MessageBox.Show("请先加载方案!", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var batchNumber = _selectedTask.BatchNumber ?? "";
                var mfgDate = _selectedTask.ProductionDate.ToString("yyyyMMdd");
                var expDate = _selectedTask.ExpirationDate.HasValue
                    ? _selectedTask.ExpirationDate.Value.ToString("yyyyMMdd")
                    : "";

                _vmService.SetGlobalVariableString("BN", batchNumber);
                _vmService.SetGlobalVariableString("Mfg", mfgDate);
                _vmService.SetGlobalVariableString("EXP", expDate);

                _logService.Log($"三期信息已设置ToVM: BN={batchNumber}, Mfg={mfgDate}, EXP={expDate}");
                MessageBox.Show($"三期信息已设置:\nBN={batchNumber}\nMfg={mfgDate}\nEXP={expDate}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logService.Log($"设置三期信息失败: {ex.Message}");
                MessageBox.Show($"设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void BtnLoadFromVm_Click(object sender, RoutedEventArgs e)
        {
            if (!_isSolutionLoad || _vmService.GetProcedure() == null)
            {
                MessageBox.Show("请先加载方案!", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var gvTool = GetGlobalVariableTool();
                if (gvTool == null)
                {
                    MessageBox.Show("未找到全局变量模块!\n请确认方案中已添加全局变量模块。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string bn = gvTool.GetGlobalVar("BN") ?? "";
                string mfg = gvTool.GetGlobalVar("Mfg") ?? "";
                string exp = gvTool.GetGlobalVar("EXP") ?? "";

                _logService.Log($"三期信息已获取FromVM: BN={bn}, Mfg={mfg}, EXP={exp}");
                MessageBox.Show($"三期信息:\nBN={bn}\nMfg={mfg}\nEXP={exp}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logService.Log($"获取三期信息失败: {ex.Message}");
                MessageBox.Show($"获取失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CmbProcedure_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbProcedure.SelectedItem == null) return;

            try
            {
                _vmService.SetProcedure(cmbProcedure.SelectedItem.ToString());
                _procedure = _vmService.GetProcedure();
                if (_vmRender != null)
                    _vmRender.ModuleSource = _procedure;

                _logService.Log($"已选择 [{cmbProcedure.SelectedItem}]");
            }
            catch (Exception ex)
            {
                dynamic vmEx = ex;
                if (vmEx.errorCode != null)
                    _logService.Log($"选择流程失败, 错误码: 0x{vmEx.errorCode:X}");
                else
                    _logService.Log($"选择流程失败: {ex.Message}");
            }
        }

        private void BtnTaskRun_Click(object sender, RoutedEventArgs e)
        {
            if (!_isSolutionLoad || _vmService.GetProcedure() == null)
            {
                _logService.Log("流程不存在!");
                return;
            }

            try
            {
                _vmService.RunOnce();
                _logService.Log("单次运行已触发，等待结果回调...");
            }
            catch (Exception ex)
            {
                dynamic vmEx = ex;
                if (vmEx.errorCode != null)
                    _logService.Log($"单次运行失败, 错误码: 0x{vmEx.errorCode:X}");
                else
                    _logService.Log($"单次运行失败: {ex.Message}");
            }
        }

        private void BtnTaskPause_Click(object sender, RoutedEventArgs e)
        {
            if (!_isSolutionLoad || _vmService.GetProcedure() == null)
            {
                _logService.Log("流程不存在!");
                return;
            }

            try
            {
                bool newState = !_vmService.IsContinuousRun;
                if (newState)
                {
                    _auditLogService.Log(SessionManager.CurrentUserId, "DETECTION_CONTINUOUS_START", "Detection", 0,
                        JsonConvert.SerializeObject(new {
                            taskId = _selectedTask?.Id ?? 0,
                            taskName = _selectedTask?.Name ?? ""
                        }));
                }
                else
                {
                    _auditLogService.Log(SessionManager.CurrentUserId, "DETECTION_CONTINUOUS_STOP", "Detection", 0,
                        JsonConvert.SerializeObject(new {
                            taskId = _selectedTask?.Id ?? 0,
                            taskName = _selectedTask?.Name ?? "",
                            totalDetections = _okCount + _ngCount
                        }));
                }
                _vmService.SetContinuousRun(newState);
                _isContinuRun = newState;
                _logService.Log($"连续运行: {newState}");
                btnContiRun.Content = _isContinuRun ? "停止连续" : "连续运行";
            }
            catch (Exception ex)
            {
                dynamic vmEx = ex;
                if (vmEx.errorCode != null)
                    _logService.Log($"连续运行切换失败, 错误码: 0x{vmEx.errorCode:X}");
                else
                    _logService.Log($"连续运行切换失败: {ex.Message}");
            }
        }

        private void DetectionView_Unloaded(object sender, RoutedEventArgs e)
        {
            _ioService.Disconnect();
            _logService.Log("[DetectionView] IO 模块已断开");

            if (_vmService != null && _vmService.IsContinuousRun)
            {
                _vmService.Stop();
                _isContinuRun = false;
                _logService.Log("[DetectionView] 连续检测已停止（切换视图）");
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                CleanupFull();
                _isDisposed = true;
            }
        }

        private void CleanupFull()
        {
            if (_vmService != null)
            {
                _vmService.Stop();
                _vmService.OnDetectionResult -= VmService_OnDetectionResult;
            }

            if (_vmRender != null)
            {
                VmRenderHost.Child = null;
                _vmRender.Dispose();
                _vmRender = null;
            }

            _logService.OnLogAdded -= OnLogAdded;

            _logService.Log("DetectionView 资源已清理");
        }
    }
}