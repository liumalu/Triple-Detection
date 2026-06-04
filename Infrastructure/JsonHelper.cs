using System;
using System.IO;
using Newtonsoft.Json;

namespace TripleDetection.Infrastructure
{

public static class JsonHelper
{
    public static T Load<T>(string filePath) where T : class
    {
        try
        {
            if (!File.Exists(filePath)) return null;
            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[JsonHelper.Load] ERROR: {ex.Message}");
            return null;
        }
    }

    public static void Save<T>(T obj, string filePath) where T : class
    {
        try
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[JsonHelper.Save] ERROR: {ex.Message}");
        }
    }
}
}