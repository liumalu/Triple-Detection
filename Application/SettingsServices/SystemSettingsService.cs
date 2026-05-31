using System;
using System.IO;
using TripleDetection.Infrastructure;
using TripleDetection.Presentation.Models;

namespace TripleDetection.Application.SettingsServices;

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
        _settings = JsonHelper.Load<SystemSettings>(_configPath);
        return _settings;
    }

    public void Save(SystemSettings settings)
    {
        _settings = settings;
        JsonHelper.Save(settings, _configPath);
    }
}