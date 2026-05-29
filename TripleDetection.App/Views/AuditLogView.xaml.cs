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
    public partial class AuditLogView : UserControl
    {
        private readonly IAuditLogService _auditLogService;
        private readonly IUserService _userService;
        private AuditLogQuery _currentQuery = new AuditLogQuery { PageIndex = 0, PageSize = 20 };
        private IPagedResult<AuditLog> _currentResult;

        public AuditLogView()
        {
            InitializeComponent();

            var repositoryFactory = new SqliteRepositoryFactory();
            var auditLogRepo = repositoryFactory.CreateAuditLogRepository();
            _auditLogService = new AuditLogService(auditLogRepo);
            _userService = new UserService(repositoryFactory.CreateUserRepository(), _auditLogService);

            LoadUsers();
            BindData();
        }

        private void LoadUsers()
        {
            cmbUser.Items.Clear();
            cmbUser.Items.Add(new ComboBoxItem { Content = "全部", Tag = "" });
            var users = _userService.GetAll();
            foreach (var user in users)
            {
                cmbUser.Items.Add(new ComboBoxItem { Content = user.RealName, Tag = user.Id.ToString() });
            }
            cmbUser.SelectedIndex = 0;
        }

        private void BindData()
        {
            _currentResult = _auditLogService.Query(_currentQuery);
            dgAuditLog.ItemsSource = _currentResult.Items.ToList();
            UpdatePagination();
        }

        private void UpdatePagination()
        {
            txtTotalCount.Text = _currentResult.TotalCount.ToString();
            txtPageIndex.Text = (_currentResult.PageIndex + 1).ToString();
            txtTotalPages.Text = _currentResult.TotalPages.ToString();
            btnPrev.IsEnabled = _currentResult.HasPreviousPage;
            btnNext.IsEnabled = _currentResult.HasNextPage;
        }

        private AuditLogQuery BuildQuery()
        {
            var query = new AuditLogQuery
            {
                PageIndex = _currentQuery.PageIndex,
                PageSize = _currentQuery.PageSize,
                SortBy = "CreateAt",
                SortDescending = true
            };

            if (dpStartDate.SelectedDate.HasValue)
                query.StartDate = dpStartDate.SelectedDate.Value;
            if (dpEndDate.SelectedDate.HasValue)
                query.EndDate = dpEndDate.SelectedDate.Value.AddDays(1).AddSeconds(-1);

            if (cmbUser.SelectedItem is ComboBoxItem userItem && !string.IsNullOrEmpty(userItem.Tag?.ToString()))
                query.UserId = int.Parse(userItem.Tag.ToString());

            if (cmbAction.SelectedItem is ComboBoxItem actionItem && !string.IsNullOrEmpty(actionItem.Tag?.ToString()))
                query.Action = actionItem.Tag.ToString();

            if (cmbObjectType.SelectedItem is ComboBoxItem objItem && !string.IsNullOrEmpty(objItem.Tag?.ToString()))
                query.ObjectType = objItem.Tag.ToString();

            query.Keyword = txtKeyword.Text;

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

        private void CmbPageSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbPageSize.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                _currentQuery.PageSize = int.Parse(item.Tag.ToString());
                _currentQuery.PageIndex = 0;
                BindData();
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var query = BuildQuery();
            query.PageIndex = 0;
            query.PageSize = int.MaxValue;
            var allData = _auditLogService.Export(query);

            var dialog = new SaveFileDialog();
            dialog.Filter = "CSV文件|*.csv";
            dialog.FileName = $"审计日志_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            if (dialog.ShowDialog() == true)
            {
                using (var writer = new StreamWriter(dialog.FileName, false, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine("时间,用户,操作,对象类型,对象ID,详情,IP地址");
                    foreach (var log in allData)
                    {
                        writer.WriteLine($"\"{log.CreateAt:yyyy-MM-dd HH:mm:ss}\",\"{log.UserName}\",\"{log.Action}\",\"{log.ObjectType}\",\"{log.ObjectId}\",\"{log.Details}\",\"{log.IpAddress}\"");
                    }
                }
                MessageBox.Show($"导出成功，共 {allData.Count()} 条记录", "导出", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}