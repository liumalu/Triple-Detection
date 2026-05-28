using System.Windows;
using System.Windows.Controls;
using TripleDetection.Services;
using TripleDetection.Models;

namespace TripleDetection.Views.Settings
{
    public partial class DeviceControlSettingsView : UserControl
    {
        private readonly DeviceControlSettingsService _service;

        public DeviceControlSettingsView()
        {
            InitializeComponent();
            _service = new DeviceControlSettingsService();
            LoadSettings();
        }

        public DeviceControlSettingsView(DeviceControlSettingsService service) : this()
        {
            _service = service;
            LoadSettings();
        }

        private void LoadSettings()
        {
            var settings = _service.Load();

            // Set light source type
            foreach (ComboBoxItem item in cboLightSourceType.Items)
            {
                if (item.Tag?.ToString() == settings.LightSourceType)
                {
                    cboLightSourceType.SelectedItem = item;
                    break;
                }
            }
            if (cboLightSourceType.SelectedItem == null)
            {
                cboLightSourceType.SelectedIndex = 0;
            }

            txtCaptureDelay.Text = settings.CaptureDelayMs.ToString();
            txtCaptureFeedbackTimeout.Text = settings.CaptureFeedbackTimeoutMs.ToString();
            txtRejectDelay.Text = settings.RejectDelayMs.ToString();
            txtRejectDuration.Text = settings.RejectDurationMs.ToString();
            txtConsecutiveRejects.Text = settings.ConsecutiveRejectsToStopLine.ToString();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = new DeviceControlSettings
                {
                    LightSourceType = (cboLightSourceType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "LED",
                    CaptureDelayMs = int.TryParse(txtCaptureDelay.Text, out int captureDelay) ? captureDelay : 100,
                    CaptureFeedbackTimeoutMs = int.TryParse(txtCaptureFeedbackTimeout.Text, out int timeout) ? timeout : 5000,
                    RejectDelayMs = int.TryParse(txtRejectDelay.Text, out int rejectDelay) ? rejectDelay : 50,
                    RejectDurationMs = int.TryParse(txtRejectDuration.Text, out int rejectDuration) ? rejectDuration : 200,
                    ConsecutiveRejectsToStopLine = int.TryParse(txtConsecutiveRejects.Text, out int consecutive) ? consecutive : 10
                };

                _service.Save(settings);
                MessageBox.Show("保存成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}