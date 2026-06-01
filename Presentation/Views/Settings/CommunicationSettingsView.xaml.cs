using System.Windows.Controls;
namespace TripleDetection.Presentation.Views.Settings
{
    public partial class CommunicationSettingsView : UserControl
    {
        public CommunicationSettingsView() : this((string?)null) { }

        public CommunicationSettingsView(string? placeholder)
        {
            InitializeComponent();
            ContentArea.Text = "通讯设置功能待建设";
        }
    }
}