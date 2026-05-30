using System;
using Prism.Commands;
using Prism.Mvvm;
using TripleDetection.Services;

namespace TripleDetection.ViewModels.Settings
{
    public class SettingsShellViewModel : BindableBase
    {
        private string _currentCategory = "Communication";
        private object _currentView;
        private readonly CommunicationSettingsService _commService;
        private readonly VmSettingsService _vmService;
        private readonly SystemSettingsService _sysService;
        private readonly DeviceControlSettingsService _deviceService;

        public DelegateCommand<string> NavigateCommand { get; }
        public DelegateCommand SyncCommand { get; }

        public SettingsShellViewModel()
            : this(new CommunicationSettingsService(), new VmSettingsService(),
                   new SystemSettingsService(), new DeviceControlSettingsService())
        {
        }

        public SettingsShellViewModel(
            CommunicationSettingsService commService,
            VmSettingsService vmService,
            SystemSettingsService sysService,
            DeviceControlSettingsService deviceService)
        {
            _commService = commService;
            _vmService = vmService;
            _sysService = sysService;
            _deviceService = deviceService;

            NavigateCommand = new DelegateCommand<string>(NavigateTo);
            SyncCommand = new DelegateCommand(SyncToVm);

            // Initialize with Communication view
            NavigateTo("Communication");
        }

        public string CurrentCategory
        {
            get => _currentCategory;
            set
            {
                if (SetProperty(ref _currentCategory, value))
                {
                    RaisePropertyChanged(nameof(IsCommunicationActive));
                    RaisePropertyChanged(nameof(IsVmSettingsActive));
                    RaisePropertyChanged(nameof(IsSystemActive));
                    RaisePropertyChanged(nameof(IsDeviceControlActive));
                }
            }
        }

        public object CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public bool IsCommunicationActive => _currentCategory == "Communication";
        public bool IsVmSettingsActive => _currentCategory == "VmSettings";
        public bool IsSystemActive => _currentCategory == "System";
        public bool IsDeviceControlActive => _currentCategory == "DeviceControl";

        public CommunicationSettingsService CommService => _commService;
        public VmSettingsService VmService => _vmService;
        public SystemSettingsService SysService => _sysService;
        public DeviceControlSettingsService DeviceService => _deviceService;

        private void NavigateTo(string category)
        {
            CurrentCategory = category;

            switch (category)
            {
                case "Communication":
                    CurrentView = new Views.Settings.CommunicationSettingsView(_commService);
                    break;
                case "VmSettings":
                    CurrentView = new Views.Settings.VmSettingsView(_vmService);
                    break;
                case "System":
                    CurrentView = new Views.Settings.SystemSettingsView(_sysService);
                    break;
                case "DeviceControl":
                    CurrentView = new Views.Settings.DeviceControlSettingsView(_deviceService);
                    break;
            }
        }

        private void SyncToVm()
        {
            try
            {
                var syncService = new SettingsSyncService();
                var commSettings = _commService.Load();
                var deviceSettings = _deviceService.Load();
                syncService.SyncToVisionMaster(commSettings, deviceSettings);
                System.Windows.MessageBox.Show("同步成功", "提示",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"同步失败: {ex.Message}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
