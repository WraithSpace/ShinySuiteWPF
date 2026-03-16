using System;
using System.IO;
using System.Text.Json;
using ShinySuite.Models;

namespace ShinySuite.Services;

public static class ConfigService
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ShinySuite",
        "shinysuite_config.json");

    private static readonly string LegacyPath = Path.Combine(
        AppContext.BaseDirectory,
        "shinysuite_config.json");

    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

    public static AppConfig Load()
    {
        try
        {
            // Migrate from legacy location (next to exe) on first run
            if (!File.Exists(ConfigPath) && File.Exists(LegacyPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
                File.Move(LegacyPath, ConfigPath);
            }

            if (!File.Exists(ConfigPath)) return new();
            var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath), Opts) ?? new();
            MigrateGameNames(config);
            return config;
        }
        catch { return new(); }
    }

    private static readonly Dictionary<string, string> _gameRenames = new()
    {
        ["HeartGold / SoulSilver"] = "Heart Gold / Soul Silver",
        ["FireRed / LeafGreen"]    = "Fire Red / Leaf Green",
    };

    private static void MigrateGameNames(AppConfig config)
    {
        if (_gameRenames.TryGetValue(config.Game, out var g)) config.Game = g;
        foreach (var r in config.Routes.Concat(config.ArchivedRoutes))
            if (_gameRenames.TryGetValue(r.Game, out var rg)) r.Game = rg;
    }

    public static void Save(AppConfig config)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, Opts));
        }
        catch { }
    }
}
