// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/Models/MazeExperimentSnapshot.cs
using NeuroSim.Problems.Maze;

namespace NeuroSim.UI.Models;

/// <summary>
/// Snapshot model for Maze playground.
/// Reusable pattern: each playground defines its own snapshot DTO with config + problem state + history.
/// </summary>
public sealed class MazeExperimentSnapshot
{
    public string   Name         { get; set; } = "Untitled";
    public DateTime SavedAt     { get; set; } = DateTime.Now;
    public string   MazeLabel   { get; set; } = "";

    // ── Config ────────────────────────────────────────────────────────────
    public MazeParadigm       Paradigm    { get; set; }
    public MazeSelectionMethod Selection  { get; set; }
    public MazeMutationMethod  Mutation   { get; set; }
    public MazeCrossoverMethod Crossover  { get; set; }
    public MazeEsStrategy      EsStrategy { get; set; }
    public int    PopulationSize { get; set; }
    public int    MaxGenerations { get; set; }
    public int    PathLength     { get; set; }
    public double MutationRate   { get; set; }
    public double CrossoverRate  { get; set; }
    public int    TournamentSize { get; set; }
    public int    Lambda         { get; set; }
    public int    RandomSeed     { get; set; }

    // ── Maze grid (serialised from EditableMaze) ───────────────────────────
    public string MazeSerialised { get; set; } = "";

    // ── Results ───────────────────────────────────────────────────────────
    public double BestFitness    { get; set; }
    public double MeanFitnessFinal { get; set; }
    public int    GenerationsRun { get; set; }
    public bool   GoalReached    { get; set; }
    public int    StepsToGoal    { get; set; }

    // ── Per-generation history ─────────────────────────────────────────────
    public List<MazeGenerationRecord> History { get; set; } = new();
}

public sealed class MazeGenerationRecord
{
    public int    Generation  { get; set; }
    public double BestFitness { get; set; }
    public double MeanFitness { get; set; }
}
