// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/Models/ExperimentSnapshot.cs
using NeuroSim.Problems.TSP;

namespace NeuroSim.UI.Models;

public sealed class CitySnapshot
{
    public string Name { get; set; } = "";
    public double X    { get; set; }
    public double Y    { get; set; }
}

public sealed class GenerationRecord
{
    public int    Generation { get; set; }
    public double BestDist   { get; set; }
    public double MeanDist   { get; set; }
    public double WorstDist  { get; set; }
    public double Diversity  { get; set; }
}

public sealed class ExperimentSnapshot
{
    public string    Name             { get; set; } = "Untitled";
    public DateTime  SavedAt          { get; set; } = DateTime.Now;
    public string    DatasetLabel     { get; set; } = "";

    // ── Config ────────────────────────────────────────────────────────────
    public TspParadigm        Paradigm       { get; set; }
    public TspSelectionMethod Selection      { get; set; }
    public TspCrossoverMethod Crossover      { get; set; }
    public TspMutationMethod  Mutation       { get; set; }
    public EsStrategy         EsStrategy     { get; set; }
    public int    PopulationSize  { get; set; }
    public int    MaxGenerations  { get; set; }
    public double MutationRate    { get; set; }
    public double CrossoverRate   { get; set; }
    public int    TournamentSize  { get; set; }
    public int    RandomSeed      { get; set; }

    // ── Cities ────────────────────────────────────────────────────────────
    public List<CitySnapshot> Cities { get; set; } = new();

    // ── Results ───────────────────────────────────────────────────────────
    public int[]   BestTour         { get; set; } = Array.Empty<int>();
    public double  BestDistance     { get; set; }
    public double  MeanDistanceFinal { get; set; }
    public int     GenerationsRun   { get; set; }

    // ── Per-generation history ────────────────────────────────────────────
    public List<GenerationRecord> History { get; set; } = new();
}
