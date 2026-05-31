using System;
using CommunityToolkit.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TripleDetection.Application.SettingsServices;
using TripleDetection.Application.VmServices;

namespace TripleDetection.Presentation.ViewModels.Settings
{
    public partial class SettingsShellViewModel : ObservableObject
    {
        [ObservableProperty] private string _currentCategory = "Communication";
        [ObservableProperty] private object? _currentView;

        private readonly CommunicationSettingsService _commService;
        private readonly VmSettingsService _vmService;
        private readonly SystemSettingsService _sysService;
        private readonly DeviceControlSettingsService _deviceService;

        public IRelayCommand<string> NavigateCommand { get; }
        public IRelayCommand SyncCommand { get; }

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

            NavigateCommand = new RelayCommand<string>(NavigateTo);
            SyncCommand = new RelayCommand(SyncToVm);

            // Initialize with Communication view
            NavigateTo("Communication");
        }

        partial void OnCurrentCategoryChanged(string value)
        {
            OnPropertyChanged(nameof(IsCommunicationActive));
            OnPropertyChanged(nameof(IsVmSettingsActive));
            OnPropertyChanged(nameof(IsSystemActive));
            OnPropertyChanged(nameof(IsDeviceControlActive));
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