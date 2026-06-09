using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LLamaCppLauncher.Services;

public class CommandParserService
{
    private static readonly HashSet<string> FlagParameters = new(StringComparer.OrdinalIgnoreCase)
    {
        "--jinja"
    };

    public Dictionary<string, string> ParseCommand(string commandText)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(commandText))
            return result;

        var cleaned = commandText
            .Replace("^", "")
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("\\", "");

        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

        var tokens = Tokenize(cleaned);

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            if (!token.StartsWith("-"))
                continue;

            var paramName = token;

            if (token == "--model" || token == "llama-server")
            {
                if (i + 1 < tokens.Count && !tokens[i + 1].StartsWith("-"))
                    i++;
                continue;
            }

            if (FlagParameters.Contains(paramName))
            {
                result[paramName] = "on";
                continue;
            }

            if (i + 1 < tokens.Count && !tokens[i + 1].StartsWith("-"))
            {
                result[paramName] = tokens[i + 1];
                i++;
            }
            else
            {
                result[paramName] = "on";
            }
        }

        return result;
    }

    public string BuildCommand(string modelPath, string host, string port, Dictionary<string, string> parameters)
    {
        var parts = new List<string>
        {
            "llama-server",
            $"--model \"{modelPath}\"",
            $"--host {host}",
            $"--port {port}"
        };

        foreach (var param in parameters)
        {
            if (string.IsNullOrWhiteSpace(param.Value))
                continue;

            if (FlagParameters.Contains(param.Key))
            {
                if (param.Value.Equals("on", StringComparison.OrdinalIgnoreCase))
                    parts.Add(param.Key);
            }
            else
            {
                if (param.Value.Contains(' ') || param.Value.Contains('{') || param.Value.Contains('"'))
                    parts.Add($"{param.Key} \"{EscapeQuotes(param.Value)}\"");
                else
                    parts.Add($"{param.Key} {param.Value}");
            }
        }

        return string.Join(" ^\n  ", parts);
    }

    private static List<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < input.Length; i++)
        {
            var c = input[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (c == ' ' && !inQuotes)
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
                continue;
            }

            current.Append(c);
        }

        if (current.Length > 0)
            tokens.Add(current.ToString());

        return tokens;
    }

    private static string EscapeQuotes(string value)
    {
        return value.Replace("\"", "\\\"");
    }
}
