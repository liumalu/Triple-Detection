using System;
using Prism.Mvvm;
using Prism.Commands;
using Newtonsoft.Json;
using TripleDetection.Application.SettingsServices;
using TripleDetection.Application.Services;
using TripleDetection.Domain;
using TripleDetection.Presentation.Models;

namespace TripleDetection.Presentation.ViewModels.Settings
{
    public class DeviceControlSettingsViewModel : ViewModelBase
    {
        private readonly DeviceControlSettingsService _service;
        private readonly IAuditLogService _auditLogService;
        private DeviceControlSettings _original;

        public DeviceControlSettingsViewModel(
            DeviceControlSettingsService service,
            IAuditLogService auditLogService)
        {
            _service = service;
            _auditLogService = auditLogService;
        }

        // === 采集参数 ===
        private string _lightSourceType = "LED";
        public string LightSourceType
        {
            get => _lightSourceType;
            set => SetProperty(ref _lightSourceType, value);
        }

        private int _captureDelayMs = 100;
        public int CaptureDelayMs
        {
            get => _captureDelayMs;
            set => SetProperty(ref _captureDelayMs, value);
        }

        private int _captureFeedbackTimeoutMs = 5000;
        public int CaptureFeedbackTimeoutMs
        {
            get => _captureFeedbackTimeoutMs;
            set => SetProperty(ref _captureFeedbackTimeoutMs, value);
        }

        // === 剔除参数 ===
        private int _rejectDelayMs = 50;
        public int RejectDelayMs
        {
            get => _rejectDelayMs;
            set => SetProperty(ref _rejectDelayMs, value);
        }

        private int _rejectDurationMs = 200;
        public int RejectDurationMs
        {
            get => _rejectDurationMs;
            set => SetProperty(ref _rejectDurationMs, value);
        }

        private int _consecutiveRejectsToStopLine = 10;
        public int ConsecutiveRejectsToStopLine
        {
            get => _consecutiveRejectsToStopLine;
            set => SetProperty(ref _consecutiveRejectsToStopLine, value);
        }

        private bool _enableLineStopOnConsecutiveRejects = false;
        public bool EnableLineStopOnConsecutiveRejects
        {
            get => _enableLineStopOnConsecutiveRejects;
            set => SetProperty(ref _enableLineStopOnConsecutiveRejects, value);
        }

        private bool _requireIOConnectionToStartTask = false;
        public bool RequireIOConnectionToStartTask
        {
            get => _requireIOConnectionToStartTask;
            set => SetProperty(ref _requireIOConnectionToStartTask, value);
        }

        // === Modbus TCP ===
        private string _modbusTcpIp = "192.168.1.100";
        public string ModbusTcpIp
        {
            get => _modbusTcpIp;
            set => SetProperty(ref _modbusTcpIp, value);
        }

        private int _modbusTcpPort = 502;
        public int ModbusTcpPort
        {
            get => _modbusTcpPort;
            set => SetProperty(ref _modbusTcpPort, value);
        }

        private int _rejectCoilAddress = 1;
        public int RejectCoilAddress
        {
            get => _rejectCoilAddress;
            set => SetProperty(ref _rejectCoilAddress, value);
        }

        private int _lineStopCoilAddress = 2;
        public int LineStopCoilAddress
        {
            get => _lineStopCoilAddress;
            set => SetProperty(ref _lineStopCoilAddress, value);
        }

        private int _connectionTimeoutMs = 3000;
        public int ConnectionTimeoutMs
        {
            get => _connectionTimeoutMs;
            set => SetProperty(ref _connectionTimeoutMs, value);
        }

        // === Options ===
        public string[] LightSourceTypeOptions => new[] { "LED", "Halogen", "Laser" };

        // === Commands ===
        private DelegateCommand _saveCommand;
        public DelegateCommand SaveCommand => _saveCommand ?? (_saveCommand = new DelegateCommand(ExecuteSave));

