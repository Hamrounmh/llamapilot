using System.Windows;
using LLamaCppLauncher.Services;

namespace LLamaCppLauncher;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var configService = new ConfigService();
        var config = configService.Load();

        if (!string.IsNullOrEmpty(config.Language))
            LocalizationService.Instance.SetLanguage(config.Language);
    }
}
