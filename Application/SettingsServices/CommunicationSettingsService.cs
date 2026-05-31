using System;
using System.IO;
using TripleDetection.Infrastructure;
using TripleDetection.Presentation.Models;

namespace TripleDetection.Application.SettingsServices;

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
        _settings = JsonHelper.Load<CommunicationSettings>(_configPath);
        return _settings;
    }

    public void Save(CommunicationSettings settings)
    {
        _settings = settings;
        JsonHelper.Save(settings, _configPath);
    }

    public string GetConfigPath() => _configPath;
}