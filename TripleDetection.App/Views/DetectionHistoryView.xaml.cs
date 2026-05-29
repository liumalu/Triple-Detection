using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TripleDetection.Data.Repositories;
using TripleDetection.Data.Repositories.Sqlite;
using TripleDetection.Data.Entities;
using TripleDetection.Services;

namespace TripleDetection.Views
{
    public partial class DetectionHistoryView : UserControl
    {
        private readonly IDetectionRecordService _detectionRecordService;
        private DetectionRecordQuery _currentQuery = new DetectionRecordQuery { PageIndex = 0, PageSize = 20 };
        private IPagedResult<DetectionRecord> _currentResult;

        public DetectionHistoryView()
        {
            InitializeComponent();

            var factory = new SqliteRepositoryFactory();
            var repo = factory.CreateDetectionRecordRepository();
            _detectionRecordService = new DetectionRecordService(repo);

            BindData();
        }

        private void BindData()
        {
            _currentResult = _detectionRecordService.Query(_currentQuery);
            dgDetectionRecords.ItemsSource = _currentResult.Items.ToList();
            UpdatePagination();
        }

        private void UpdatePagination()
        {
            txtTotalCount.Text = _currentResult.TotalCount.ToString();
            txtPageIndex.Text = (_currentResult.PageIndex + 1).ToString();
            btnPrev.IsEnabled = _currentResult.HasPreviousPage;
            btnNext.IsEnabled = _currentResult.HasNextPage;
        }

        private DetectionRecordQuery BuildQuery()
        {
            var query = new DetectionRecordQuery
            {
                PageIndex = _currentQuery.PageIndex,
                PageSize = _currentQuery.PageSize,
                SortBy = "DetectionTime",
                SortDescending = true
            };

            if (dpStartDate.SelectedDate.HasValue)
                query.StartDate = dpStartDate.SelectedDate.Value;
            if (dpEndDate.SelectedDate.HasValue)
                query.EndDate = dpEndDate.SelectedDate.Value.AddDays(1).AddSeconds(-1);
            if (!string.IsNullOrWhiteSpace(txtBatchNumber.Text))
                query.BatchNumber = txtBatchNumber.Text;
            if (cmbResult.SelectedItem is ComboBoxItem item && !string.IsNullOrEmpty(item.Tag?.ToString()))
                query.IsOK = bool.Parse(item.Tag.ToString());

            return query;
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            _currentQuery = BuildQuery();
            _currentQuery.PageIndex = 0;
            BindData();
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            _currentQuery.PageIndex--;
            BindData();
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            _currentQuery.PageIndex++;
            BindData();
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var query = BuildQuery();
            query.PageIndex = 0;
            query.PageSize = int.MaxValue;
            var allData = _detectionRecordService.Export(query);

            var dialog = new SaveFileDialog();
            dialog.Filter = "CSV文件|*.csv";
            dialog.FileName = $"检测记录_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            if (dialog.ShowDialog() == true)
            {
                using (var writer = new StreamWriter(dialog.FileName, false, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine("检测时间,批次号,结果,置信度,字符数,识别内容,耗时(ms),图像路径");
                    foreach (var r in allData)
                    {
                        writer.WriteLine($"\"{r.DetectionTime:yyyy-MM-dd HH:mm:ss}\",\"{r.BatchNumber}\",\"{(r.IsOK ? "OK" : "NG")}\",\"{r.Confidence:P2}\",\"{r.CharCount}\",\"{r.CodeInfo}\",\"{r.ElapsedMs}\",\"{r.ImagePath}\"");
                    }
                }
                MessageBox.Show($"导出成功，共 {allData.Count()} 条记录", "导出", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}