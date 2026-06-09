namespace LLamaCppLauncher.Models;

public class ModelInfo
{
    public string Name { get; set; } = string.Empty;
    public string Quantization { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public ulong ParameterCount { get; set; }
    public string Architecture { get; set; } = string.Empty;
    public ulong ContextLength { get; set; }
    public double BenchmarkPP { get; set; }
    public double BenchmarkTG { get; set; }

    public string DisplayName => string.IsNullOrEmpty(Quantization)
        ? Name
        : $"{Name} ({Quantization})";
}
