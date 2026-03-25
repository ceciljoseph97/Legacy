// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/Models/ChartSlot.cs
using OxyPlot;

namespace NeuroSim.UI.Models;

/// <summary>
/// Reusable chart slot for analysis panels.
/// Title + PlotModel; PlaygroundAnalysisView renders with export button.
/// </summary>
public sealed class ChartSlot
{
    public string     Title { get; init; } = "";
    public PlotModel?  Plot  { get; init; }
}
