using System;
using System.IO;
using Newtonsoft.Json;
using TripleDetection.Domain.Entities;
using TripleDetection.Infrastructure;
using TripleDetection.Infrastructure.Repositories;
using TripleDetection.Presentation.Models;

namespace TripleDetection.Application.SettingsServices
{

public class CommunicationSettingsService
{
    private static readonly string DbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "tripledetection.db");
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
        SaveToDb(settings);
    }

    public void SaveToDb(CommunicationSettings settings)
    {
        var config = new SystemConfig
        {
            Category = "Communication",
            Key = "Settings",
            Value = JsonConvert.SerializeObject(settings)
        };
        var repo = new SystemConfigRepository($"Data Source={DbPath}");
        repo.SaveOrUpdate(config);
    }

    public string GetConfigPath() => _configPath;
}
}