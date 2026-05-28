using System;
using System.IO;
using TripleDetection.Models;

namespace TripleDetection.Services
{
    public class CommunicationSettingsService
    {
        private readonly string _configPath;
        private CommunicationSettings _settings;

        public CommunicationSettingsService()
        {
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "communication.json");
        }

        public CommunicationSettings Load()
        {
            _settings = SimpleJsonHelper.Load<CommunicationSettings>(_configPath);
            return _settings;
        }

        public void Save(CommunicationSettings settings)
        {
            _settings = settings;
            SimpleJsonHelper.Save(settings, _configPath);
        }

        public string GetConfigPath() => _configPath;
    }
}