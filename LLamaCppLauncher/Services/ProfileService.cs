using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LLamaCppLauncher.Models;

namespace LLamaCppLauncher.Services;

public class ProfileService
{
    private readonly string _profilesDir;

    public ProfileService()
    {
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        _profilesDir = Path.Combine(exeDir, "profiles");
        Directory.CreateDirectory(_profilesDir);
    }

    public void SaveProfile(string name, LaunchProfile profile)
    {
        try
        {
            profile.Name = name;
            var filePath = Path.Combine(_profilesDir, $"{SanitizeFileName(name)}.json");
            var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la sauvegarde du profil : {ex.Message}");
        }
    }

    public LaunchProfile? LoadProfile(string name)
    {
        try
        {
            var filePath = Path.Combine(_profilesDir, $"{SanitizeFileName(name)}.json");
            if (!File.Exists(filePath))
                return null;

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<LaunchProfile>(json);
        }
        catch
        {
            return null;
        }
    }

    public List<string> GetProfileNames()
    {
        try
        {
            if (!Directory.Exists(_profilesDir))
                return new List<string>();

            return Directory.GetFiles(_profilesDir, "*.json")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .OrderBy(n => n)
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    public void DeleteProfile(string name)
    {
        try
        {
            var filePath = Path.Combine(_profilesDir, $"{SanitizeFileName(name)}.json");
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la suppression du profil : {ex.Message}");
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Where(c => !invalid.Contains(c)).ToArray());
    }
}
