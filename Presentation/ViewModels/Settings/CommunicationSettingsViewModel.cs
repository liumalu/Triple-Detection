using System;
using Prism.Mvvm;
using Prism.Commands;
using TripleDetection.Application.SettingsServices;
using TripleDetection.Application.Services;
using TripleDetection.Domain;
using TripleDetection.Presentation.Models;
using Newtonsoft.Json;

namespace TripleDetection.Presentation.ViewModels.Settings
{
    public class CommunicationSettingsViewModel : ViewModelBase
    {
        private readonly CommunicationSettingsService _service;
        private readonly IAuditLogService _auditLogService;
        private CommunicationSettings _originalSettings;

        public CommunicationSettingsViewModel(
            CommunicationSettingsService service,
            IAuditLogService auditLogService)
        {
            _service = service;
            _auditLogService = auditLogService;
            SaveCommand = new DelegateCommand(ExecuteSave);
        }

        private string _cameraIp = string.Empty;
        public string CameraIp
        {
            get => _cameraIp;
            set => SetProperty(ref _cameraIp, value);
        }

        private int _cameraPort;
        public int CameraPort
        {
            get => _cameraPort;
            set => SetProperty(ref _cameraPort, value);
        }

        private string _plcIp = string.Empty;
        public string PlcIp
        {
            get => _plcIp;
            set => SetProperty(ref _plcIp, value);
        }

        private int _plcPort;
        public int PlcPort
        {
            get => _plcPort;
            set => SetProperty(ref _plcPort, value);
        }

        private string _plcType = string.Empty;
        public string PlcType
        {
            get => _plcType;
            set => SetProperty(ref _plcType, value);
        }

        private int _baudRate;
        public int BaudRate
        {
            get => _baudRate;
            set => SetProperty(ref _baudRate, value);
        }

        public string[] PlcTypeOptions { get; } = { "Mitsubishi", "Siemens", "Omron" };
        public int[] BaudRateOptions { get; } = { 9600, 19200, 38400, 115200 };

        public DelegateCommand SaveCommand { get; }

        public void Load()
        {
            _originalSettings = _service.Load();
            CameraIp = _originalSettings.CameraIp;
            CameraPort = _originalSettings.CameraPort;
            PlcIp = _originalSettings.PlcIp;
            PlcPort = _originalSettings.PlcPort;
            PlcType = _originalSettings.PlcType;
            BaudRate = _originalSettings.BaudRate;
        }

        private void ExecuteSave()
        {
            try
            {
                var newSettings = new CommunicationSettings
                {
                    CameraIp = CameraIp,
                    CameraPort = CameraPort,
                    PlcIp = PlcIp,
                    PlcPort = PlcPort,
                    PlcType = PlcType,
                    BaudRate = BaudRate
                };

                _service.Save(newSettings);

                // Detect changes by comparing with original settings
                var changes = new System.Collections.Generic.List<string>();
                if (_originalSettings.CameraIp != CameraIp) changes.Add("CameraIp");
                if (_originalSettings.CameraPort != CameraPort) changes.Add("CameraPort");
                if (_originalSettings.PlcIp != PlcIp) changes.Add("PlcIp");
                if (_originalSettings.PlcPort != PlcPort) changes.Add("PlcPort");
                if (_originalSettings.PlcType != PlcType) changes.Add("PlcType");
                if (_originalSettings.BaudRate != BaudRate) changes.Add("BaudRate");

                var details = JsonConvert.SerializeObject(new
                {
                    category = "Communication",
                    changes = changes
                });

                _auditLogService.Log(
                    SessionManager.CurrentUserId,
                    "SETTINGS_UPDATE",
                    "SystemConfig",
                    0,
                    details
                );

                System.Windows.MessageBox.Show("保存成功", "提示",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                // Reload to update _originalSettings
                Load();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"保存失败: {ex.Message}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}