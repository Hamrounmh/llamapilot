namespace LLamaCppLauncher.Models;

public class ModelInfo
{
    public string Name { get; set; } = string.Empty;
    public string Quantization { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;

    public string DisplayName => string.IsNullOrEmpty(Quantization)
        ? Name
        : $"{Name} ({Quantization})";
}
