namespace LLamaCppLauncher.Models;

public class BenchmarkResult
{
    public string ModelName { get; set; } = string.Empty;
    public string ModelQuantization { get; set; } = string.Empty;
    public string ModelSize { get; set; } = string.Empty;
    public string ModelParams { get; set; } = string.Empty;
    public string LlamaVersion { get; set; } = string.Empty;
    public string Backend { get; set; } = string.Empty;
    public double PromptProcessingTs { get; set; }
    public double GenerationTs { get; set; }
    public string PromptProcessingRaw { get; set; } = string.Empty;
    public string GenerationRaw { get; set; } = string.Empty;
    public bool HasError { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
