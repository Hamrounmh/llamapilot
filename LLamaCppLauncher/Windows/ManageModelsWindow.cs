using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using LLamaCppLauncher.Models;
using LLamaCppLauncher.Services;
using WpfButton = System.Windows.Controls.Button;
using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfControl = System.Windows.Controls.Control;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace LLamaCppLauncher.Windows;

public class ManageModelsWindow : Window
{
    private readonly ModelDiscoveryService _modelDiscoveryService;
    private readonly BenchmarkService _benchmarkService;
    private readonly string _llamaVersionDir;
    private readonly string _modelsDir;
    private readonly LocalizationService _loc = LocalizationService.Instance;

    private readonly ObservableCollection<ManageModelItem> _items = new();
    private readonly DataGrid _dataGrid;
    private readonly WpfTextBox _hfRefBox;
    private readonly WpfTextBox _progressBox;
    private readonly StackPanel _progressPanel;
    private readonly WpfButton _downloadButton;

    public ManageModelsWindow(
        ModelDiscoveryService modelDiscoveryService,
        BenchmarkService benchmarkService,
        string llamaVersionDir,
        string modelsDir)
    {
        _modelDiscoveryService = modelDiscoveryService;
        _benchmarkService = benchmarkService;
        _llamaVersionDir = llamaVersionDir;
        _modelsDir = modelsDir;

        Title = _loc["manage.title"];
        Width = 1100;
        Height = 650;
        MinWidth = 800;
        MinHeight = 450;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush("#1E1E1E");

        var mainGrid = new Grid();
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = CreateHeader();
        _dataGrid = CreateDataGrid();
        var actionsPanel = CreateActionsPanel();
        var downloadPanel = CreateDownloadPanel(out _hfRefBox, out _downloadButton);
        _progressPanel = CreateProgressPanel(out _progressBox);

        Grid.SetRow(header, 0);
        Grid.SetRow(_dataGrid, 1);
        Grid.SetRow(actionsPanel, 2);
        Grid.SetRow(downloadPanel, 3);
        Grid.SetRow(_progressPanel, 4);

        mainGrid.Children.Add(header);
        mainGrid.Children.Add(_dataGrid);
        mainGrid.Children.Add(actionsPanel);
        mainGrid.Children.Add(downloadPanel);
        mainGrid.Children.Add(_progressPanel);

        Content = mainGrid;

        _dataGrid.ItemsSource = _items;
        LoadModels();
    }

    private static SolidColorBrush Brush(string hex) =>
        new((WpfColor)WpfColorConverter.ConvertFromString(hex));

    private Border CreateHeader()
    {
        var panel = new DockPanel { Margin = new Thickness(16, 12, 16, 8) };

        var title = new TextBlock
        {
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush("#007ACC"),
            VerticalAlignment = VerticalAlignment.Center
        };
        title.Inlines.Add(new System.Windows.Documents.Run("⚙"));
        title.Inlines.Add(new System.Windows.Documents.Run(" " + _loc["manage.header"].Replace("⚙ ", "")));

        var closeBtn = CreateButton(_loc["manage.close"], "#3F3F46", "#D4D4D4");
        closeBtn.Margin = new Thickness(0);
        closeBtn.HorizontalAlignment = WpfHorizontalAlignment.Right;
        closeBtn.Click += (_, _) => Close();
        DockPanel.SetDock(closeBtn, Dock.Right);

        panel.Children.Add(closeBtn);
        panel.Children.Add(title);

        return new Border
        {
            Child = panel,
            BorderBrush = Brush("#3F3F46"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(0, 0, 0, 8)
        };
    }

    private DataGrid CreateDataGrid()
    {
        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            SelectionMode = DataGridSelectionMode.Single,
            Background = Brush("#1E1E1E"),
            RowBackground = Brush("#252526"),
            AlternatingRowBackground = Brush("#2D2D30"),
            Foreground = Brush("#E0E0E0"),
            BorderBrush = Brush("#3F3F46"),
            BorderThickness = new Thickness(1),
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
            HorizontalGridLinesBrush = Brush("#3F3F46"),
            HeadersVisibility = DataGridHeadersVisibility.Column,
            Margin = new Thickness(16, 8, 16, 8),
            FontSize = 12,
            CanUserReorderColumns = true,
            CanUserResizeColumns = true,
            CanUserSortColumns = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var headerStyle = new Style(typeof(DataGridColumnHeader));
        headerStyle.Setters.Add(new Setter(WpfControl.BackgroundProperty, Brush("#2D2D30")));
        headerStyle.Setters.Add(new Setter(WpfControl.ForegroundProperty, Brush("#B0B0B0")));
        headerStyle.Setters.Add(new Setter(WpfControl.PaddingProperty, new Thickness(8, 6, 8, 6)));
        headerStyle.Setters.Add(new Setter(WpfControl.BorderBrushProperty, Brush("#3F3F46")));
        headerStyle.Setters.Add(new Setter(WpfControl.BorderThicknessProperty, new Thickness(0, 0, 1, 1)));
        headerStyle.Setters.Add(new Setter(WpfControl.FontWeightProperty, FontWeights.SemiBold));
        grid.ColumnHeaderStyle = headerStyle;

        var cellStyle = new Style(typeof(DataGridCell));
        cellStyle.Setters.Add(new Setter(WpfControl.PaddingProperty, new Thickness(8, 4, 8, 4)));
        cellStyle.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(0)));

