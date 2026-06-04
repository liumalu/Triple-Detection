using System;
using System.IO;
using Newtonsoft.Json;
using TripleDetection.Domain.Entities;
using TripleDetection.Infrastructure;
using TripleDetection.Infrastructure.Repositories;
using TripleDetection.Presentation.Models;

namespace TripleDetection.Application.SettingsServices
{

public class VmSettingsService
{
    private readonly string _configPath;
    private static readonly string DbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "tripledetection.db");
    private VmSettings _settings;

    public VmSettingsService()
    {
        _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "vm_settings.json");
    }

    public VmSettings Load()
    {
        _settings = JsonHelper.Load<VmSettings>(_configPath);
        return _settings;
    }

    public void Save(VmSettings settings)
    {
        _settings = settings;
        JsonHelper.Save(settings, _configPath);
        SaveToDb(settings);
    }

    private void SaveToDb(VmSettings settings)
    {
        var systemConfig = new SystemConfig
        {
            Category = "VmSettings",
            Key = "Settings",
            Value = JsonConvert.SerializeObject(settings)
        };
        var repository = new SystemConfigRepository($"Data Source={DbPath}");
        repository.SaveOrUpdate(systemConfig);
    }
}
}