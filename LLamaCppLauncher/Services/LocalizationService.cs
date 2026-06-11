using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LLamaCppLauncher.Services;

public class LocalizationService : INotifyPropertyChanged
{
    private static LocalizationService? _instance;
    public static LocalizationService Instance => _instance ??= new LocalizationService();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? LanguageChanged;

    private string _currentLanguage = "en";

    public string CurrentLanguage
    {
        get => _currentLanguage;
        private set
        {
            _currentLanguage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
        }
    }

    public string this[string key] => Get(key);

    public string Get(string key)
    {
        if (_translations.TryGetValue(key, out var langDict) &&
            langDict.TryGetValue(CurrentLanguage, out var value))
            return value;
        return $"[{key}]";
    }

    public string Format(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }

    public void SetLanguage(string language)
    {
        if (CurrentLanguage == language) return;
        CurrentLanguage = language;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        LanguageChanged?.Invoke();
    }

    public void ToggleLanguage()
    {
        SetLanguage(CurrentLanguage == "en" ? "fr" : "en");
    }

    public string LanguageLabel => CurrentLanguage.ToUpperInvariant();

    private static readonly Dictionary<string, Dictionary<string, string>> _translations = new()
    {
        // Title bar
        ["title.minimize"] = new() { ["en"] = "Minimize", ["fr"] = "Réduire" },
        ["title.maximize"] = new() { ["en"] = "Maximize", ["fr"] = "Agrandir" },
        ["title.close"] = new() { ["en"] = "Close", ["fr"] = "Fermer" },

        // Configuration section
        ["config.title"] = new() { ["en"] = " Configuration", ["fr"] = " Configuration" },
        ["config.llama_dir"] = new() { ["en"] = "llama.cpp directory", ["fr"] = "Répertoire llama.cpp" },
        ["config.models_dir"] = new() { ["en"] = "Models directory", ["fr"] = "Répertoire modèles" },
        ["config.version"] = new() { ["en"] = "llama.cpp version", ["fr"] = "Version llama.cpp" },
        ["config.model"] = new() { ["en"] = "Model", ["fr"] = "Modèle" },
        ["config.browse"] = new() { ["en"] = "Browse", ["fr"] = "Parcourir" },
        ["config.refresh"] = new() { ["en"] = "Refresh", ["fr"] = "Actualiser" },
        ["config.manage_models"] = new() { ["en"] = "Manage models", ["fr"] = "Gérer les modèles" },
        ["config.last_benchmark"] = new() { ["en"] = "📊 Last benchmark", ["fr"] = "📊 Dernier benchmark" },

        // Server section
        ["server.title"] = new() { ["en"] = " Server", ["fr"] = " Serveur" },
        ["server.open_browser"] = new() { ["en"] = "Open in browser", ["fr"] = "Ouvrir dans le navigateur" },

        // Benchmark section
        ["benchmark.title"] = new() { ["en"] = " Benchmark", ["fr"] = " Benchmark" },
        ["benchmark.no_benchmark"] = new() { ["en"] = "No benchmark available", ["fr"] = "Aucun benchmark disponible" },

        // Launch parameters section
        ["params.title"] = new() { ["en"] = " Launch parameters", ["fr"] = " Paramètres de lancement" },

        // Profiles section
        ["profiles.title"] = new() { ["en"] = " Profiles", ["fr"] = " Profils" },
        ["profiles.save"] = new() { ["en"] = "💾 Save", ["fr"] = "💾 Sauvegarder" },
        ["profiles.load"] = new() { ["en"] = "📂 Load", ["fr"] = "📂 Charger" },
        ["profiles.import"] = new() { ["en"] = "📥 Import", ["fr"] = "📥 Importer" },

        // Console section
        ["console.title"] = new() { ["en"] = " Console", ["fr"] = " Console" },
        ["console.copy"] = new() { ["en"] = "📋 Copy", ["fr"] = "📋 Copier" },
        ["console.clear"] = new() { ["en"] = "🗑 Clear", ["fr"] = "🗑 Effacer" },
        ["console.copy_tooltip"] = new() { ["en"] = "Copy logs", ["fr"] = "Copier les logs" },
        ["console.clear_tooltip"] = new() { ["en"] = "Clear logs", ["fr"] = "Effacer les logs" },

        // Action buttons
        ["action.start"] = new() { ["en"] = "▶  Start", ["fr"] = "▶  Démarrer" },
        ["action.stop"] = new() { ["en"] = "■  Stop", ["fr"] = "■  Stop" },
        ["action.restart"] = new() { ["en"] = "↻  Restart", ["fr"] = "↻  Redémarrer" },
        ["action.start_tooltip"] = new() { ["en"] = "Start llama.cpp server", ["fr"] = "Démarrer le serveur llama.cpp" },
        ["action.stop_tooltip"] = new() { ["en"] = "Stop server", ["fr"] = "Arrêter le serveur" },
        ["action.restart_tooltip"] = new() { ["en"] = "Restart server", ["fr"] = "Redémarrer le serveur" },

        // Status bar
        ["status.stopped"] = new() { ["en"] = "Stopped", ["fr"] = "Arrêté" },
        ["status.running"] = new() { ["en"] = "Running", ["fr"] = "En cours" },
        ["status.version_format"] = new() { ["en"] = "Version: {0}", ["fr"] = "Version : {0}" },
        ["status.version_none"] = new() { ["en"] = "Version: -", ["fr"] = "Version : -" },
        ["status.model_format"] = new() { ["en"] = "Model: {0}", ["fr"] = "Modèle : {0}" },
        ["status.model_none"] = new() { ["en"] = "Model: -", ["fr"] = "Modèle : -" },

        // ViewModel - dialogs
        ["vm.select_llama_dir"] = new() { ["en"] = "Select llama.cpp directory", ["fr"] = "Sélectionner le répertoire llama.cpp" },
        ["vm.select_models_dir"] = new() { ["en"] = "Select models directory", ["fr"] = "Sélectionner le répertoire des modèles" },
        ["vm.save_profile_title"] = new() { ["en"] = "Save profile", ["fr"] = "Sauvegarder le profil" },
        ["vm.json_filter"] = new() { ["en"] = "JSON Files|*.json", ["fr"] = "Fichiers JSON|*.json" },
        ["vm.error"] = new() { ["en"] = "Error", ["fr"] = "Erreur" },
        ["vm.select_profile_msg"] = new() { ["en"] = "Please select a profile", ["fr"] = "Veuillez sélectionner un profil" },
        ["vm.profile_not_found"] = new() { ["en"] = "Profile not found", ["fr"] = "Profil introuvable" },
        ["vm.select_version_model_msg"] = new() { ["en"] = "Please select a version and a model", ["fr"] = "Veuillez sélectionner une version et un modèle" },
        ["vm.configure_dirs_msg"] = new() { ["en"] = "Please configure llama.cpp and models directories", ["fr"] = "Veuillez configurer les répertoires llama.cpp et modèles" },
        ["vm.no_version_model_found"] = new() { ["en"] = "No llama.cpp version or model found", ["fr"] = "Aucune version llama.cpp ou modèle trouvé" },
        ["vm.import_command_title"] = new() { ["en"] = "Import a command", ["fr"] = "Importer une commande" },
        ["vm.cancel"] = new() { ["en"] = "Cancel", ["fr"] = "Annuler" },

        // ViewModel - log messages
        ["vm.log.versions_refreshed"] = new() { ["en"] = "[INFO] Versions refreshed", ["fr"] = "[INFO] Versions actualisées" },
        ["vm.log.models_refreshed"] = new() { ["en"] = "[INFO] Models refreshed", ["fr"] = "[INFO] Modèles actualisés" },
        ["vm.log.profile_saved"] = new() { ["en"] = "[INFO] Profile '{0}' saved", ["fr"] = "[INFO] Profil '{0}' sauvegardé" },
        ["vm.log.profile_loaded"] = new() { ["en"] = "[INFO] Profile '{0}' loaded", ["fr"] = "[INFO] Profil '{0}' chargé" },
        ["vm.log.command_imported"] = new() { ["en"] = "[INFO] Command imported", ["fr"] = "[INFO] Commande importée" },
        ["vm.log.server_stopped"] = new() { ["en"] = "[INFO] Server stopped", ["fr"] = "[INFO] Serveur arrêté" },
        ["vm.log.stop_requested"] = new() { ["en"] = "[INFO] Server stop requested", ["fr"] = "[INFO] Arrêt du serveur demandé" },
        ["vm.log.logs_copied"] = new() { ["en"] = "[INFO] Logs copied to clipboard", ["fr"] = "[INFO] Logs copiés dans le presse-papiers" },
        ["vm.log.error_prefix"] = new() { ["en"] = "[ERROR] {0}", ["fr"] = "[ERREUR] {0}" },

        // ViewModel - benchmark
        ["vm.benchmark.starting"] = new() { ["en"] = "[BENCHMARK] Starting {0} benchmark(s)", ["fr"] = "[BENCHMARK] Démarrage de {0} benchmark(s)" },
        ["vm.benchmark.status"] = new() { ["en"] = "Benchmark {0}/{1}: {2} - {3}", ["fr"] = "Benchmark {0}/{1} : {2} - {3}" },
        ["vm.benchmark.separator"] = new() { ["en"] = "[BENCHMARK] ========== {0}/{1} ==========", ["fr"] = "[BENCHMARK] ========== {0}/{1} ==========" },
        ["vm.benchmark.version"] = new() { ["en"] = "[BENCHMARK] Version: {0}", ["fr"] = "[BENCHMARK] Version : {0}" },
        ["vm.benchmark.model"] = new() { ["en"] = "[BENCHMARK] Model: {0}", ["fr"] = "[BENCHMARK] Modèle : {0}" },
        ["vm.benchmark.error"] = new() { ["en"] = "[BENCHMARK] ✗ ERROR: {0}", ["fr"] = "[BENCHMARK] ✗ ERREUR : {0}" },
        ["vm.benchmark.results"] = new() { ["en"] = "[BENCHMARK] ========== RESULTS ==========", ["fr"] = "[BENCHMARK] ========== RÉSULTATS ==========" },
        ["vm.benchmark.done"] = new() { ["en"] = "[BENCHMARK] Done - {0}/{1} benchmarks completed", ["fr"] = "[BENCHMARK] Terminé - {0}/{1} benchmarks effectués" },
        ["vm.benchmark.saved"] = new() { ["en"] = "[BENCHMARK] Results saved in benchmark.md", ["fr"] = "[BENCHMARK] Résultats sauvegardés dans benchmark.md" },
        ["vm.benchmark.all_exist"] = new() { ["en"] = "[INFO] All benchmarks already exist", ["fr"] = "[INFO] Tous les benchmarks existent déjà" },

        // Manage Models window
        ["manage.title"] = new() { ["en"] = "Manage models", ["fr"] = "Gérer les modèles" },
        ["manage.header"] = new() { ["en"] = "⚙ Model management", ["fr"] = "⚙ Gestion des modèles" },
        ["manage.close"] = new() { ["en"] = "✕ Close", ["fr"] = "✕ Fermer" },
        ["manage.col_name"] = new() { ["en"] = "Name", ["fr"] = "Nom" },
        ["manage.col_size"] = new() { ["en"] = "Size", ["fr"] = "Taille" },
        ["manage.col_version"] = new() { ["en"] = "llama.cpp", ["fr"] = "llama.cpp" },
        ["manage.col_context"] = new() { ["en"] = "Context", ["fr"] = "Contexte" },
        ["manage.refresh"] = new() { ["en"] = "🔄 Refresh", ["fr"] = "🔄 Actualiser" },
        ["manage.delete"] = new() { ["en"] = "🗑 Delete", ["fr"] = "🗑 Supprimer" },
        ["manage.download_label"] = new() { ["en"] = "📥 Download from HuggingFace", ["fr"] = "📥 Télécharger depuis HuggingFace" },
        ["manage.download_btn"] = new() { ["en"] = "📥 Download", ["fr"] = "📥 Télécharger" },
        ["manage.select_model_msg"] = new() { ["en"] = "Please select a model", ["fr"] = "Veuillez sélectionner un modèle" },
        ["manage.confirm_delete"] = new() { ["en"] = "Permanently delete model:\n{0}?", ["fr"] = "Supprimer définitivement le modèle :\n{0} ?" },
        ["manage.confirm_delete_title"] = new() { ["en"] = "Confirm deletion", ["fr"] = "Confirmer la suppression" },
        ["manage.delete_error"] = new() { ["en"] = "Error deleting: {0}", ["fr"] = "Erreur lors de la suppression : {0}" },
        ["manage.enter_hf_ref"] = new() { ["en"] = "Please enter a HuggingFace reference", ["fr"] = "Veuillez entrer une référence HuggingFace" },
        ["manage.invalid_dir"] = new() { ["en"] = "Invalid llama.cpp directory", ["fr"] = "Répertoire llama.cpp invalide" },
        ["manage.server_not_found"] = new() { ["en"] = "llama-server.exe not found in {0}", ["fr"] = "llama-server.exe introuvable dans {0}" },
        ["manage.downloading"] = new() { ["en"] = "Downloading {0}...", ["fr"] = "Téléchargement de {0}..." },
        ["manage.download_done"] = new() { ["en"] = "Download complete.", ["fr"] = "Téléchargement terminé." },
        ["manage.download_cmd_open"] = new() { ["en"] = "Opening download window...", ["fr"] = "Ouverture de la fenêtre de téléchargement..." },

        // LlamaProcessService
        ["svc.process_already_running"] = new() { ["en"] = "A process is already running.", ["fr"] = "Un processus est déjà en cours d'exécution." },
        ["svc.server_not_found"] = new() { ["en"] = "llama-server.exe not found in {0}", ["fr"] = "llama-server.exe introuvable dans {0}" },
        ["svc.log.server_started"] = new() { ["en"] = "[INFO] Server started - PID: {0}", ["fr"] = "[INFO] Serveur démarré - PID : {0}" },
        ["svc.log.directory"] = new() { ["en"] = "[INFO] Directory: {0}", ["fr"] = "[INFO] Répertoire : {0}" },
        ["svc.log.model"] = new() { ["en"] = "[INFO] Model: {0}", ["fr"] = "[INFO] Modèle : {0}" },
        ["svc.log.command"] = new() { ["en"] = "[INFO] Command: llama-server {0}", ["fr"] = "[INFO] Commande : llama-server {0}" },

        // BenchmarkService
        ["svc.bench.not_found_log"] = new() { ["en"] = "[ERROR] llama-bench.exe not found in {0}", ["fr"] = "[ERREUR] llama-bench.exe introuvable dans {0}" },
        ["svc.bench.not_found"] = new() { ["en"] = "llama-bench.exe not found", ["fr"] = "llama-bench.exe introuvable" },
        ["svc.bench.command_log"] = new() { ["en"] = "[BENCHMARK] Command: llama-bench {0}", ["fr"] = "[BENCHMARK] Commande : llama-bench {0}" },
        ["svc.bench.parse_error"] = new() { ["en"] = "Unable to parse llama-bench output", ["fr"] = "Impossible de parser la sortie de llama-bench" },
        ["svc.bench.md_date"] = new() { ["en"] = "**Last updated:** {0}", ["fr"] = "**Date de dernière mise à jour :** {0}" },
        ["svc.bench.md_fixed_params"] = new() { ["en"] = "**Fixed parameters:**", ["fr"] = "**Paramètres fixes :**" },
        ["svc.bench.md_results"] = new() { ["en"] = "## Results", ["fr"] = "## Résultats" },
        ["svc.bench.md_error"] = new() { ["en"] = "ERROR", ["fr"] = "ERREUR" },
        ["svc.bench.md_error_details"] = new() { ["en"] = "## Error details", ["fr"] = "## Détails des erreurs" },

        // ConfigService & ProfileService
        ["svc.config_save_error"] = new() { ["en"] = "Error saving config: {0}", ["fr"] = "Erreur lors de la sauvegarde de la config : {0}" },
        ["svc.profile_save_error"] = new() { ["en"] = "Error saving profile: {0}", ["fr"] = "Erreur lors de la sauvegarde du profil : {0}" },
        ["svc.profile_delete_error"] = new() { ["en"] = "Error deleting profile: {0}", ["fr"] = "Erreur lors de la suppression du profil : {0}" },

        // Language
        ["lang.switch"] = new() { ["en"] = "FR", ["fr"] = "EN" },
    };
}
