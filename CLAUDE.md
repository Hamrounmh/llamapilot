# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

LLamaCpp Launcher is a WPF desktop application (.NET 8.0, Windows-only) for launching and managing llama.cpp server instances with a GUI. It provides model/version discovery, configurable launch parameters, profile management, command import/export, and integrated benchmarking via `llama-bench`.

The UI and all user-facing strings are bilingual (English/French), handled by `LocalizationService`.

## Build & Run

```bash
dotnet build                                    # Build the solution
dotnet run --project LLamaCppLauncher           # Run in development
dotnet publish -c Release -r win-x64 --self-contained  # Publish standalone
```

There are no tests, linter configuration, or CI/CD pipelines in this repository.

## Architecture

**Pattern:** MVVM using CommunityToolkit.Mvvm (source generators for `[ObservableProperty]`, `[RelayCommand]`).

**Single project:** `LLamaCppLauncher/` contains everything — no class library separation.

### Key layers

- **ViewModels/MainViewModel.cs** — Central orchestrator. Owns all UI state, wires commands to services, manages process lifecycle and benchmarking loop. Contains `ImportCommandDialog` (inline WPF window class).
- **Services/** — Stateless or near-stateless service classes, instantiated directly in the ViewModel constructor (no DI container):
  - `ConfigService` — Reads/writes `config.json` (persisted paths, selections, language)
  - `ProfileService` — CRUD for `profiles/*.json` launch parameter presets
  - `ModelDiscoveryService` — Scans directories for llama.cpp versions (by presence of `llama-server.exe`) and GGUF model files (excluding `mmproj` files)
  - `GgufMetadataService` — Binary parser for GGUF file headers (architecture, parameter count, context length)
  - `CommandParserService` — Tokenizes and parses llama-server CLI commands into parameter dictionaries; also builds command strings
  - `LlamaProcessService` — Manages a single `llama-server.exe` child process with stdout/stderr streaming
  - `BenchmarkService` — Runs `llama-bench.exe`, parses its markdown table output, manages `benchmark.md` persistence
  - `LocalizationService` — Singleton with hardcoded translation dictionary (en/fr). Uses `INotifyPropertyChanged` for live UI updates. Accessed via `LocalizationService.Instance` or the `{loc:Loc}` XAML markup extension.
- **Models/** — Plain data classes: `AppConfig`, `LaunchProfile`, `LlamaParameter` (uses `[ObservableProperty]`), `ModelInfo`, `BenchmarkResult`, `BenchmarkConfig` (static config holder)
- **Windows/ManageModelsWindow.cs** — Model management dialog (entirely code-behind, no XAML). Shows model metadata + benchmark results in a DataGrid, supports delete and HuggingFace download via `llama-server -hf`.
- **Helpers/LocalizationExtension.cs** — `{loc:Loc key}` markup extension for XAML data-binding to localized strings
- **Converters/** — `BoolToVisibilityConverter`, `StringToBrushConverter`

### Important conventions

- **No XAML for secondary windows.** `ManageModelsWindow` and `ImportCommandDialog` are built entirely in C# code-behind.
- **Services are not injected.** They are `new()`-ed in `MainViewModel`'s constructor.
- **Process management is single-instance.** Only one `llama-server` process at a time; starting while running throws.
- **Benchmark results persist to `benchmark.md`** as a markdown table. The service parses this file back on load, so the format matters.
- **Flag parameters** (like `--jinja`) use value `"on"` to indicate enabled.
- **Localization keys** are dot-separated (e.g., `vm.log.server_stopped`). All translations live in `LocalizationService._translations` — there are no resource files.

### Generated files (not committed)

- `config.json` — App configuration (paths, last selections, language)
- `profiles/*.json` — Saved launch parameter profiles
- `benchmark.md` — Benchmark results table

## XAML Theme

The app uses a VS Code-inspired dark theme defined inline in `MainWindow.xaml` resources. Key colors: background `#1E1E1E`, card `#252526`, accent `#007ACC`, text `#E0E0E0`. Custom button styles exist for Start (green), Stop (red), Restart (orange), and window controls.
