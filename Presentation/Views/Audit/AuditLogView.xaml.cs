using System;
using System.Windows;
using System.Windows.Controls;
using TripleDetection.Application.Services;
using TripleDetection.Domain.Entities;
using TripleDetection.Domain.Entities.Queries;
using TripleDetection.Infrastructure.Repositories;
using TripleDetection.Infrastructure.Persistence;

namespace TripleDetection.Presentation.Views.Audit
{
    public partial class AuditLogView : UserControl
    {
        private readonly IAuditLogService _auditLogService;
        private readonly IStatisticsService _statisticsService;
        private AuditLogQuery _currentQuery;
        private int _totalPages = 1;

        public AuditLogView()
        {
            InitializeComponent();
            _auditLogService = new AuditLogService(new AuditLogRepository(
                "Data Source=" + System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "tripledetection.db")));
            _statisticsService = new StatisticsService(new SqliteConnectionFactory(
                "Data Source=" + System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "tripledetection.db")));
            _currentQuery = new AuditLogQuery { PageIndex = 1, PageSize = 20 };

            dpStartDate.SelectedDate = DateTime.Today.AddDays(-7);
            dpEndDate.SelectedDate = DateTime.Today;

            LoadUsers();
            LoadActionTypes();
            LoadData();
            LoadCharts();
        }

        private void LoadUsers()
        {
            var users = new[] { new { UserId = 0, UserName = "全部" } };
            cmbUser.ItemsSource = users;
            cmbUser.SelectedIndex = 0;
        }

        private void LoadActionTypes()
        {
            cmbAction.ItemsSource = new[] { "", "LOGIN", "LOGOUT", "PRODUCT_CREATE", "PRODUCT_UPDATE",
                "TASK_CREATE", "TASK_APPROVE", "DETECTION_RUN", "DETECTION_CONTINUOUS_START" };
            cmbAction.SelectedIndex = 0;
        }

        private void LoadData()
        {
            try
            {
                _currentQuery.StartDate = dpStartDate.SelectedDate;
                _currentQuery.EndDate = dpEndDate.SelectedDate?.AddDays(1);
                if (cmbUser.SelectedValue != null && (int)cmbUser.SelectedValue != 0)
                    _currentQuery.UserId = (int)cmbUser.SelectedValue;
                if (!string.IsNullOrEmpty(cmbAction.Text))
                    _currentQuery.Action = cmbAction.Text;
                if (!string.IsNullOrEmpty(txtKeyword.Text))
                    _currentQuery.Keyword = txtKeyword.Text;

                var result = _auditLogService.Query(_currentQuery);
                dgLogs.ItemsSource = result.Items;
                int totalPages = (int)Math.Ceiling((double)result.TotalCount / _currentQuery.PageSize);
                _totalPages = totalPages > 0 ? totalPages : 1;
                txtPageInfo.Text = $"{_currentQuery.PageIndex} / {_totalPages}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadData failed: {ex.Message}");
                MessageBox.Show("加载数据失败: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCharts()
        {
            try
            {
                var startDate = dpStartDate.SelectedDate ?? DateTime.Today.AddDays(-7);
                var endDate = (dpEndDate.SelectedDate ?? DateTime.Today).AddDays(1);

                var distribution = _statisticsService.GetActionDistribution(startDate, endDate);
                icActionDistribution.ItemsSource = distribution;

                var trend = _statisticsService.GetDailyOperationTrend(startDate, endDate);
                icDailyTrend.ItemsSource = trend;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadCharts failed: {ex.Message}");
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            _currentQuery.PageIndex = 1;
            LoadData();
            LoadCharts();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            dpStartDate.SelectedDate = DateTime.Today.AddDays(-7);
            dpEndDate.SelectedDate = DateTime.Today;
            cmbUser.SelectedIndex = 0;
            cmbAction.SelectedIndex = 0;
            txtKeyword.Text = "";
            _currentQuery = new AuditLogQuery { PageIndex = 1, PageSize = 20 };
            LoadData();
            LoadCharts();
        }

        private void FirstPage_Click(object sender, RoutedEventArgs e) { _currentQuery.PageIndex = 1; LoadData(); }
        private void PrevPage_Click(object sender, RoutedEventArgs e) { if (_currentQuery.PageIndex > 1) { _currentQuery.PageIndex--; LoadData(); } }
        private void NextPage_Click(object sender, RoutedEventArgs e) { if (_currentQuery.PageIndex < _totalPages) { _currentQuery.PageIndex++; LoadData(); } }
        private void LastPage_Click(object sender, RoutedEventArgs e) { _currentQuery.PageIndex = _totalPages; LoadData(); }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导出功能待实现");
        }
    }
}
