using System;
using System.IO;
using TripleDetection.Models;

namespace TripleDetection.Services
{
    public class SystemSettingsService
    {
        private readonly string _configPath;
        private SystemSettings _settings;

        public SystemSettingsService()
        {
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "system.json");
        }

        public SystemSettings Load()
        {
            _settings = SimpleJsonHelper.Load<SystemSettings>(_configPath);
            return _settings;
        }

        public void Save(SystemSettings settings)
        {
            _settings = settings;
            SimpleJsonHelper.Save(settings, _configPath);
        }
    }
}