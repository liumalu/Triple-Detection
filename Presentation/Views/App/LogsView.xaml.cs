using System.Windows.Controls;

namespace TripleDetection.Presentation.Views.App
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