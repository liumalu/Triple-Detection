using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using VM.Core;
using VM.PlatformSDKCS;
using TripleDetection.Services;
using TripleDetection.ViewModels;
using MessageBox = System.Windows.MessageBox;
using TaskEntity = TripleDetection.Data.Entities.Task;
using iMVS_6000PlatformSDKCS.SyncPlatformSDKCS;

namespace TripleDetection.Views
{
    public partial class DetectionView : UserControl
    {
        private readonly LoggingService _logService;
        private readonly MainViewModel _viewModel;
        private string _selectedSolPath;
        private bool _isSolutionLoad = false;
        private bool _isContinuRun = false;
        private VmProcedure _procedure;
        private VMControls.Winform.Release.VmRenderControl _vmRender;
        private List<TaskEntity> _taskList = new List<TaskEntity>();
        private TaskEntity _selectedTask;
        private int _okCount = 0;
        private int _ngCount = 0;

        public DetectionView()
        {
            InitializeComponent();
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", "Message");
            _logService = new LoggingService(logPath);
            _viewModel = new MainViewModel();

            LoadTasks();
            SubscribeToLogs();

            _logService.Log("检测页面已加载");
        }

        private void LoadTasks()
        {
            var taskService = new TripleDetection.Services.TaskService();
            var tasks = taskService.GetAll().Where(t => t.Status == Data.Entities.TaskStatus.Approved).ToList();
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
            _selectedTask = _taskList[selectedIndex];

            // 通过 ProductId 获取产品信息
            var productService = new TripleDetection.Services.ProductService();
            var product = productService.GetById(_selectedTask.ProductId);

            var productName = product?.Name ?? "--";
            var solFilePath = product?.SolFilePath ?? "--";

            txtProduct.Text = $"产品: {productName}";
            txtBatch.Text = $"批次: {_selectedTask.BatchNumber ?? "--"}";
            txtProductionDate.Text = $"生产日期: {_selectedTask.ProductionDate:yyyy-MM-dd}";
            txtExpirationDate.Text = _selectedTask.ExpirationDate.HasValue
                ? $"有效期至: {_selectedTask.ExpirationDate.Value:yyyy-MM-dd}"
                : "有效期至: --";
            txtSolFilePath.Text = $"方案路径: {solFilePath}";

            _logService.Log($"已选择任务: {_selectedTask.Name}");
        }

        private void UpdateDetectionResult(string result, double confidence)
        {
            Dispatcher.Invoke(() =>
            {
                txtCurrentResult.Text = result;
                txtCurrentResult.Foreground = result == "OK"
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 192, 0))
                    : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));

                txtPassRate.Text = ((double)_okCount / (_okCount + _ngCount)).ToString("P2");

                if (result == "OK") _okCount++;
                else _ngCount++;
                txtOkCount.Text = $"OK: {_okCount}";
                txtNgCount.Text = $"NG: {_ngCount}";

