using System.Windows.Controls;
using TripleDetection.Application.SettingsServices;
using TripleDetection.Application.Services;
using TripleDetection.Presentation.ViewModels.Settings;

namespace TripleDetection.Presentation.Views.Settings
{
    public partial class SystemSettingsView : UserControl
    {
        public SystemSettingsView()
        {
            InitializeComponent();
            var dbPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "",
                "Config",
                "tripledetection.db");
            var vm = new SystemSettingsViewModel(
                new SystemSettingsService(),
                new AuditLogService(new TripleDetection.Infrastructure.Repositories.AuditLogRepository($"Data Source={dbPath}")));
            DataContext = vm;
            vm.Load();
        }
    }
}