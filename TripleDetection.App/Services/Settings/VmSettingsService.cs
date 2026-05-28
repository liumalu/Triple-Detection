using System;
using System.IO;
using TripleDetection.Models;

namespace TripleDetection.Services
{
    public class VmSettingsService
    {
        private readonly string _configPath;
        private VmSettings _settings;

        public VmSettingsService()
        {
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "vm_settings.json");
        }

        public VmSettings Load()
        {
            _settings = SimpleJsonHelper.Load<VmSettings>(_configPath);
            return _settings;
        }

        public void Save(VmSettings settings)
        {
            _settings = settings;
            SimpleJsonHelper.Save(settings, _configPath);
        }
    }
}