// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/Models/Blueprint/BlueprintConnection.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NeuroSim.UI.Models.Blueprint;

/// <summary>Wire from an output port to an input port.</summary>
public sealed class BlueprintConnection : INotifyPropertyChanged
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public BlueprintNode SourceNode { get; init; } = null!;
    public string SourcePortId { get; init; } = "";
    public BlueprintNode TargetNode { get; init; } = null!;
    public string TargetPortId { get; init; } = "";

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? p = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}
