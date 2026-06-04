using System.Windows.Controls;
using TripleDetection.Presentation.ViewModels.Settings;
using TripleDetection.Application.SettingsServices;
using TripleDetection.Application.Services;

namespace TripleDetection.Presentation.Views.Settings
{
    public partial class DeviceControlSettingsView : UserControl
    {
        public DeviceControlSettingsView()
        {
            InitializeComponent();
            var vm = new DeviceControlSettingsViewModel(
                (DeviceControlSettingsService)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(DeviceControlSettingsService)),
                (IAuditLogService)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IAuditLogService)));
            vm.Load();
            DataContext = vm;
        }
    }
}