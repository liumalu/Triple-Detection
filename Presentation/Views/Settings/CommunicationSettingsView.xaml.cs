using System.Windows.Controls;
using TripleDetection.Presentation.ViewModels.Settings;
using TripleDetection.Application.SettingsServices;
using TripleDetection.Application.Services;

namespace TripleDetection.Presentation.Views.Settings
{
    public partial class CommunicationSettingsView : UserControl
    {
        public CommunicationSettingsView()
        {
            InitializeComponent();
            var vm = new CommunicationSettingsViewModel(
                (CommunicationSettingsService)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(CommunicationSettingsService)),
                (IAuditLogService)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IAuditLogService)));
            vm.Load();
            DataContext = vm;
        }
    }
}