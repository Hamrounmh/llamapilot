using System;
using System.Windows;
using System.Windows.Markup;
using LLamaCppLauncher.Services;

namespace LLamaCppLauncher.Helpers;

public class LocExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    private object? _target;
    private DependencyProperty? _targetProperty;

    public LocExtension() { }

    public LocExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var target = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
        if (target != null)
        {
            _target = target.TargetObject;
            _targetProperty = target.TargetProperty as DependencyProperty;
        }

        LocalizationService.Instance.LanguageChanged += OnLanguageChanged;

        return LocalizationService.Instance[Key];
    }

    private void OnLanguageChanged()
    {
        if (_target is DependencyObject depObj && _targetProperty != null)
        {
            if (depObj is FrameworkElement element && !element.IsLoaded)
                return;

            depObj.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    depObj.SetValue(_targetProperty, LocalizationService.Instance[Key]);
                }
                catch
                {
                }
            });
        }
    }
}