                var logEntry = $"[{DateTime.Now:HH:mm:ss}] {_selectedTask?.Name}: {result} ({confidence:P2})";
                lstDetectionLogs.Items.Insert(0, logEntry);
                if (lstDetectionLogs.Items.Count > 100)
                    lstDetectionLogs.Items.RemoveAt(lstDetectionLogs.Items.Count - 1);
            });
        }

        private void SubscribeToLogs()
        {
            _logService.OnLogAdded += (s, entry) =>
            {
                Dispatcher.Invoke(() =>
                {
                    lstLogs.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {entry.Message}");
                    if (lstLogs.Items.Count > 100)
                        lstLogs.Items.RemoveAt(lstLogs.Items.Count - 1);
                });
            };
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
            // 如果没有选择方案路径，尝试从已选任务获取方案路径
            if (string.IsNullOrEmpty(_selectedSolPath))
            {
                if (_selectedTask != null)
                {
                    var productService = new TripleDetection.Services.ProductService();
                    var product = productService.GetById(_selectedTask.ProductId);
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

                VmSolution.Load(_selectedSolPath);
                _isSolutionLoad = true;

                _logService.Log("加载方案成功!");
                MessageBox.Show("加载方案成功!", "信息", MessageBoxButton.OK, MessageBoxImage.Information);

                cmbProcedure.Items.Clear();
                var processList = VmSolution.Instance.GetAllProcedureList();
                for (int i = 0; i < processList.nNum; i++)
                {
                    cmbProcedure.Items.Add(processList.astProcessInfo[i].strProcessName);
                }

                if (cmbProcedure.Items.Count > 0)
                {
                    cmbProcedure.SelectedIndex = 0;
                    _procedure = VmSolution.Instance[processList.astProcessInfo[0].strProcessName] as VmProcedure;
                    if (_procedure == null)
                    {
                        _logService.Log(" Procedure 为空，请检查方案!");
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
            catch (VmException ex)
            {
                _logService.Log($"加载方案失败, 错误码: 0x{ex.errorCode:X}");
                MessageBox.Show($"加载方案失败: 0x{ex.errorCode:X}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                _logService.Log($"加载方案失败: {ex.Message}");
                MessageBox.Show($"加载方案失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                var pfsync = new ImvsSdkPFSync();
                var result = pfsync.Start();
                if (result != 0)
                {
                    MessageBox.Show($"SDK初始化失败: 0x{result:X}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var batchNumber = _selectedTask.BatchNumber ?? "";
                var mfgDate = _selectedTask.ProductionDate.ToString("yyyyMMdd");
                var expDate = _selectedTask.ExpirationDate.HasValue
                    ? _selectedTask.ExpirationDate.Value.ToString("yyyyMMdd")
                    : "";

                pfsync.modules.moduleControl.SetGlobalVarValue("BN", batchNumber);
                pfsync.modules.moduleControl.SetGlobalVarValue("Mfg", mfgDate);
                pfsync.modules.moduleControl.SetGlobalVarValue("EXP", expDate);

                pfsync.Exit();

                _logService.Log($"三期信息已设置: BN={batchNumber}, Mfg={mfgDate}, EXP={expDate}");
                MessageBox.Show($"三期信息已设置:\nBN={batchNumber}\nMfg={mfgDate}\nEXP={expDate}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logService.Log($"设置三期信息失败: {ex.Message}");
                MessageBox.Show($"设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CmbProcedure_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbProcedure.SelectedItem == null) return;

            try
            {
                _procedure = VmSolution.Instance[cmbProcedure.SelectedItem.ToString()] as VmProcedure;
                if (_vmRender != null)
                    _vmRender.ModuleSource = _procedure;

                _logService.Log($"已选择 [{cmbProcedure.SelectedItem}]");
            }
            catch (VmException ex)
            {
                _logService.Log($"选择流程失败, 错误码: 0x{ex.errorCode:X}");
            }
            catch (Exception ex)
            {
                _logService.Log($"选择流程失败: {ex.Message}");
            }
        }

        private void BtnTaskRun_Click(object sender, RoutedEventArgs e)
        {
            if (!_isSolutionLoad || _procedure == null)
            {
                _logService.Log("流程不存在!");
                return;
            }

            try
            {
                _procedure.Run();
                _logService.Log("单次运行已触发");

                // 模拟检测结果更新
                var random = new Random();
                var isOk = random.Next(100) > 20;
                var confidence = random.NextDouble() * 0.4 + 0.6;
                UpdateDetectionResult(isOk ? "OK" : "NG", confidence);
            }
            catch (VmException ex)
            {
                _logService.Log($"单次运行失败, 错误码: 0x{ex.errorCode:X}");
            }
            catch (Exception ex)
            {
                _logService.Log($"单次运行失败: {ex.Message}");
            }
        }

        private void BtnTaskPause_Click(object sender, RoutedEventArgs e)
        {
            if (!_isSolutionLoad || _procedure == null)
            {
                _logService.Log("流程不存在!");
                return;
            }

            try
            {
                bool beforeToggle = _procedure.ContinuousRunEnable;
                _procedure.ContinuousRunEnable = _procedure.ContinuousRunEnable ^ true;
                _isContinuRun = _procedure.ContinuousRunEnable;

                _logService.Log($"连续运行切换: {beforeToggle} -> {_procedure.ContinuousRunEnable}");
                btnContiRun.Content = _isContinuRun ? "停止连续" : "连续运行";
            }
            catch (VmException ex)
            {
                _logService.Log($"连续运行切换失败, 错误码: 0x{ex.errorCode:X}");
            }
            catch (Exception ex)
            {
                _logService.Log($"连续运行切换失败: {ex.Message}");
            }
        }
    }
}