using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LLamaCppLauncher.Services;

public class LlamaProcessService
{
    private Process? _process;
    private CancellationTokenSource? _cts;
    private readonly LocalizationService _loc = LocalizationService.Instance;

    public bool IsRunning => _process != null && !_process.HasExited;

    public async Task StartAsync(
        string llamaDir,
        string modelPath,
        string host,
        string port,
        Dictionary<string, string> parameters,
        Action<string> onOutput,
        Action<string> onError,
        Action onExit)
    {
        if (IsRunning)
            throw new InvalidOperationException(_loc["svc.process_already_running"]);

        _cts = new CancellationTokenSource();

        var exePath = Path.Combine(llamaDir, "llama-server.exe");
        if (!File.Exists(exePath))
            throw new FileNotFoundException(_loc.Format("svc.server_not_found", llamaDir));

        var args = BuildArguments(modelPath, host, port, parameters);

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = args,
            WorkingDirectory = llamaDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        _process = new Process { StartInfo = startInfo };

        _process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
                onOutput(e.Data);
        };

        _process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                onError(e.Data);
        };

        _process.EnableRaisingEvents = true;
        _process.Exited += (_, _) =>
        {
            onExit();
        };

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        onOutput(_loc.Format("svc.log.server_started", _process.Id));
        onOutput(_loc.Format("svc.log.directory", llamaDir));
        onOutput(_loc.Format("svc.log.model", modelPath));
        onOutput(_loc.Format("svc.log.command", args));
        onOutput("");

        try
        {
            await _process.WaitForExitAsync(_cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void Stop()
    {
        if (_process == null || _process.HasExited)
            return;

        try
        {
            _cts?.Cancel();
            _process.Kill(true);
            _process.Dispose();
            _process = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping process: {ex.Message}");
        }
    }

    private static string BuildArguments(string modelPath, string host, string port, Dictionary<string, string> parameters)
    {
        var flagParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "--jinja" };

        var parts = new List<string>
        {
            $"--model \"{modelPath}\"",
            $"--host {host}",
            $"--port {port}"
        };

        foreach (var param in parameters)
        {
            if (string.IsNullOrWhiteSpace(param.Value))
                continue;

            if (flagParams.Contains(param.Key))
            {
                if (param.Value.Equals("on", StringComparison.OrdinalIgnoreCase))
                    parts.Add(param.Key);
            }
            else
            {
                if (param.Value.Contains(' ') || param.Value.Contains('{') || param.Value.Contains('"'))
                    parts.Add($"{param.Key} \"{param.Value.Replace("\"", "\\\"")}\"");
                else
                    parts.Add($"{param.Key} {param.Value}");
            }
        }

        return string.Join(" ", parts);
    }
}
