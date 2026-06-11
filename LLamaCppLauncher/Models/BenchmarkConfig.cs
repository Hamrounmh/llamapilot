namespace LLamaCppLauncher.Models;

public static class BenchmarkConfig
{
    public static int Ngl { get; set; } = 999;
    public static string CacheTypeK { get; set; } = "f16";
    public static string CacheTypeV { get; set; } = "f16";
    public static int PromptTokens { get; set; } = 512;
    public static int GenerationTokens { get; set; } = 128;
    public static int Repetitions { get; set; } = 3;
}
