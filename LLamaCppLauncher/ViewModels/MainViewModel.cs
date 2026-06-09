using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LLamaCppLauncher.Models;
using LLamaCppLauncher.Services;
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
        RefreshProfiles();
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

    private void SaveConfiguration()
    {
        var config = new AppConfig
        {
            LlamaCppDirectory = LlamaCppDirectory,
            ModelsDirectory = ModelsDirectory,
            LastSelectedVersion = SelectedVersion ?? string.Empty,
            LastSelectedModel = SelectedModel?.FullPath ?? string.Empty
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
            Description = "Sélectionner le répertoire llama.cpp"
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
            Description = "Sélectionner le répertoire des modèles"
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
            AddLog("[INFO] Versions actualisées");
        }
    }

    [RelayCommand]
    private void RefreshModelsCommand()
    {
        if (!string.IsNullOrEmpty(ModelsDirectory))
        {
            RefreshModels();
            AddLog("[INFO] Modèles actualisés");
        }
    }

    [RelayCommand]
    private void SaveProfile()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Fichiers JSON|*.json",
            DefaultExt = ".json",
            Title = "Sauvegarder le profil"
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
            AddLog($"[INFO] Profil '{profile.Name}' sauvegardé");
        }
    }

    [RelayCommand]
    private void LoadProfile()
    {
        if (SelectedProfile == null)
        {
            WpfMessageBox.Show("Veuillez sélectionner un profil", "Erreur", WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
            return;
        }

        var profile = _profileService.LoadProfile(SelectedProfile);
        if (profile == null)
        {
            WpfMessageBox.Show("Profil introuvable", "Erreur", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
            return;
        }

        foreach (var param in Parameters)
        {
            if (profile.Parameters.TryGetValue(param.Name, out var value))
                param.Value = value;
            else
                param.Value = string.Empty;
        }

        AddLog($"[INFO] Profil '{SelectedProfile}' chargé");
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

            AddLog("[INFO] Commande importée");
        }
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task Start()
    {
        if (SelectedVersion == null || SelectedModel == null)
        {
            WpfMessageBox.Show("Veuillez sélectionner une version et un modèle", "Erreur", WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
            return;
        }

        IsRunning = true;
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
                line => WpfApplication.Current.Dispatcher.Invoke(() => AddLog($"[ERREUR] {line}")),
                () => WpfApplication.Current.Dispatcher.Invoke(() =>
                {
                    IsRunning = false;
                    StartCommand.NotifyCanExecuteChanged();
                    StopCommand.NotifyCanExecuteChanged();
                    RestartCommand.NotifyCanExecuteChanged();
                    AddLog("[INFO] Serveur arrêté");
                })
            );
        }
        catch (Exception ex)
        {
            AddLog($"[ERREUR] {ex.Message}");
            IsRunning = false;
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
        AddLog("[INFO] Arrêt du serveur demandé");
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
        AddLog("[INFO] Logs copiés dans le presse-papiers");
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
    }

    partial void OnSelectedModelChanged(ModelInfo? value)
    {
        SaveConfiguration();
        UpdateSelectedBenchmarkResult();
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
            SelectedBenchmarkDisplay = "Aucun benchmark disponible";
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

    private async Task RunBenchmarks(bool onlyMissing)
    {
        if (string.IsNullOrEmpty(LlamaCppDirectory) || string.IsNullOrEmpty(ModelsDirectory))
        {
            WpfMessageBox.Show("Veuillez configurer les répertoires llama.cpp et modèles", "Erreur", WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
            return;
        }

        var versions = _modelDiscoveryService.GetLlamaVersions(LlamaCppDirectory);
        var models = _modelDiscoveryService.GetModels(ModelsDirectory);

        if (versions.Count == 0 || models.Count == 0)
        {
            WpfMessageBox.Show("Aucune version llama.cpp ou modèle trouvé", "Erreur", WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
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
            AddLog("[INFO] Tous les benchmarks existent déjà");
            return;
        }

        IsBenchmarking = true;
        BenchmarkTotal = benchmarksToRun.Count;
        BenchmarkProgress = 0;
        BenchmarkAllCommand.NotifyCanExecuteChanged();
        BenchmarkMissingCommand.NotifyCanExecuteChanged();

        AddLog($"[BENCHMARK] Démarrage de {BenchmarkTotal} benchmark(s)");

        var results = new List<BenchmarkResult>(existingResults);

        foreach (var (version, model) in benchmarksToRun)
        {
            var versionName = System.IO.Path.GetFileName(version);
            BenchmarkProgress++;
            BenchmarkStatus = $"Benchmark {BenchmarkProgress}/{BenchmarkTotal} : {versionName} - {model.DisplayName}";

            AddLog("");
            AddLog($"[BENCHMARK] ========== {BenchmarkProgress}/{BenchmarkTotal} ==========");
            AddLog($"[BENCHMARK] Version : {versionName}");
            AddLog($"[BENCHMARK] Modèle : {model.DisplayName}");

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
                    AddLog($"[BENCHMARK] ✗ ERREUR : {result.ErrorMessage}");
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
        AddLog("[BENCHMARK] ========== RÉSULTATS ==========");
        var markdown = _benchmarkService.GenerateMarkdownTable(sortedResults);
        foreach (var line in markdown.Split('\n'))
        {
            AddLog(line);
        }

        AddLog("");
        AddLog($"[BENCHMARK] Terminé - {BenchmarkProgress}/{BenchmarkTotal} benchmarks effectués");
        AddLog($"[BENCHMARK] Résultats sauvegardés dans benchmark.md");

        IsBenchmarking = false;
        BenchmarkStatus = string.Empty;
        BenchmarkAllCommand.NotifyCanExecuteChanged();
        BenchmarkMissingCommand.NotifyCanExecuteChanged();
    }
}

public class ImportCommandDialog : WpfWindow
{
    public string CommandText { get; private set; } = string.Empty;

    public ImportCommandDialog()
    {
        Title = "Importer une commande";
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
            Content = "Annuler",
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
