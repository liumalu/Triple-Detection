using System.Windows;
using System.Windows.Controls;
using TripleDetection.Presentation.ViewModels.Settings;
using TripleDetection.Application.SettingsServices;
using TripleDetection.Application.VmServices;

namespace TripleDetection.Presentation.Views
{
    public partial class SettingsView : UserControl
    {
        private readonly SettingsShellViewModel _viewModel;
        private readonly CommunicationSettingsService _commService;
        private readonly VmSettingsService _vmService;
        private readonly SystemSettingsService _sysService;
        private readonly DeviceControlSettingsService _deviceService;

        private readonly Button[] _navButtons;

        public SettingsView(
            CommunicationSettingsService commService,
            VmSettingsService vmService,
            SystemSettingsService sysService,
            DeviceControlSettingsService deviceService,
            SettingsSyncService syncService)
        {
            InitializeComponent();

            _commService = commService;
            _vmService = vmService;
            _sysService = sysService;
            _deviceService = deviceService;

            _viewModel = new SettingsShellViewModel(commService, vmService, sysService, deviceService);

            _navButtons = new[] { btnCommunication, btnVmSettings, btnSystem, btnDeviceControl };

            // Set data context
            this.DataContext = _viewModel;

            // Load default view
            NavigateTo("Communication");
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                NavigateTo(tag);
            }
        }

        private void NavigateTo(string category)
        {
            // Update button styles
            foreach (var button in _navButtons)
            {
                button.Style = (Style)FindResource(
                    button.Tag?.ToString() == category ? "NavButtonActiveStyle" : "NavButtonStyle");
            }

            // Load the appropriate view
            switch (category)
            {
                case "Communication":
                    ContentArea.Content = new Settings.CommunicationSettingsView();
                    break;
                case "VmSettings":
                    ContentArea.Content = new Settings.VmSettingsView();
                    break;
                case "System":
                    ContentArea.Content = new Settings.SystemSettingsView();
                    break;
                case "DeviceControl":
                    ContentArea.Content = new Settings.DeviceControlSettingsView();
                    break;
            }
        }

        private void SyncButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var syncService = new SettingsSyncService();
                var commSettings = _commService.Load();
                var deviceSettings = _deviceService.Load();
                syncService.SyncToVisionMaster(commSettings, deviceSettings);
                MessageBox.Show("同步成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"同步失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}