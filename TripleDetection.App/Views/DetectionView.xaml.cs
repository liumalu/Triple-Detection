using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TripleDetection.Services;
using TripleDetection.ViewModels;
using TaskEntity = TripleDetection.Data.Entities.Task;

namespace TripleDetection.Views
{
    public partial class DetectionView : UserControl
    {
        private readonly LoggingService _logService;
        private readonly TaskService _taskService;
        private readonly MainViewModel _mainViewModel;
        private TaskEntity _selectedTask;

        public DetectionView()
        {
            InitializeComponent();
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", "Message");
            _logService = new LoggingService(logPath);
            _taskService = new TaskService();
            _mainViewModel = new MainViewModel();

            LoadTasks();
            SubscribeToLogs();

            _logService.Log("检测页面已加载");
        }

        private void LoadTasks()
        {
            var tasks = _taskService.GetAll().Where(t => t.Status == Data.Entities.TaskStatus.Approved).ToList();
            cmbTask.ItemsSource = tasks;
            if (tasks.Count > 0)
                cmbTask.SelectedIndex = 0;
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

        private void CmbTask_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbTask.SelectedItem is TaskEntity task)
            {
                _selectedTask = task;
                txtProduct.Text = $"产品：{task.Product?.Name ?? "-"}";
                txtBatch.Text = $"批次：{task.BatchNumber}";
                txtProductionDate.Text = $"生产日期：{task.ProductionDate:yyyy-MM-dd}";
                txtExpirationDate.Text = task.ExpirationDate.HasValue
                    ? $"有效期至：{task.ExpirationDate:yyyy-MM-dd}"
                    : $"有效期至：-";
                _logService.Log($"已选择任务：{task.Name}");
            }
        }

        private void BtnSelectSol_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "VM Sol File|*.sol*";
            if (dialog.ShowDialog() == true)
            {
                _logService.Log($"已选择方案：{dialog.FileName}");
            }
        }

        private void BtnLoadSol_Click(object sender, RoutedEventArgs e)
        {
            _logService.Log("加载方案...");
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = false;
            btnStop.IsEnabled = true;
            _logService.Log("检测开始");
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
            _logService.Log("检测停止");
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            txtOkCount.Text = "OK: 0";
            txtNgCount.Text = "NG: 0";
            txtConfidence.Text = "--";
            _logService.Log("结果已重置");
        }
    }
}