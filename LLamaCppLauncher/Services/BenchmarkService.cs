using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LLamaCppLauncher.Models;

namespace LLamaCppLauncher.Services;

public class BenchmarkService
{
    private readonly string _benchmarkFilePath;

    public BenchmarkService()
    {
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        _benchmarkFilePath = Path.Combine(exeDir, "benchmark.md");
    }

    public async Task<BenchmarkResult?> RunBenchmarkAsync(
        string llamaDir,
        string modelPath,
        string modelName,
        string modelQuant,
        Action<string> onLog)
    {
        var benchExe = Path.Combine(llamaDir, "llama-bench.exe");
        if (!File.Exists(benchExe))
        {
            onLog($"[ERREUR] llama-bench.exe introuvable dans {llamaDir}");
            return new BenchmarkResult
            {
                ModelName = modelName,
                ModelQuantization = modelQuant,
                LlamaVersion = Path.GetFileName(llamaDir),
                HasError = true,
                ErrorMessage = "llama-bench.exe introuvable"
            };
        }

        var args = $"-m \"{modelPath}\" -ngl {BenchmarkConfig.Ngl} " +
                   $"-ctk {BenchmarkConfig.CacheTypeK} -ctv {BenchmarkConfig.CacheTypeV} " +
                   $"-p {BenchmarkConfig.PromptTokens} -n {BenchmarkConfig.GenerationTokens} " +
                   $"-r {BenchmarkConfig.Repetitions}";

        onLog($"[BENCHMARK] Commande: llama-bench {args}");

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = benchExe,
                Arguments = args,
                WorkingDirectory = llamaDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = startInfo };

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                    onLog(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    errorBuilder.AppendLine(e.Data);
                    onLog($"[STDERR] {e.Data}");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            var output = outputBuilder.ToString();
            var result = ParseBenchmarkOutput(output);

            if (result == null)
            {
                return new BenchmarkResult
                {
                    ModelName = modelName,
                    ModelQuantization = modelQuant,
                    LlamaVersion = Path.GetFileName(llamaDir),
                    HasError = true,
                    ErrorMessage = "Impossible de parser la sortie de llama-bench"
                };
            }

            result.ModelName = modelName;
            result.ModelQuantization = modelQuant;
            result.LlamaVersion = Path.GetFileName(llamaDir);

            return result;
        }
        catch (Exception ex)
        {
            onLog($"[ERREUR] {ex.Message}");
            return new BenchmarkResult
            {
                ModelName = modelName,
                ModelQuantization = modelQuant,
                LlamaVersion = Path.GetFileName(llamaDir),
                HasError = true,
                ErrorMessage = ex.Message
            };
        }
    }

    public BenchmarkResult? ParseBenchmarkOutput(string output)
    {
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var dataLines = lines
            .Where(l => l.TrimStart().StartsWith("|") && !l.Contains("---"))
            .Skip(1)
            .ToList();

        if (dataLines.Count < 2)
            return null;

        var result = new BenchmarkResult();

        foreach (var line in dataLines)
        {
            var cells = line.Split('|')
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();

            if (cells.Count < 7)
                continue;

            int testIndex;
            int tsIndex;

            if (cells.Count >= 9)
            {
                testIndex = 7;
                tsIndex = 8;
            }
            else
            {
                testIndex = 5;
                tsIndex = 6;
            }

            if (string.IsNullOrEmpty(result.ModelSize))
                result.ModelSize = cells[1];
            if (string.IsNullOrEmpty(result.ModelParams))
                result.ModelParams = cells[2];
            if (string.IsNullOrEmpty(result.Backend))
                result.Backend = cells[3];

            var test = cells[testIndex].ToLowerInvariant();
            var ts = cells[tsIndex];

            if (test.Contains("pp"))
            {
                result.PromptProcessingRaw = ts;
                result.PromptProcessingTs = ExtractTsValue(ts);
            }
            else if (test.Contains("tg"))
            {
                result.GenerationRaw = ts;
                result.GenerationTs = ExtractTsValue(ts);
            }
        }

        if (string.IsNullOrEmpty(result.PromptProcessingRaw) && string.IsNullOrEmpty(result.GenerationRaw))
            return null;

        return result;
    }

    private static double ExtractTsValue(string tsValue)
    {
        var match = Regex.Match(tsValue, @"(\d+\.?\d*)");
        if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            return value;
        return 0;
    }

    public List<BenchmarkResult> LoadExistingResults()
    {
        if (!File.Exists(_benchmarkFilePath))
            return new List<BenchmarkResult>();

        try
        {
            var content = File.ReadAllText(_benchmarkFilePath);
            return ParseMarkdownTable(content);
        }
        catch
        {
            return new List<BenchmarkResult>();
        }
    }

    private List<BenchmarkResult> ParseMarkdownTable(string markdown)
    {
        var results = new List<BenchmarkResult>();
        var lines = markdown.Split('\n');
        var inTable = false;

        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("| Modèle"))
            {
                inTable = true;
                continue;
            }

            if (inTable && line.Contains("---"))
                continue;

            if (inTable && line.TrimStart().StartsWith("|"))
            {
                var cells = line.Split('|')
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToList();

                if (cells.Count >= 8)
                {
                    var result = new BenchmarkResult
                    {
                        ModelName = cells[0],
                        ModelQuantization = cells[1],
                        LlamaVersion = cells[2],
                        Backend = cells[3],
                        ModelSize = cells[4],
                        ModelParams = cells[5],
                        PromptProcessingRaw = cells[6],
                        GenerationRaw = cells[7],
                        PromptProcessingTs = cells[6] == "ERREUR" ? 0 : ExtractTsValue(cells[6]),
                        GenerationTs = cells[7] == "ERREUR" ? 0 : ExtractTsValue(cells[7]),
                        HasError = cells[6] == "ERREUR" || cells[7] == "ERREUR"
                    };
                    results.Add(result);
                }
            }
            else if (inTable && !line.TrimStart().StartsWith("|"))
            {
                inTable = false;
            }
        }

        return results;
    }

    public void SaveResults(List<BenchmarkResult> results)
    {
        var markdown = GenerateMarkdownTable(results);
        File.WriteAllText(_benchmarkFilePath, markdown, Encoding.UTF8);
    }

    public string GenerateMarkdownTable(List<BenchmarkResult> results)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Benchmark Results");
        sb.AppendLine();
        sb.AppendLine($"**Date de dernière mise à jour :** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("**Paramètres fixes :**");
        sb.AppendLine($"- NGL: {BenchmarkConfig.Ngl}");
        sb.AppendLine($"- Cache Type K/V: {BenchmarkConfig.CacheTypeK}");
        sb.AppendLine($"- Prompt tokens: {BenchmarkConfig.PromptTokens}");
        sb.AppendLine($"- Generation tokens: {BenchmarkConfig.GenerationTokens}");
        sb.AppendLine($"- Repetitions: {BenchmarkConfig.Repetitions}");
        sb.AppendLine();
        sb.AppendLine("## Résultats");
        sb.AppendLine();
        sb.AppendLine("| Modèle | Quant | Version | Backend | Size | Params | PP (t/s) | TG (t/s) |");
        sb.AppendLine("|--------|-------|---------|---------|------|--------|----------|----------|");

        var sorted = results
            .OrderByDescending(r => r.HasError ? 0 : r.PromptProcessingTs)
            .ToList();

        foreach (var r in sorted)
        {
            var pp = r.HasError ? "ERREUR" : r.PromptProcessingRaw;
            var tg = r.HasError ? "ERREUR" : r.GenerationRaw;
            sb.AppendLine($"| {r.ModelName} | {r.ModelQuantization} | {r.LlamaVersion} | {r.Backend} | {r.ModelSize} | {r.ModelParams} | {pp} | {tg} |");
        }

        var errors = results.Where(r => r.HasError).ToList();
        if (errors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Détails des erreurs");
            sb.AppendLine();
            foreach (var e in errors)
            {
                sb.AppendLine($"### {e.ModelName} ({e.ModelQuantization}) - {e.LlamaVersion}");
                sb.AppendLine("```");
                sb.AppendLine(e.ErrorMessage);
                sb.AppendLine("```");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    public bool BenchmarkExists(
        List<BenchmarkResult> results,
        string llamaVersion,
        string modelName,
        string modelQuant)
    {
        return results.Any(r =>
            r.LlamaVersion.Equals(llamaVersion, StringComparison.OrdinalIgnoreCase) &&
            r.ModelName.Equals(modelName, StringComparison.OrdinalIgnoreCase) &&
            r.ModelQuantization.Equals(modelQuant, StringComparison.OrdinalIgnoreCase));
    }
}
