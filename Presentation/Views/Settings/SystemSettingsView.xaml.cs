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
            var vm = new SystemSettingsViewModel(
                (SystemSettingsService)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(SystemSettingsService)),
                (IAuditLogService)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IAuditLogService)));
            DataContext = vm;
            vm.Load();
        }
    }
}