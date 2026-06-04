using System;
using System.IO;
using TripleDetection.Domain.Entities;
using TripleDetection.Infrastructure;
using TripleDetection.Presentation.Models;

namespace TripleDetection.Application.SettingsServices
{

public class SystemSettingsService
{
    private readonly string _configPath;
    private SystemSettings _settings;
    private static readonly string DbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "tripledetection.db");

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
        SaveToDb(settings);
    }

    private void SaveToDb(SystemSettings settings)
    {
        var systemConfig = new SystemConfig
        {
            Category = "System",
            Key = "Settings",
            Value = Newtonsoft.Json.JsonConvert.SerializeObject(settings)
        };
        var repo = new TripleDetection.Infrastructure.Repositories.SystemConfigRepository($"Data Source={DbPath}");
        repo.SaveOrUpdate(systemConfig);
    }
}
}