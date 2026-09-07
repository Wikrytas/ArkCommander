using System.IO;
using System.Text.Json;
using ArkCommander.Models;

namespace ArkCommander.Services;

public static class ConfigService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static AppConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            var defaultConfig = new AppConfig();
            Save(path, defaultConfig);
            return defaultConfig;
        }

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppConfig>(json, Options) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public static void Save(string path, AppConfig config)
    {
        string json = JsonSerializer.Serialize(config, Options);
        File.WriteAllText(path, json);
    }
}