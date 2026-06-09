using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LLamaCppLauncher.Models;

namespace LLamaCppLauncher.Services;

public class ModelDiscoveryService
{
    public List<string> GetLlamaVersions(string rootDir)
    {
        try
        {
            if (!Directory.Exists(rootDir))
                return new List<string>();

            return Directory.GetDirectories(rootDir)
                .Where(d => File.Exists(Path.Combine(d, "llama-server.exe")))
                .OrderBy(d => d)
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    public List<ModelInfo> GetModels(string modelsDir)
    {
        try
        {
            if (!Directory.Exists(modelsDir))
                return new List<ModelInfo>();

            var ggufFiles = Directory.GetFiles(modelsDir, "*.gguf", SearchOption.AllDirectories)
                .Where(f => !Path.GetFileName(f).Contains("mmproj", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var models = new List<ModelInfo>();

            foreach (var filePath in ggufFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var modelInfo = ParseModelFileName(fileName, filePath);
                models.Add(modelInfo);
            }

            return models.OrderBy(m => m.DisplayName).ToList();
        }
        catch
        {
            return new List<ModelInfo>();
        }
    }

    private static ModelInfo ParseModelFileName(string fileName, string fullPath)
    {
        var modelInfo = new ModelInfo
        {
            FileName = Path.GetFileName(fullPath),
            FullPath = fullPath
        };

        var quantPattern = @"^(.+?)[-\.](Q[0-9A-Z_]+|IQ[0-9A-Z_]+|UD-Q[0-9A-Z_]+|BF16|F16|FP16|FP32|NVFP4)$";
        var match = Regex.Match(fileName, quantPattern, RegexOptions.IgnoreCase);

        if (match.Success)
        {
            modelInfo.Name = match.Groups[1].Value;
            modelInfo.Quantization = match.Groups[2].Value;
        }
        else
        {
            modelInfo.Name = fileName;
            modelInfo.Quantization = string.Empty;
        }

        return modelInfo;
    }
}
