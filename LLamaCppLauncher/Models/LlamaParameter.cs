using CommunityToolkit.Mvvm.ComponentModel;

namespace LLamaCppLauncher.Models;

public partial class LlamaParameter : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    [ObservableProperty]
    private string _defaultValue = string.Empty;

    [ObservableProperty]
    private bool _isFlag;

    public bool IsEnabled => !string.IsNullOrWhiteSpace(Value) || (IsFlag && Value == "on");
}
