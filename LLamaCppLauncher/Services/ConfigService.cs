using System;
using System.IO;
using System.Text.Json;
using LLamaCppLauncher.Models;

namespace LLamaCppLauncher.Services;

public class ConfigService
{
    private readonly string _configPath;

    public ConfigService()
    {
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        _configPath = Path.Combine(exeDir, "config.json");
    }

    public AppConfig Load()
    {
        try
        {
            if (!File.Exists(_configPath))
                return new AppConfig();

            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public void Save(AppConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la sauvegarde de la config : {ex.Message}");
        }
    }
}
