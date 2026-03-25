// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/Models/Blueprint/BlueprintNode.cs
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NeuroSim.UI.Models.Blueprint;

/// <summary>Instance of a block on the canvas. Has position and optional config.</summary>
public sealed class BlueprintNode : INotifyPropertyChanged
{
    private double _x;
    private double _y;

    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public BlockDefinition Definition { get; init; } = null!;

    /// <summary>Editable parameters (e.g. "Method" = "Tournament", "PopulationSize" = 80).</summary>
    public Dictionary<string, object> Config { get; } = new();

    public double X
    {
        get => _x;
        set { if (Math.Abs(_x - value) > 0.01) { _x = value; OnPropertyChanged(); } }
    }

    public double Y
    {
        get => _y;
        set { if (Math.Abs(_y - value) > 0.01) { _y = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? p = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}
