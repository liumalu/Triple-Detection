using System;
using System.IO;
using Newtonsoft.Json;
using TripleDetection.Domain.Entities;
using TripleDetection.Infrastructure;
using TripleDetection.Infrastructure.Repositories;
using TripleDetection.Presentation.Models;

namespace TripleDetection.Application.SettingsServices
{

public class DeviceControlSettingsService
{
    private readonly string _configPath;
    private DeviceControlSettings _settings;
    private static readonly string DbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "tripledetection.db");

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
        SaveToDb(settings);
    }

    private void SaveToDb(DeviceControlSettings settings)
    {
        var config = new SystemConfig
        {
            Category = "DeviceControl",
            Key = "Settings",
            Value = JsonConvert.SerializeObject(settings)
        };
        var repository = new SystemConfigRepository($"Data Source={DbPath}");
        repository.SaveOrUpdate(config);
    }
}
}