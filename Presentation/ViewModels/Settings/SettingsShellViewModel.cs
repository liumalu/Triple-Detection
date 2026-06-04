using System;
using Prism.Mvvm;
using Prism.Commands;
using TripleDetection.Application.SettingsServices;
using TripleDetection.Application.VmServices;

namespace TripleDetection.Presentation.ViewModels.Settings
{
    public partial class SettingsShellViewModel : ViewModelBase
    {
        private string _currentCategory = "Communication";
        private object _currentView;

        private readonly CommunicationSettingsService _commService;
        private readonly VmSettingsService _vmService;
        private readonly SystemSettingsService _sysService;
        private readonly DeviceControlSettingsService _deviceService;

        public DelegateCommand<string> NavigateCommand { get; }
        public DelegateCommand SyncCommand { get; }

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
                    OnPropertyChanged(nameof(IsCommunicationActive));
                    OnPropertyChanged(nameof(IsVmSettingsActive));
                    OnPropertyChanged(nameof(IsSystemActive));
                    OnPropertyChanged(nameof(IsDeviceControlActive));
                }
            }
        }

        public object CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public bool IsCommunicationActive => CurrentCategory == "Communication";
        public bool IsVmSettingsActive => CurrentCategory == "VmSettings";
        public bool IsSystemActive => CurrentCategory == "System";
        public bool IsDeviceControlActive => CurrentCategory == "DeviceControl";

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
                    CurrentView = new Views.Settings.CommunicationSettingsView();
                    break;
                case "VmSettings":
                    CurrentView = new Views.Settings.VmSettingsView();
                    break;
                case "System":
                    CurrentView = new Views.Settings.SystemSettingsView();
                    break;
                case "DeviceControl":
                    CurrentView = new Views.Settings.DeviceControlSettingsView();
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