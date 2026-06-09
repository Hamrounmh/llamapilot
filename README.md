# LLamaCpp Launcher

Application WPF pour lancer et gérer des instances de llama-server avec une interface graphique intuitive.

## Fonctionnalités

### Configuration (Container gauche)
- **Répertoire llama.cpp** : Sélectionnez le dossier contenant vos versions de llama.cpp
- **Répertoire modèles** : Sélectionnez le dossier contenant vos modèles GGUF
- **Version llama.cpp** : Liste déroulante des versions disponibles
- **Modèle** : Liste déroulante des modèles GGUF disponibles (filtrage automatique des fichiers mmproj)
- **Host/Port** : Configuration du serveur (défaut: 127.0.0.1:8080)

### Paramètres de lancement (Container centre)
Paramètres configurables (laisser vide pour désactiver) :
- `-ngl` : GPU Layers
- `--parallel` : Requêtes parallèles
- `--ctx-size` : Taille du contexte
- `--flash-attn` : Flash Attention
- `--cache-type-k/v` : Types de cache
- `--batch-size` / `--ubatch-size` : Tailles de batch
- `--temp` : Température
- `--top-p` / `--top-k` : Sampling
- `--presence-penalty` : Pénalité de présence
- `--spec-type` / `--spec-draft-n-max` : Spéculation
- `--jinja` : Jinja (flag)
- `--chat-template-kwargs` : Template chat (JSON)

### Gestion des profils
- **Sauvegarder** : Enregistre les paramètres actifs dans un profil
- **Charger** : Restaure un profil sauvegardé
- **Importer commande** : Parse une commande existante et pré-remplit les paramètres

### Console (Container droite)
- Affichage en temps réel des logs (stdout/stderr)
- Auto-scroll vers les dernières lignes
- Boutons Copier/Effacer

### Contrôles
- **Démarrer** : Lance llama-server avec les paramètres configurés
- **Stop** : Arrête le serveur en cours
- **Redémarrer** : Stop + Démarrer automatique

### Benchmark
- **Benchmark All** : Lance `llama-bench` sur toutes les combinaisons modèles × versions llama.cpp
- **Benchmark Missing** : Lance uniquement les benchmarks qui n'existent pas encore dans `benchmark.md`
- **Barre de progression** : Affiche l'avancement en temps réel
- **Résultats** : Tableau Markdown trié par performance (prompt processing décroissant)
- **Sauvegarde** : Résultats enregistrés dans `benchmark.md`

**Paramètres fixes du benchmark :**
- NGL: 999
- Cache Type K/V: f16
- Prompt tokens: 512
- Generation tokens: 128
- Repetitions: 3

**Métriques capturées :**
- Prompt Processing (t/s) : Vitesse de traitement du prompt
- Generation (t/s) : Vitesse de génération de tokens

## Installation

### Prérequis
- .NET 8.0 SDK
- Windows (WPF)

### Compilation
```bash
dotnet build
```

### Exécution
```bash
dotnet run --project LLamaCppLauncher
```

### Publication (exécutable standalone)
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## Structure du projet

```
LLamaCppLauncher/
├── Models/
│   ├── AppConfig.cs          # Configuration persistante
│   ├── LaunchProfile.cs      # Profil de paramètres
│   ├── LlamaParameter.cs     # Paramètre individuel
│   ├── ModelInfo.cs          # Informations modèle
│   ├── BenchmarkResult.cs    # Résultat de benchmark
│   └── BenchmarkConfig.cs    # Configuration benchmark
├── Services/
│   ├── ConfigService.cs      # Gestion config.json
│   ├── ProfileService.cs     # Gestion profils
│   ├── ModelDiscoveryService.cs  # Scan versions/modèles
│   ├── CommandParserService.cs   # Parse commandes
│   ├── LlamaProcessService.cs    # Gestion processus
│   └── BenchmarkService.cs       # Gestion benchmarks
├── Converters/
│   └── BoolToVisibilityConverter.cs  # Converter WPF
├── ViewModels/
│   └── MainViewModel.cs      # Logique MVVM
├── MainWindow.xaml           # Interface utilisateur
└── MainWindow.xaml.cs        # Code-behind
```

## Fichiers générés

- `config.json` : Configuration persistante (chemins, sélections)
- `profiles/*.json` : Profils sauvegardés
- `benchmark.md` : Résultats des benchmarks (tableau Markdown)

## Architecture

- **Pattern** : MVVM (Model-View-ViewModel)
- **Framework** : CommunityToolkit.Mvvm
- **Thème** : Sombre (style VS Code)
- **Processus** : Une seule instance à la fois

## Utilisation

1. Configurez les répertoires llama.cpp et modèles
2. Sélectionnez une version et un modèle
3. Configurez les paramètres (laisser vide = désactivé)
4. Cliquez sur "Démarrer"
5. Surveillez les logs dans la console
6. Utilisez "Stop" pour arrêter le serveur

### Import d'une commande existante

1. Cliquez sur "Importer commande"
2. Collez votre commande (avec ou sans `^`)
3. Les paramètres sont automatiquement extraits
4. Les paramètres non mentionnés restent vides (désactivés)

## Exemple de commande importée

```
-ngl 999 ^
--parallel 1 ^
--ctx-size 65536 ^
--flash-attn on ^
--cache-type-k q8_0 ^
--cache-type-v q8_0 ^
--batch-size 2048 ^
--ubatch-size 512 ^
--top-p 0.95 ^
--top-k 40 ^
--presence-penalty 0 ^
--spec-type draft-mtp ^
--spec-draft-n-max 2 ^
--jinja ^
--chat-template-kwargs "{\"preserve_thinking\": true}"
```

### Benchmark

1. Configurez les répertoires llama.cpp et modèles
2. Cliquez sur **"Benchmark All"** pour tester toutes les combinaisons
3. Ou cliquez sur **"Benchmark Missing"** pour tester uniquement les combinaisons non benchmarkées
4. Surveillez la progression dans la barre de progression
5. Les résultats s'affichent dans la console en temps réel
6. Le tableau final est sauvegardé dans `benchmark.md`

**Note :** Chaque benchmark peut prendre 1-5 minutes selon le modèle et la configuration.

## Format du fichier benchmark.md

```markdown
# Benchmark Results

**Date de dernière mise à jour :** 2026-06-08 10:30:45

**Paramètres fixes :**
- NGL: 999
- Cache Type K/V: f16
- Prompt tokens: 512
- Generation tokens: 128
- Repetitions: 3

## Résultats

| Modèle | Quant | Version | Backend | Size | Params | PP (t/s) | TG (t/s) |
|--------|-------|---------|---------|------|--------|----------|----------|
| gemma-4-12B-it | Q4_K_M | llama-b9553-vulkan | Vulkan | 7.2 GiB | 12.0 B | 85.32 ± 1.23 | 15.67 ± 0.45 |
| qwen3.6-27B | Q4_K_M | llama-b9553-vulkan | Vulkan | 16.4 GiB | 26.9 B | 45.21 ± 1.87 | 8.34 ± 0.21 |
```