        var selectedTrigger = new Trigger { Property = DataGridCell.IsSelectedProperty, Value = true };
        selectedTrigger.Setters.Add(new Setter(WpfControl.BackgroundProperty, Brush("#094771")));
        selectedTrigger.Setters.Add(new Setter(WpfControl.ForegroundProperty, System.Windows.Media.Brushes.White));
        cellStyle.Triggers.Add(selectedTrigger);
        grid.CellStyle = cellStyle;

        grid.Columns.Add(new DataGridTextColumn
        {
            Header = _loc["manage.col_name"],
            Binding = new System.Windows.Data.Binding("Name"),
            Width = new DataGridLength(1, DataGridLengthUnitType.Star)
        });
        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Quantization",
            Binding = new System.Windows.Data.Binding("Quantization"),
            Width = new DataGridLength(110)
        });
        grid.Columns.Add(new DataGridTextColumn
        {
            Header = _loc["manage.col_size"],
            Binding = new System.Windows.Data.Binding("FileSizeDisplay"),
            Width = new DataGridLength(80)
        });
        grid.Columns.Add(new DataGridTextColumn
        {
            Header = _loc["manage.col_params"],
            Binding = new System.Windows.Data.Binding("ParameterCountDisplay"),
            Width = new DataGridLength(90)
        });
        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Architecture",
            Binding = new System.Windows.Data.Binding("Architecture"),
            Width = new DataGridLength(100)
        });
        grid.Columns.Add(new DataGridTextColumn
        {
            Header = _loc["manage.col_context"],
            Binding = new System.Windows.Data.Binding("ContextLengthDisplay"),
            Width = new DataGridLength(80)
        });
        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "PP (t/s)",
            Binding = new System.Windows.Data.Binding("BenchmarkPPDisplay"),
            Width = new DataGridLength(75)
        });
        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "TG (t/s)",
            Binding = new System.Windows.Data.Binding("BenchmarkTGDisplay"),
            Width = new DataGridLength(75)
        });

        return grid;
    }

    private StackPanel CreateActionsPanel()
    {
        var panel = new StackPanel
        {
            Orientation = WpfOrientation.Horizontal,
            Margin = new Thickness(16, 0, 16, 8)
        };

        var refreshBtn = CreateButton(_loc["manage.refresh"], "#3F3F46", "#D4D4D4");
        refreshBtn.Margin = new Thickness(0, 0, 8, 0);
        refreshBtn.Click += (_, _) => LoadModels();

        var deleteBtn = CreateButton(_loc["manage.delete"], "#C62828", "#FFFFFF");
        deleteBtn.Click += DeleteModel;

        panel.Children.Add(refreshBtn);
        panel.Children.Add(deleteBtn);

        return panel;
    }

    private Border CreateDownloadPanel(out WpfTextBox hfRefBox, out WpfButton downloadButton)
    {
        var panel = new StackPanel { Margin = new Thickness(16, 0, 16, 8) };

        var label = new TextBlock
        {
            Text = _loc["manage.download_label"],
            Foreground = Brush("#4EC9B0"),
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6)
        };

        var inputGrid = new Grid();
        inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        hfRefBox = new WpfTextBox
        {
            Background = Brush("#2D2D30"),
            Foreground = Brush("#E0E0E0"),
            BorderBrush = Brush("#3F3F46"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 6, 8, 6),
            FontSize = 12,
            CaretBrush = Brush("#E0E0E0")
        };

        downloadButton = CreateButton(_loc["manage.download_btn"], "#2E7D32", "#FFFFFF");
        downloadButton.Margin = new Thickness(8, 0, 0, 0);
        downloadButton.Click += DownloadModel;

        Grid.SetColumn(hfRefBox, 0);
        Grid.SetColumn(downloadButton, 1);
        inputGrid.Children.Add(hfRefBox);
        inputGrid.Children.Add(downloadButton);

        panel.Children.Add(label);
        panel.Children.Add(inputGrid);

        return new Border
        {
            Child = panel,
            BorderBrush = Brush("#3F3F46"),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(0, 12, 0, 0)
        };
    }

    private StackPanel CreateProgressPanel(out WpfTextBox progressBox)
    {
        var panel = new StackPanel { Visibility = Visibility.Collapsed };

        progressBox = new WpfTextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new WpfFontFamily("Cascadia Code, Consolas, Courier New"),
            FontSize = 11,
            Height = 180,
            Background = Brush("#1A1A1A"),
            Foreground = Brush("#D4D4D4"),
            BorderBrush = Brush("#3F3F46"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8),
            Margin = new Thickness(16, 8, 16, 16)
        };

        panel.Children.Add(progressBox);
        return panel;
    }

    private static WpfButton CreateButton(string content, string background, string foreground)
    {
        var btn = new WpfButton
        {
            Content = content,
            Background = Brush(background),
            Foreground = Brush(foreground),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(14, 8, 14, 8),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Cursor = System.Windows.Input.Cursors.Hand
        };

        var template = new ControlTemplate(typeof(WpfButton));
        var border = new FrameworkElementFactory(typeof(Border), "border");
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(WpfControl.BackgroundProperty));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
        border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(WpfControl.PaddingProperty));

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, WpfHorizontalAlignment.Center);
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(presenter);
        template.VisualTree = border;

        var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty,
            Brush(LightenColor(background)), "border"));
        template.Triggers.Add(hoverTrigger);

        var pressedTrigger = new Trigger { Property = WpfButton.IsPressedProperty, Value = true };
        pressedTrigger.Setters.Add(new Setter(Border.BackgroundProperty,
            Brush(DarkenColor(background)), "border"));
        template.Triggers.Add(pressedTrigger);

        btn.Template = template;
        return btn;
    }

    private static string LightenColor(string hex)
    {
        var color = (WpfColor)WpfColorConverter.ConvertFromString(hex);
        color.R = (byte)Math.Min(255, color.R + 25);
        color.G = (byte)Math.Min(255, color.G + 25);
        color.B = (byte)Math.Min(255, color.B + 25);
        return color.ToString();
    }

    private static string DarkenColor(string hex)
    {
        var color = (WpfColor)WpfColorConverter.ConvertFromString(hex);
        color.R = (byte)Math.Max(0, color.R - 25);
        color.G = (byte)Math.Max(0, color.G - 25);
        color.B = (byte)Math.Max(0, color.B - 25);
        return color.ToString();
    }

    private void LoadModels()
    {
        _items.Clear();
        var models = _modelDiscoveryService.GetModels(_modelsDir);
        var benchmarkResults = _benchmarkService.LoadExistingResults();

        foreach (var model in models)
        {
            var item = new ManageModelItem
            {
                Name = model.Name,
                Quantization = model.Quantization,
                FullPath = model.FullPath,
                FileName = model.FileName,
                FileSize = model.FileSize,
                ParameterCount = model.ParameterCount,
                Architecture = model.Architecture,
                ContextLength = model.ContextLength
            };

            var result = benchmarkResults.FirstOrDefault(r =>
                !r.HasError &&
                r.ModelName.Equals(model.Name, StringComparison.OrdinalIgnoreCase) &&
                r.ModelQuantization.Equals(model.Quantization, StringComparison.OrdinalIgnoreCase));

            if (result != null)
            {
                item.BenchmarkPP = result.PromptProcessingTs;
                item.BenchmarkTG = result.GenerationTs;
            }

            _items.Add(item);
        }
    }

    private void DeleteModel(object sender, RoutedEventArgs e)
    {
        if (_dataGrid.SelectedItem is not ManageModelItem selected)
        {
            System.Windows.MessageBox.Show(_loc["manage.select_model_msg"], _loc["vm.error"],
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = System.Windows.MessageBox.Show(
            _loc.Format("manage.confirm_delete", selected.FileName),
            _loc["manage.confirm_delete_title"],
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            if (File.Exists(selected.FullPath))
                File.Delete(selected.FullPath);

            _items.Remove(selected);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(_loc.Format("manage.delete_error", ex.Message), _loc["vm.error"],
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void DownloadModel(object sender, RoutedEventArgs e)
    {
        var hfRef = _hfRefBox.Text.Trim();
        if (string.IsNullOrEmpty(hfRef))
        {
            System.Windows.MessageBox.Show(_loc["manage.enter_hf_ref"], _loc["vm.error"],
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(_llamaVersionDir) || !Directory.Exists(_llamaVersionDir))
        {
            System.Windows.MessageBox.Show(_loc["manage.invalid_dir"], _loc["vm.error"],
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var llamaServerExe = Path.Combine(_llamaVersionDir, "llama-server.exe");
        if (!File.Exists(llamaServerExe))
        {
            System.Windows.MessageBox.Show(_loc.Format("manage.server_not_found", _llamaVersionDir), _loc["vm.error"],
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _progressPanel.Visibility = Visibility.Visible;
        _progressBox.Clear();
        _downloadButton.IsEnabled = false;

        AppendProgress(_loc.Format("manage.downloading", hfRef));
        AppendProgress($"Cache: {_modelsDir}");
        AppendProgress($"Version: {Path.GetFileName(_llamaVersionDir)}");
        AppendProgress("");

        var startInfo = new ProcessStartInfo
        {
            FileName = llamaServerExe,
            Arguments = $"-hf {hfRef}",
            WorkingDirectory = _llamaVersionDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.EnvironmentVariables["HF_HUB_CACHE"] = _modelsDir;

        try
        {
            var process = new Process { StartInfo = startInfo };

            process.OutputDataReceived += (_, args) =>
            {
                if (args.Data != null)
                    Dispatcher.Invoke(() => AppendProgress(args.Data));
            };

            process.ErrorDataReceived += (_, args) =>
            {
                if (args.Data != null)
                    Dispatcher.Invoke(() => AppendProgress(_loc.Format("vm.log.error_prefix", args.Data)));
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await System.Threading.Tasks.Task.Run(() => process.WaitForExit());

            AppendProgress("");
            AppendProgress(_loc["manage.download_done"]);

            LoadModels();
        }
        catch (Exception ex)
        {
            AppendProgress(_loc.Format("vm.log.error_prefix", ex.Message));
        }
        finally
        {
            _downloadButton.IsEnabled = true;
        }
    }

    private void AppendProgress(string text)
    {
        _progressBox.AppendText(text + Environment.NewLine);
        _progressBox.ScrollToEnd();
    }
}

public class ManageModelItem
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

    public string FileSizeDisplay => FileSize > 0 ? FormatSize(FileSize) : "-";
    public string ParameterCountDisplay => ParameterCount > 0 ? FormatParams(ParameterCount) : "-";
    public string ContextLengthDisplay => ContextLength > 0 ? ContextLength.ToString("N0") : "-";
    public string BenchmarkPPDisplay => BenchmarkPP > 0 ? BenchmarkPP.ToString("F1") : "-";
    public string BenchmarkTGDisplay => BenchmarkTG > 0 ? BenchmarkTG.ToString("F1") : "-";

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1_073_741_824L) return $"{bytes / 1_073_741_824.0:F2} GB";
        if (bytes >= 1_048_576L) return $"{bytes / 1_048_576.0:F1} MB";
        if (bytes >= 1024L) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes} B";
    }

    private static string FormatParams(ulong count)
    {
        if (count >= 1_000_000_000UL) return $"{count / 1_000_000_000.0:F1}B";
        if (count >= 1_000_000UL) return $"{count / 1_000_000.0:F0}M";
        return count.ToString("N0");
    }
}
