using System;
using System.Windows.Input;
using TripleDetection.Services;

namespace TripleDetection.ViewModels.Settings
{
    public class SettingsShellViewModel
    {
        private string _currentCategory = "Communication";
        private object _currentView;
        private readonly CommunicationSettingsService _commService;
        private readonly VmSettingsService _vmService;
        private readonly SystemSettingsService _sysService;
        private readonly DeviceControlSettingsService _deviceService;

        public ICommand NavigateCommand { get; }
        public ICommand SyncCommand { get; }

        public SettingsShellViewModel()
        {
            _commService = new CommunicationSettingsService();
            _vmService = new VmSettingsService();
            _sysService = new SystemSettingsService();
            _deviceService = new DeviceControlSettingsService();

            NavigateCommand = new RelayCommand(param => NavigateTo(param as string));
            SyncCommand = new RelayCommand(param => SyncToVm());

            // Initialize with Communication view
            NavigateTo("Communication");
        }

        public string CurrentCategory
        {
            get => _currentCategory;
            set
            {
                _currentCategory = value;
                OnPropertyChanged(nameof(CurrentCategory));
                OnPropertyChanged(nameof(IsCommunicationActive));
                OnPropertyChanged(nameof(IsVmSettingsActive));
                OnPropertyChanged(nameof(IsSystemActive));
                OnPropertyChanged(nameof(IsDeviceControlActive));
            }
        }

        public object CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged(nameof(CurrentView));
            }
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

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}