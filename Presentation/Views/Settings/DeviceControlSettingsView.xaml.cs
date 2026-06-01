using System.Windows.Controls;
namespace TripleDetection.Presentation.Views.Settings
{
    public partial class DeviceControlSettingsView : UserControl
    {
        public DeviceControlSettingsView() : this((string?)null) { }

        public DeviceControlSettingsView(string? placeholder)
        {
            InitializeComponent();
            ContentArea.Text = "设备控制设置功能待建设";
        }
    }
}