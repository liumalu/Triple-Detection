using System;
using System.IO;
using TripleDetection.Data;
using TripleDetection.Models;

namespace TripleDetection.Services
{
    public class DeviceControlSettingsService
    {
        private readonly string _configPath;
        private DeviceControlSettings _settings;

        public DeviceControlSettingsService()
        {
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "device_control.json");
        }

        public DeviceControlSettings Load()
        {
            _settings = JsonHelper.Load<DeviceControlSettings>(_configPath);
            return _settings;
        }

        public void Save(DeviceControlSettings settings)
        {
            _settings = settings;
            JsonHelper.Save(settings, _configPath);
        }
    }
}