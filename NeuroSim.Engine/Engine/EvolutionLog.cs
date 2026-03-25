// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Engine/EvolutionLog.cs
using System.Text;

namespace NeuroSim.Engine.Engine;

public sealed record GenerationStats(
    int Generation,
    double BestFitness,
    double MeanFitness,
    double StdDev,
    double WorstFitness,
    int Evaluations,
    double ElapsedMs,
    string BestGenomeStr
);

public sealed class EvolutionLog
{
    private readonly List<GenerationStats> _history = new();

    public IReadOnlyList<GenerationStats> History => _history;

    public GenerationStats? Latest => _history.Count > 0 ? _history[^1] : null;

    public void Record(GenerationStats stats) => _history.Add(stats);

    public void Clear() => _history.Clear();

    public string ToCSV()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Generation,BestFitness,MeanFitness,StdDev,WorstFitness,Evaluations,ElapsedMs,BestGenome");
        foreach (var s in _history)
            sb.AppendLine($"{s.Generation},{s.BestFitness:F6},{s.MeanFitness:F6},{s.StdDev:F6}," +
                          $"{s.WorstFitness:F6},{s.Evaluations},{s.ElapsedMs:F1},\"{s.BestGenomeStr}\"");
        return sb.ToString();
    }
}
