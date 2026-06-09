using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LLamaCppLauncher.Models;

public class LaunchProfile
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public Dictionary<string, string> Parameters { get; set; } = new();
}
