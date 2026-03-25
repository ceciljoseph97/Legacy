// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/Models/GenomeDisplayInfo.cs
namespace NeuroSim.UI.Models;

/// <summary>For binding graph edges in XAML.</summary>
public record EdgeDisplayItem(int From, int To, double Weight);

/// <summary>
/// Reusable genome display model for analysis panels.
/// Each playground provides one; GenomeDisplayView renders based on DisplayType.
/// </summary>
public enum GenomeDisplayType
{
    Permutation,        // TSP: city order [2,0,3,1,...]
    DirectionSequence,  // Maze: N/E/S/W moves
    Binary,             // Bit string 101010...
    RealValued,         // Weight vector [0.3, -1.4, ...]
    Graph               // Nodes + edges (adjacency / edge list)
}

/// <summary>
/// Data for genome visualization. Bind to GenomeDisplayView.
/// </summary>
public sealed class GenomeDisplayInfo
{
    public GenomeDisplayType DisplayType { get; init; }
    public string Title { get; init; } = "Best genome";
    public string FormattedValue { get; init; } = "";

    /// <summary>Permutation: city indices. TSP uses this.</summary>
    public int[]? Permutation { get; init; }

    /// <summary>Direction sequence: 0=N,1=E,2=S,3=W. Maze uses this.</summary>
    public int[]? DirectionSequence { get; init; }

    /// <summary>Binary: 0/1 bits.</summary>
    public bool[]? Binary { get; init; }

    /// <summary>Real-valued: weight vector. GP uses this.</summary>
    public double[]? RealValued { get; init; }

    /// <summary>Graph: (from, to, weight). GraphGenome uses this.</summary>
    public IReadOnlyList<(int From, int To, double Weight)>? GraphEdges { get; init; }

    /// <summary>For XAML binding of graph edges.</summary>
    public IEnumerable<EdgeDisplayItem>? GraphEdgesForDisplay =>
        GraphEdges?.Select(e => new EdgeDisplayItem(e.From, e.To, e.Weight));

    public int? NodeCount { get; init; }
    public bool? Directed { get; init; }

    /// <summary>Build compact string from raw data if FormattedValue empty.</summary>
    public string DisplayText => !string.IsNullOrEmpty(FormattedValue) ? FormattedValue : BuildDefault();

    private string BuildDefault()
    {
        return DisplayType switch
        {
            GenomeDisplayType.Permutation when Permutation != null
                => string.Join(" → ", Permutation.Take(Math.Min(Permutation.Length, 24)))
                   + (Permutation.Length > 24 ? " …" : ""),
            GenomeDisplayType.DirectionSequence when DirectionSequence != null
                => string.Join(" ", DirectionSequence.Take(Math.Min(DirectionSequence.Length, 40))
                    .Select(d => d switch { 0 => "N", 1 => "E", 2 => "S", 3 => "W", _ => "?" }))
                   + (DirectionSequence.Length > 40 ? " …" : ""),
            GenomeDisplayType.Binary when Binary != null
                => string.Concat(Binary.Take(80).Select(b => b ? '1' : '0'))
                   + (Binary.Length > 80 ? "…" : ""),
            GenomeDisplayType.RealValued when RealValued != null
                => "[" + string.Join(", ", RealValued.Take(8).Select(v => v.ToString("F2")))
                   + (RealValued.Length > 8 ? ", …]" : "]"),
            GenomeDisplayType.Graph when GraphEdges != null
                => $"{NodeCount ?? 0} nodes, {GraphEdges.Count} edges: "
                   + string.Join("; ", GraphEdges.Take(6).Select(e => $"{e.From}→{e.To}({e.Weight:F1})"))
                   + (GraphEdges.Count > 6 ? " …" : ""),
            _ => FormattedValue
        };
    }
}
