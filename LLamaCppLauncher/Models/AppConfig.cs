using System.Text.Json.Serialization;

namespace LLamaCppLauncher.Models;

public class AppConfig
{
    [JsonPropertyName("llamaCppDirectory")]
    public string LlamaCppDirectory { get; set; } = string.Empty;

    [JsonPropertyName("modelsDirectory")]
    public string ModelsDirectory { get; set; } = string.Empty;

    [JsonPropertyName("lastSelectedVersion")]
    public string LastSelectedVersion { get; set; } = string.Empty;

    [JsonPropertyName("lastSelectedModel")]
    public string LastSelectedModel { get; set; } = string.Empty;
}
