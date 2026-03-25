// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/Models/Blueprint/BlockDefinition.cs
namespace NeuroSim.UI.Models.Blueprint;

/// <summary>Metadata for a block type. Used in palette and for instantiation.</summary>
public sealed class BlockDefinition
{
    public string Id { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string Category { get; init; } = "";
    public string Description { get; init; } = "";
    public IReadOnlyList<PortDefinition> Inputs { get; init; } = Array.Empty<PortDefinition>();
    public IReadOnlyList<PortDefinition> Outputs { get; init; } = Array.Empty<PortDefinition>();
}

/// <summary>Input or output port on a block.</summary>
public sealed class PortDefinition
{
    public string Id { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string DataType { get; init; } = "any";
    public bool IsInput { get; init; }
}
