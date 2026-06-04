using System.Windows.Controls;
using TripleDetection.Presentation.ViewModels.Settings;
using TripleDetection.Application.SettingsServices;
using TripleDetection.Application.Services;

namespace TripleDetection.Presentation.Views.Settings
{
    public partial class VmSettingsView : UserControl
    {
        public VmSettingsView()
        {
            InitializeComponent();
            var vm = new VmSettingsViewModel(
                (VmSettingsService)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(VmSettingsService)),
                (IAuditLogService)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IAuditLogService)));
            vm.Load();
            DataContext = vm;
        }
    }
}