// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/Models/Blueprint/BlockConfigEntry.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NeuroSim.UI.Models.Blueprint;

/// <summary>Editable config entry for the properties panel.</summary>
public sealed class BlockConfigEntry : INotifyPropertyChanged
{
    private object _value = "";

    public string Key { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public object Value
    {
        get => _value;
        set { _value = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? p = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}
