using System;
using System.Windows;
using VM.PlatformSDKCS;
using TripleDetection.Services;
using TripleDetection.ViewModels;
using System.Windows.Forms;
using System.Configuration;

namespace TripleDetection
{
    public partial class MainWindow : Window
    {
        private VmIntegrationService _vmService;
        private ImageStorageService _imageStorage;
        private readonly string _solPath;
        private readonly string _okDir;
        private readonly string _ngDir;
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            _solPath = System.Configuration.ConfigurationManager.AppSettings["SolFilePath"];
            _okDir = System.Configuration.ConfigurationManager.AppSettings["OkImageDir"];
            _ngDir = System.Configuration.ConfigurationManager.AppSettings["NgImageDir"];

            txtSolPath.Text = _solPath;

            _imageStorage = new ImageStorageService(_okDir, _ngDir);
            _vmService = new VmIntegrationService(_imageStorage);
            _vmService.OnDetectionResult += VmService_OnDetectionResult;

            _viewModel = new MainViewModel();
            this.DataContext = _viewModel;
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "VM Sol File|*.sol*";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtSolPath.Text = dialog.FileName;
                }
            }
        }

        private void BtnLoadSolu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _vmService.LoadSolution(txtSolPath.Text);
                System.Windows.MessageBox.Show("Solution loaded successfully!", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (VmException ex)
            {
                System.Windows.MessageBox.Show($"Failed to load solution: 0x{ex.errorCode:X}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            _vmService.SetContinuousRun(true);
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _vmService.SetContinuousRun(false);
        }

        private void VmService_OnDetectionResult(object sender, Models.DetectionResult result)
        {
            Dispatcher.Invoke(() =>
            {
                txtResult.Text = result.IsOK ? "OK" : "NG";
                borderResult.Background = result.IsOK ?
                    new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 192, 0)) :
                    new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                txtDetails.Text = $"Code: {result.CodeInfo}, Chars: {result.CharCount}, Confidence: {result.Confidence:P}";

                _viewModel.ResultText = result.IsOK ? "OK" : "NG";
                _viewModel.Details = $"Code: {result.CodeInfo}, Chars: {result.CharCount}, Confidence: {result.Confidence:P}";
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var procedure = _vmService.GetProcedure();
            if (procedure != null)
            {
                try
                {
                    VmHost.Child = new VMControls.Winform.Release.VmRenderControl { ModuleSource = procedure };
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to load VM control: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}