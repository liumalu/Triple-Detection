using System.Windows.Controls;

namespace TripleDetection.Views
{
    public partial class LogsView : UserControl
    {
        public LogsView()
        {
            InitializeComponent();
            LoadLogs();
        }

        private void LoadLogs()
        {
            // TODO: 从服务加载实际数据
        }
    }
}