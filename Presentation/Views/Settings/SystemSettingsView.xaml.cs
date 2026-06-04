using System.Windows.Controls;
namespace TripleDetection.Presentation.Views.Settings
{
    public partial class SystemSettingsView : UserControl
    {
        public SystemSettingsView() : this((string)null) { }

        public SystemSettingsView(string placeholder)
        {
            InitializeComponent();
            ContentArea.Text = "系统设置功能待建设";
        }
    }
}