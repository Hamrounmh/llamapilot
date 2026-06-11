using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LLamaCppLauncher.Models;
using LLamaCppLauncher.Services;
using LLamaCppLauncher.Windows;
using Microsoft.Win32;
using WpfApplication = System.Windows.Application;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;
using WpfClipboard = System.Windows.Clipboard;
using WpfWindow = System.Windows.Window;

namespace LLamaCppLauncher.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ConfigService _configService;
    private readonly ProfileService _profileService;
    private readonly ModelDiscoveryService _modelDiscoveryService;
    private readonly CommandParserService _commandParserService;
    private readonly LlamaProcessService _llamaProcessService;
    private readonly BenchmarkService _benchmarkService;
    private readonly LocalizationService _loc = LocalizationService.Instance;

    [ObservableProperty]
    private string _llamaCppDirectory = string.Empty;

    [ObservableProperty]
    private string _modelsDirectory = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _llamaVersions = new();

    [ObservableProperty]
    private string? _selectedVersion;

    [ObservableProperty]
    private ObservableCollection<ModelInfo> _models = new();

    [ObservableProperty]
    private ModelInfo? _selectedModel;

    [ObservableProperty]
    private ObservableCollection<LlamaParameter> _parameters = new();

    [ObservableProperty]
    private string _host = "127.0.0.1";

    [ObservableProperty]
    private string _port = "8080";

    [ObservableProperty]
    private ObservableCollection<string> _logs = new();

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private ObservableCollection<string> _profileNames = new();

    [ObservableProperty]
    private string? _selectedProfile;

    [ObservableProperty]
    private bool _isBenchmarking;

    [ObservableProperty]
    private int _benchmarkProgress;

    [ObservableProperty]
    private int _benchmarkTotal;

    [ObservableProperty]
    private string _benchmarkStatus = string.Empty;

    [ObservableProperty]
    private BenchmarkResult? _selectedBenchmarkResult;

    [ObservableProperty]
    private bool _hasSelectedBenchmark;

    [ObservableProperty]
    private string _selectedBenchmarkDisplay = string.Empty;

    [ObservableProperty]
    private string _selectedBenchmarkColor = "#4EC9B0";

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private string _statusBarVersion = string.Empty;

    [ObservableProperty]
    private string _statusBarModel = string.Empty;

    [ObservableProperty]
    private string _logCountDisplay = string.Empty;

    [ObservableProperty]
    private string _benchmarkNgl = "999";

    [ObservableProperty]
    private string _benchmarkCacheTypeK = "f16";

    [ObservableProperty]
    private string _benchmarkCacheTypeV = "f16";

    [ObservableProperty]
    private string _benchmarkPromptTokens = "512";

    [ObservableProperty]
    private string _benchmarkGenerationTokens = "128";

    [ObservableProperty]
    private string _benchmarkRepetitions = "3";

    public string LanguageLabel => _loc.LanguageLabel;

    public MainViewModel()
    {
        _configService = new ConfigService();
        _profileService = new ProfileService();
        _modelDiscoveryService = new ModelDiscoveryService();
        _commandParserService = new CommandParserService();
        _llamaProcessService = new LlamaProcessService();
        _benchmarkService = new BenchmarkService();

        InitializeParameters();
        LoadConfiguration();
        LoadDefaultProfile();
        RefreshProfiles();
        UpdateStatusText();
        UpdateStatusBarVersion();
        UpdateStatusBarModel();
        UpdateLogCountDisplay();

        Logs.CollectionChanged += (_, _) => UpdateLogCountDisplay();
        _loc.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(LanguageLabel));
        UpdateStatusText();
        UpdateStatusBarVersion();
        UpdateStatusBarModel();
        UpdateLogCountDisplay();
        UpdateSelectedBenchmarkResult();
    }

    private void UpdateStatusText()
    {
        StatusText = IsRunning ? _loc["status.running"] : _loc["status.stopped"];
    }

    private void UpdateStatusBarVersion()
    {
        StatusBarVersion = SelectedVersion != null
            ? _loc.Format("status.version_format", SelectedVersion)
            : _loc["status.version_none"];
    }

    private void UpdateStatusBarModel()
    {
        StatusBarModel = SelectedModel != null
            ? _loc.Format("status.model_format", SelectedModel.DisplayName)
            : _loc["status.model_none"];
    }

    private void UpdateLogCountDisplay()
    {
        LogCountDisplay = $"({Logs.Count} lines)";
    }

    private void InitializeParameters()
    {
        Parameters = new ObservableCollection<LlamaParameter>
        {
            new() { Name = "-ngl", DisplayName = "GPU Layers", DefaultValue = "999" },
            new() { Name = "--parallel", DisplayName = "Parallel", DefaultValue = "1" },
            new() { Name = "--ctx-size", DisplayName = "Context Size", DefaultValue = "2048" },
            new() { Name = "--flash-attn", DisplayName = "Flash Attention", DefaultValue = "on" },
            new() { Name = "--cache-type-k", DisplayName = "Cache Type K", DefaultValue = "q8_0" },
            new() { Name = "--cache-type-v", DisplayName = "Cache Type V", DefaultValue = "q8_0" },
            new() { Name = "--batch-size", DisplayName = "Batch Size", DefaultValue = "2048" },
            new() { Name = "--ubatch-size", DisplayName = "Ubatch Size", DefaultValue = "512" },
            new() { Name = "--temp", DisplayName = "Temperature", DefaultValue = "0.8" },
            new() { Name = "--top-p", DisplayName = "Top-p", DefaultValue = "0.95" },
            new() { Name = "--top-k", DisplayName = "Top-k", DefaultValue = "40" },
            new() { Name = "--presence-penalty", DisplayName = "Presence Penalty", DefaultValue = "0" },
            new() { Name = "--spec-type", DisplayName = "Spec Type", DefaultValue = "draft-mtp" },
            new() { Name = "--spec-draft-n-max", DisplayName = "Spec Draft N Max", DefaultValue = "2" },
            new() { Name = "--jinja", DisplayName = "Jinja", DefaultValue = "", IsFlag = true },
            new() { Name = "--chat-template-kwargs", DisplayName = "Chat Template Kwargs", DefaultValue = "{\"preserve_thinking\": true}" }
        };
    }

    private void LoadConfiguration()
    {
        var config = _configService.Load();

        LlamaCppDirectory = config.LlamaCppDirectory;
        ModelsDirectory = config.ModelsDirectory;

        if (!string.IsNullOrEmpty(LlamaCppDirectory))
        {
            RefreshVersions();
            if (!string.IsNullOrEmpty(config.LastSelectedVersion))
                SelectedVersion = LlamaVersions.FirstOrDefault(v => v == config.LastSelectedVersion);
        }

        if (!string.IsNullOrEmpty(ModelsDirectory))
        {
            RefreshModels();
            if (!string.IsNullOrEmpty(config.LastSelectedModel))
                SelectedModel = Models.FirstOrDefault(m => m.FullPath == config.LastSelectedModel);
        }
    }

    private void LoadDefaultProfile()
    {
        var defaultProfile = new LaunchProfile
        {
            Name = "default",
            Parameters = new Dictionary<string, string>
            {
                { "-ngl", "999" },
                { "--ctx-size", "65000" }
            }
        };

        _profileService.SaveProfile("default", defaultProfile);
        RefreshProfiles();
        SelectedProfile = "default";

        foreach (var param in Parameters)
        {
            if (defaultProfile.Parameters.TryGetValue(param.Name, out var value))
                param.Value = value;
            else
                param.Value = string.Empty;
        }
    }

    private void SaveConfiguration()
    {
        var config = new AppConfig
        {
            LlamaCppDirectory = LlamaCppDirectory,
            ModelsDirectory = ModelsDirectory,
            LastSelectedVersion = SelectedVersion ?? string.Empty,
            LastSelectedModel = SelectedModel?.FullPath ?? string.Empty,
            Language = _loc.CurrentLanguage
        };
        _configService.Save(config);
    }

    private void RefreshVersions()
    {
        var versions = _modelDiscoveryService.GetLlamaVersions(LlamaCppDirectory);
        LlamaVersions = new ObservableCollection<string>(versions);
    }

    private void RefreshModels()
    {
        var models = _modelDiscoveryService.GetModels(ModelsDirectory);
        Models = new ObservableCollection<ModelInfo>(models);
    }

    private void RefreshProfiles()
    {
        var names = _profileService.GetProfileNames();
        ProfileNames = new ObservableCollection<string>(names);
    }

    [RelayCommand]
    private void BrowseLlamaDir()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = _loc["vm.select_llama_dir"]
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            LlamaCppDirectory = dialog.SelectedPath;
            RefreshVersions();
            SaveConfiguration();
        }
    }

    [RelayCommand]
    private void BrowseModelsDir()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = _loc["vm.select_models_dir"]
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            ModelsDirectory = dialog.SelectedPath;
            RefreshModels();
            SaveConfiguration();
        }
    }

    [RelayCommand]
    private void RefreshVersionsCommand()
    {
        if (!string.IsNullOrEmpty(LlamaCppDirectory))
        {
            RefreshVersions();
            AddLog(_loc["vm.log.versions_refreshed"]);
        }
    }

    [RelayCommand]
    private void RefreshModelsCommand()
    {
        if (!string.IsNullOrEmpty(ModelsDirectory))
        {
            RefreshModels();
            AddLog(_loc["vm.log.models_refreshed"]);
        }
    }

    [RelayCommand]
    private void ManageModels()
    {
        var dialog = new ManageModelsWindow(
            _modelDiscoveryService,
            _benchmarkService,
            SelectedVersion ?? string.Empty,
            ModelsDirectory);
        dialog.Owner = WpfApplication.Current.MainWindow;
        dialog.ShowDialog();
        RefreshModels();
    }

    [RelayCommand]
    private void SaveProfile()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = _loc["vm.json_filter"],
            DefaultExt = ".json",
            Title = _loc["vm.save_profile_title"]
        };

        if (dialog.ShowDialog() == true)
        {
            var profile = new LaunchProfile
            {
                Name = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName),
                Parameters = Parameters
                    .Where(p => !string.IsNullOrWhiteSpace(p.Value))
                    .ToDictionary(p => p.Name, p => p.Value)
            };

            _profileService.SaveProfile(profile.Name, profile);
            RefreshProfiles();
            AddLog(_loc.Format("vm.log.profile_saved", profile.Name));
        }
    }

    [RelayCommand]
    private void LoadProfile()
    {
        if (SelectedProfile == null)
        {
            WpfMessageBox.Show(_loc["vm.select_profile_msg"], _loc["vm.error"], WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
            return;
        }

        var profile = _profileService.LoadProfile(SelectedProfile);
        if (profile == null)
        {
            WpfMessageBox.Show(_loc["vm.profile_not_found"], _loc["vm.error"], WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
            return;
        }

        foreach (var param in Parameters)
        {
            if (profile.Parameters.TryGetValue(param.Name, out var value))
                param.Value = value;
            else
                param.Value = string.Empty;
        }

        AddLog(_loc.Format("vm.log.profile_loaded", SelectedProfile));
    }

    [RelayCommand]
    private void ImportCommand()
    {
        var dialog = new ImportCommandDialog();
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.CommandText))
        {
            var parsed = _commandParserService.ParseCommand(dialog.CommandText);

            foreach (var param in Parameters)
            {
                if (parsed.TryGetValue(param.Name, out var value))
                    param.Value = value;
                else
                    param.Value = string.Empty;
            }

            AddLog(_loc["vm.log.command_imported"]);
        }
    }

    [RelayCommand]
    private void OpenServer()
    {
        var url = $"http://{Host}:{Port}";
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private void SwitchLanguage()
    {
        _loc.ToggleLanguage();
        SaveConfiguration();
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task Start()
    {
        if (SelectedVersion == null || SelectedModel == null)
        {
            WpfMessageBox.Show(_loc["vm.select_version_model_msg"], _loc["vm.error"], WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
            return;
        }

        IsRunning = true;
        UpdateStatusText();
        StartCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        RestartCommand.NotifyCanExecuteChanged();

        var parameters = Parameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .ToDictionary(p => p.Name, p => p.Value);

        try
        {
            await _llamaProcessService.StartAsync(
                SelectedVersion,
                SelectedModel.FullPath,
                Host,
                Port,
                parameters,
                line => WpfApplication.Current.Dispatcher.Invoke(() => AddLog(line)),
                line => WpfApplication.Current.Dispatcher.Invoke(() => AddLog(_loc.Format("vm.log.error_prefix", line))),
                () => WpfApplication.Current.Dispatcher.Invoke(() =>
                {
                    IsRunning = false;
                    UpdateStatusText();
                    StartCommand.NotifyCanExecuteChanged();
                    StopCommand.NotifyCanExecuteChanged();
                    RestartCommand.NotifyCanExecuteChanged();
                    AddLog(_loc["vm.log.server_stopped"]);
                })
            );
        }
        catch (Exception ex)
        {
            AddLog(_loc.Format("vm.log.error_prefix", ex.Message));
            IsRunning = false;
            UpdateStatusText();
            StartCommand.NotifyCanExecuteChanged();
            StopCommand.NotifyCanExecuteChanged();
            RestartCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanStart() => !IsRunning;

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop()
    {
        _llamaProcessService.Stop();
        AddLog(_loc["vm.log.stop_requested"]);
    }

    private bool CanStop() => IsRunning;

    [RelayCommand(CanExecute = nameof(CanStop))]
    private async Task Restart()
    {
        Stop();
        await Task.Delay(1000);
        await Start();
    }

    [RelayCommand]
    private void CopyLogs()
    {
        var text = string.Join(Environment.NewLine, Logs);
        WpfClipboard.SetText(text);
        AddLog(_loc["vm.log.logs_copied"]);
    }

    [RelayCommand]
    private void ClearLogs()
    {
        Logs.Clear();
    }

    private void AddLog(string message)
    {
        var timestampedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
        Logs.Add(timestampedMessage);

        while (Logs.Count > 1000)
            Logs.RemoveAt(0);
    }

    partial void OnSelectedVersionChanged(string? value)
    {
        SaveConfiguration();
        UpdateSelectedBenchmarkResult();
        UpdateStatusBarVersion();
    }

    partial void OnSelectedModelChanged(ModelInfo? value)
    {
        SaveConfiguration();
        UpdateSelectedBenchmarkResult();
        UpdateStatusBarModel();
    }

    partial void OnIsRunningChanged(bool value)
    {
        UpdateStatusText();
    }

    private void UpdateSelectedBenchmarkResult()
    {
        if (SelectedVersion == null || SelectedModel == null)
        {
            SelectedBenchmarkResult = null;
            HasSelectedBenchmark = false;
            SelectedBenchmarkDisplay = string.Empty;
            return;
        }

        var versionName = System.IO.Path.GetFileName(SelectedVersion);
        var results = _benchmarkService.LoadExistingResults();
        var fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(SelectedModel.FileName);

        var result = results.FirstOrDefault(r =>
            r.LlamaVersion.Equals(versionName, StringComparison.OrdinalIgnoreCase) &&
            !r.HasError &&
            (
                (r.ModelName.Equals(SelectedModel.Name, StringComparison.OrdinalIgnoreCase) &&
                 r.ModelQuantization.Equals(SelectedModel.Quantization, StringComparison.OrdinalIgnoreCase))
                ||
                r.ModelName.Equals(fileNameWithoutExt, StringComparison.OrdinalIgnoreCase)
            ));

        HasSelectedBenchmark = true;

        if (result != null)
        {
            SelectedBenchmarkResult = result;
            SelectedBenchmarkDisplay = $"PP: {result.PromptProcessingRaw} t/s | TG: {result.GenerationRaw} t/s";
            SelectedBenchmarkColor = "#4EC9B0";
        }
        else
        {
            SelectedBenchmarkResult = null;
            SelectedBenchmarkDisplay = _loc["benchmark.no_benchmark"];
            SelectedBenchmarkColor = "#808080";
        }
    }

    private bool CanBenchmark() => !IsRunning && !IsBenchmarking;

    [RelayCommand(CanExecute = nameof(CanBenchmark))]
    private async Task BenchmarkAll()
    {
        await RunBenchmarks(false);
    }

    [RelayCommand(CanExecute = nameof(CanBenchmark))]
    private async Task BenchmarkMissing()
    {
        await RunBenchmarks(true);
    }

    [RelayCommand(CanExecute = nameof(CanBenchmark))]
    private async Task BenchmarkSelectedModel()
    {
        if (SelectedModel == null)
        {
            WpfMessageBox.Show(_loc["vm.select_model_msg"], _loc["vm.error"], WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(LlamaCppDirectory))
        {
            WpfMessageBox.Show(_loc["vm.configure_dirs_msg"], _loc["vm.error"], WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
            return;
        }

        UpdateBenchmarkConfig();

        var versions = _modelDiscoveryService.GetLlamaVersions(LlamaCppDirectory);

        if (versions.Count == 0)
        {
            WpfMessageBox.Show(_loc["vm.no_version_model_found"], _loc["vm.error"], WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
            return;
        }

        var existingResults = _benchmarkService.LoadExistingResults();
        var benchmarksToRun = new List<(string version, ModelInfo model)>();

        foreach (var version in versions)
        {
            var versionName = System.IO.Path.GetFileName(version);
            if (!_benchmarkService.BenchmarkExists(existingResults, versionName, SelectedModel.Name, SelectedModel.Quantization))
                benchmarksToRun.Add((version, SelectedModel));
        }

        if (benchmarksToRun.Count == 0)
        {
            AddLog(_loc["vm.benchmark.all_exist"]);
            return;
        }

        IsBenchmarking = true;
        BenchmarkTotal = benchmarksToRun.Count;
        BenchmarkProgress = 0;
        BenchmarkAllCommand.NotifyCanExecuteChanged();
        BenchmarkMissingCommand.NotifyCanExecuteChanged();
        BenchmarkSelectedModelCommand.NotifyCanExecuteChanged();

        AddLog(_loc.Format("vm.benchmark.starting", BenchmarkTotal));

        var results = new List<BenchmarkResult>(existingResults);

        foreach (var (version, model) in benchmarksToRun)
        {
            var versionName = System.IO.Path.GetFileName(version);
            BenchmarkProgress++;
            BenchmarkStatus = _loc.Format("vm.benchmark.status", BenchmarkProgress, BenchmarkTotal, versionName, model.DisplayName);

            AddLog("");
            AddLog(_loc.Format("vm.benchmark.separator", BenchmarkProgress, BenchmarkTotal));
            AddLog(_loc.Format("vm.benchmark.version", versionName));
            AddLog(_loc.Format("vm.benchmark.model", model.DisplayName));

            var result = await _benchmarkService.RunBenchmarkAsync(
                version,
                model.FullPath,
                model.Name,
                model.Quantization,
                line => WpfApplication.Current.Dispatcher.Invoke(() => AddLog(line)));

            if (result != null)
            {
                results.RemoveAll(r =>
                    r.LlamaVersion.Equals(versionName, StringComparison.OrdinalIgnoreCase) &&
                    r.ModelName.Equals(model.Name, StringComparison.OrdinalIgnoreCase) &&
                    r.ModelQuantization.Equals(model.Quantization, StringComparison.OrdinalIgnoreCase));

                results.Add(result);

                if (result.HasError)
                {
                    AddLog(_loc.Format("vm.benchmark.error", result.ErrorMessage));
                }
                else
                {
                    AddLog($"[BENCHMARK] ✓ PP: {result.PromptProcessingRaw} t/s | TG: {result.GenerationRaw} t/s");
                }
            }
        }

        var sortedResults = results
            .OrderByDescending(r => r.HasError ? 0 : r.PromptProcessingTs)
            .ToList();

        _benchmarkService.SaveResults(sortedResults);

        AddLog("");
        AddLog(_loc["vm.benchmark.results"]);
        var markdown = _benchmarkService.GenerateMarkdownTable(sortedResults);
        foreach (var line in markdown.Split('\n'))
        {
            AddLog(line);
        }

        AddLog("");
        AddLog(_loc.Format("vm.benchmark.done", BenchmarkProgress, BenchmarkTotal));
        AddLog(_loc["vm.benchmark.saved"]);

        IsBenchmarking = false;
        BenchmarkStatus = string.Empty;
        BenchmarkAllCommand.NotifyCanExecuteChanged();
        BenchmarkMissingCommand.NotifyCanExecuteChanged();
        BenchmarkSelectedModelCommand.NotifyCanExecuteChanged();
    }

    private async Task RunBenchmarks(bool onlyMissing)
    {
        if (string.IsNullOrEmpty(LlamaCppDirectory) || string.IsNullOrEmpty(ModelsDirectory))
        {
            WpfMessageBox.Show(_loc["vm.configure_dirs_msg"], _loc["vm.error"], WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
            return;
        }

        UpdateBenchmarkConfig();

        var versions = _modelDiscoveryService.GetLlamaVersions(LlamaCppDirectory);
        var models = _modelDiscoveryService.GetModels(ModelsDirectory);

        if (versions.Count == 0 || models.Count == 0)
        {
            WpfMessageBox.Show(_loc["vm.no_version_model_found"], _loc["vm.error"], WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
            return;
        }

        var existingResults = _benchmarkService.LoadExistingResults();
        var benchmarksToRun = new List<(string version, ModelInfo model)>();

        foreach (var version in versions)
        {
            foreach (var model in models)
            {
                var versionName = System.IO.Path.GetFileName(version);
                if (onlyMissing && _benchmarkService.BenchmarkExists(existingResults, versionName, model.Name, model.Quantization))
                    continue;
                benchmarksToRun.Add((version, model));
            }
        }

        if (benchmarksToRun.Count == 0)
        {
            AddLog(_loc["vm.benchmark.all_exist"]);
            return;
        }

        IsBenchmarking = true;
        BenchmarkTotal = benchmarksToRun.Count;
        BenchmarkProgress = 0;
        BenchmarkAllCommand.NotifyCanExecuteChanged();
        BenchmarkMissingCommand.NotifyCanExecuteChanged();
        BenchmarkSelectedModelCommand.NotifyCanExecuteChanged();

        AddLog(_loc.Format("vm.benchmark.starting", BenchmarkTotal));

        var results = new List<BenchmarkResult>(existingResults);

        foreach (var (version, model) in benchmarksToRun)
        {
            var versionName = System.IO.Path.GetFileName(version);
            BenchmarkProgress++;
            BenchmarkStatus = _loc.Format("vm.benchmark.status", BenchmarkProgress, BenchmarkTotal, versionName, model.DisplayName);

            AddLog("");
            AddLog(_loc.Format("vm.benchmark.separator", BenchmarkProgress, BenchmarkTotal));
            AddLog(_loc.Format("vm.benchmark.version", versionName));
            AddLog(_loc.Format("vm.benchmark.model", model.DisplayName));

            var result = await _benchmarkService.RunBenchmarkAsync(
                version,
                model.FullPath,
                model.Name,
                model.Quantization,
                line => WpfApplication.Current.Dispatcher.Invoke(() => AddLog(line)));

            if (result != null)
            {
                results.RemoveAll(r =>
                    r.LlamaVersion.Equals(versionName, StringComparison.OrdinalIgnoreCase) &&
                    r.ModelName.Equals(model.Name, StringComparison.OrdinalIgnoreCase) &&
                    r.ModelQuantization.Equals(model.Quantization, StringComparison.OrdinalIgnoreCase));

                results.Add(result);

                if (result.HasError)
                {
                    AddLog(_loc.Format("vm.benchmark.error", result.ErrorMessage));
                }
                else
                {
                    AddLog($"[BENCHMARK] ✓ PP: {result.PromptProcessingRaw} t/s | TG: {result.GenerationRaw} t/s");
                }
            }
        }

        var sortedResults = results
            .OrderByDescending(r => r.HasError ? 0 : r.PromptProcessingTs)
            .ToList();

        _benchmarkService.SaveResults(sortedResults);

        AddLog("");
        AddLog(_loc["vm.benchmark.results"]);
        var markdown = _benchmarkService.GenerateMarkdownTable(sortedResults);
        foreach (var line in markdown.Split('\n'))
        {
            AddLog(line);
        }

        AddLog("");
        AddLog(_loc.Format("vm.benchmark.done", BenchmarkProgress, BenchmarkTotal));
        AddLog(_loc["vm.benchmark.saved"]);

        IsBenchmarking = false;
        BenchmarkStatus = string.Empty;
        BenchmarkAllCommand.NotifyCanExecuteChanged();
        BenchmarkMissingCommand.NotifyCanExecuteChanged();
        BenchmarkSelectedModelCommand.NotifyCanExecuteChanged();
    }

    private void UpdateBenchmarkConfig()
    {
        if (int.TryParse(BenchmarkNgl, out var ngl))
            BenchmarkConfig.Ngl = ngl;
        if (!string.IsNullOrWhiteSpace(BenchmarkCacheTypeK))
            BenchmarkConfig.CacheTypeK = BenchmarkCacheTypeK;
        if (!string.IsNullOrWhiteSpace(BenchmarkCacheTypeV))
            BenchmarkConfig.CacheTypeV = BenchmarkCacheTypeV;
        if (int.TryParse(BenchmarkPromptTokens, out var pt))
            BenchmarkConfig.PromptTokens = pt;
        if (int.TryParse(BenchmarkGenerationTokens, out var gt))
            BenchmarkConfig.GenerationTokens = gt;
        if (int.TryParse(BenchmarkRepetitions, out var rep))
            BenchmarkConfig.Repetitions = rep;
    }
}

public class ImportCommandDialog : WpfWindow
{
    public string CommandText { get; private set; } = string.Empty;

    public ImportCommandDialog()
    {
        var loc = LocalizationService.Instance;

        Title = loc["vm.import_command_title"];
        Width = 600;
        Height = 400;
        WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

        var grid = new System.Windows.Controls.Grid();
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

        var textBox = new System.Windows.Controls.TextBox
        {
            AcceptsReturn = true,
            AcceptsTab = true,
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            Margin = new System.Windows.Thickness(10)
        };

        var buttonPanel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new System.Windows.Thickness(10)
        };

        var okButton = new System.Windows.Controls.Button
        {
            Content = "OK",
            Width = 80,
            Height = 30,
            Margin = new System.Windows.Thickness(0, 0, 10, 0)
        };
        okButton.Click += (_, _) =>
        {
            CommandText = textBox.Text;
            DialogResult = true;
        };

        var cancelButton = new System.Windows.Controls.Button
        {
            Content = loc["vm.cancel"],
            Width = 80,
            Height = 30
        };
        cancelButton.Click += (_, _) => DialogResult = false;

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);

        System.Windows.Controls.Grid.SetRow(textBox, 0);
        System.Windows.Controls.Grid.SetRow(buttonPanel, 1);

        grid.Children.Add(textBox);
        grid.Children.Add(buttonPanel);

        Content = grid;
    }
}
