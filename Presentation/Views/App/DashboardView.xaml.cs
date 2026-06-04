using System.Windows.Controls;
using TripleDetection.Application.Services;

namespace TripleDetection.Presentation.Views.App
{
    public partial class DashboardView : UserControl
    {
        private readonly IStatisticsService _statisticsService;
        private readonly ITaskService _taskService;

        public DashboardView(IStatisticsService statisticsService, ITaskService taskService)
        {
            _statisticsService = statisticsService;
            _taskService = taskService;
            InitializeComponent();
            LoadStats();
        }

        private void LoadStats()
        {
            var today = System.DateTime.Today;
            var summary = _statisticsService.GetDailyDetectionSummary(today);

            txtTodayOk.Text = summary.OkCount.ToString();
            txtTodayNg.Text = summary.NgCount.ToString();

            var allTasks = _taskService.GetAll();
            txtTotalTasks.Text = allTasks.Count().ToString();

            var pendingTasks = _taskService.GetByStatus(TripleDetection.Domain.Enums.TaskStatus.Pending);
            txtPending.Text = pendingTasks.Count().ToString();
        }
    }
}