        public void Load()
        {
            _original = _service.Load();
            LightSourceType = _original.LightSourceType;
            CaptureDelayMs = _original.CaptureDelayMs;
            CaptureFeedbackTimeoutMs = _original.CaptureFeedbackTimeoutMs;
            RejectDelayMs = _original.RejectDelayMs;
            RejectDurationMs = _original.RejectDurationMs;
            ConsecutiveRejectsToStopLine = _original.ConsecutiveRejectsToStopLine;
            EnableLineStopOnConsecutiveRejects = _original.EnableLineStopOnConsecutiveRejects;
            RequireIOConnectionToStartTask = _original.RequireIOConnectionToStartTask;
            ModbusTcpIp = _original.ModbusTcpIp;
            ModbusTcpPort = _original.ModbusTcpPort;
            RejectCoilAddress = _original.RejectCoilAddress;
            LineStopCoilAddress = _original.LineStopCoilAddress;
            ConnectionTimeoutMs = _original.ConnectionTimeoutMs;
        }

        private void ExecuteSave()
        {
            var newSettings = new DeviceControlSettings
            {
                LightSourceType = LightSourceType,
                CaptureDelayMs = CaptureDelayMs,
                CaptureFeedbackTimeoutMs = CaptureFeedbackTimeoutMs,
                RejectDelayMs = RejectDelayMs,
                RejectDurationMs = RejectDurationMs,
                ConsecutiveRejectsToStopLine = ConsecutiveRejectsToStopLine,
                EnableLineStopOnConsecutiveRejects = EnableLineStopOnConsecutiveRejects,
                RequireIOConnectionToStartTask = RequireIOConnectionToStartTask,
                ModbusTcpIp = ModbusTcpIp,
                ModbusTcpPort = ModbusTcpPort,
                RejectCoilAddress = RejectCoilAddress,
                LineStopCoilAddress = LineStopCoilAddress,
                ConnectionTimeoutMs = ConnectionTimeoutMs
            };

            _service.Save(newSettings);

            var changes = BuildChangedFieldsJson(newSettings);
            var userId = SessionManager.CurrentUserId;
            _auditLogService.Log(userId, "SETTINGS_UPDATE", "DeviceControl", 0, changes);

            System.Windows.MessageBox.Show("保存成功", "提示",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

            Load();
        }

        private string BuildChangedFieldsJson(DeviceControlSettings newSettings)
        {
            var changes = new System.Collections.Generic.Dictionary<string, object>();

            if (_original.LightSourceType != newSettings.LightSourceType)
                changes["LightSourceType"] = newSettings.LightSourceType;
            if (_original.CaptureDelayMs != newSettings.CaptureDelayMs)
                changes["CaptureDelayMs"] = newSettings.CaptureDelayMs;
            if (_original.CaptureFeedbackTimeoutMs != newSettings.CaptureFeedbackTimeoutMs)
                changes["CaptureFeedbackTimeoutMs"] = newSettings.CaptureFeedbackTimeoutMs;
            if (_original.RejectDelayMs != newSettings.RejectDelayMs)
                changes["RejectDelayMs"] = newSettings.RejectDelayMs;
            if (_original.RejectDurationMs != newSettings.RejectDurationMs)
                changes["RejectDurationMs"] = newSettings.RejectDurationMs;
            if (_original.ConsecutiveRejectsToStopLine != newSettings.ConsecutiveRejectsToStopLine)
                changes["ConsecutiveRejectsToStopLine"] = newSettings.ConsecutiveRejectsToStopLine;
            if (_original.EnableLineStopOnConsecutiveRejects != newSettings.EnableLineStopOnConsecutiveRejects)
                changes["EnableLineStopOnConsecutiveRejects"] = newSettings.EnableLineStopOnConsecutiveRejects;
            if (_original.RequireIOConnectionToStartTask != newSettings.RequireIOConnectionToStartTask)
                changes["RequireIOConnectionToStartTask"] = newSettings.RequireIOConnectionToStartTask;
            if (_original.ModbusTcpIp != newSettings.ModbusTcpIp)
                changes["ModbusTcpIp"] = newSettings.ModbusTcpIp;
            if (_original.ModbusTcpPort != newSettings.ModbusTcpPort)
                changes["ModbusTcpPort"] = newSettings.ModbusTcpPort;
            if (_original.RejectCoilAddress != newSettings.RejectCoilAddress)
                changes["RejectCoilAddress"] = newSettings.RejectCoilAddress;
            if (_original.LineStopCoilAddress != newSettings.LineStopCoilAddress)
                changes["LineStopCoilAddress"] = newSettings.LineStopCoilAddress;
            if (_original.ConnectionTimeoutMs != newSettings.ConnectionTimeoutMs)
                changes["ConnectionTimeoutMs"] = newSettings.ConnectionTimeoutMs;

            return JsonConvert.SerializeObject(changes);
        }
    }
}