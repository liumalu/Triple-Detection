using System.Windows.Controls;
namespace TripleDetection.Presentation.Views.Settings
{
    public partial class VmSettingsView : UserControl
    {
        public VmSettingsView() : this((string)null) { }

        public VmSettingsView(string placeholder)
        {
            InitializeComponent();
            ContentArea.Text = "VisionMaster设置功能待建设";
        }
    }
}