using System.Windows.Controls;

namespace TripleDetection.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            LoadStats();
        }

        private void LoadStats()
        {
            // TODO: 从服务加载实际数据
            txtTodayOk.Text = "156";
            txtTodayNg.Text = "3";
            txtTotalTasks.Text = "42";
            txtPending.Text = "8";
        }
    }
}