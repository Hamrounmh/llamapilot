namespace LLamaCppLauncher.Models;

public static class BenchmarkConfig
{
    public const int Ngl = 999;
    public const string CacheTypeK = "f16";
    public const string CacheTypeV = "f16";
    public const int PromptTokens = 512;
    public const int GenerationTokens = 128;
    public const int Repetitions = 3;
}
