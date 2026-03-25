// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/Models/StatBadge.cs
namespace NeuroSim.UI.Models;

/// <summary>
/// Reusable stat card for analysis panels.
/// Each playground provides a collection; PlaygroundAnalysisView renders them uniformly.
/// </summary>
public sealed class StatBadge
{
    public string Label    { get; init; } = "";
    public string Value    { get; init; } = "";
    public string SubLabel { get; init; } = "";
    public string ValueColor { get; init; } = "#F1F5F9";  // hex for value text
}
