// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Engine/EvolutionConfig.cs
using NeuroSim.Engine.Genomes;

namespace NeuroSim.Engine.Engine;

public enum SelectionType { Tournament, RouletteWheel, Rank }
public enum BinaryCrossoverType { SinglePoint, TwoPoint, Uniform }
public enum RealCrossoverType { Blend, SBX, Arithmetic }
public enum BinaryMutationType { FlipBit }
public enum RealMutationType { Gaussian, Polynomial }

public sealed class EvolutionConfig
{
    // ── Population ────────────────────────────────────────────────────────
    public int PopulationSize { get; set; } = 100;
    public int MaxGenerations { get; set; } = 200;
    public double EliteRatio { get; set; } = 0.05;

    // ── Genome ────────────────────────────────────────────────────────────
    public GenomeType GenomeType { get; set; } = GenomeType.Binary;
    public int GenomeLength { get; set; } = 30;

    // Real-valued params
    public double RealMinValue { get; set; } = -5.0;
    public double RealMaxValue { get; set; } = 5.0;

    // Tree genome params
    public int NumTreeVariables { get; set; } = 1;
    public int MaxTreeDepth { get; set; } = 5;

    // Graph genome params
    public int GraphNodeCount { get; set; } = 8;
    public bool GraphDirected { get; set; } = false;

    // ── Operators ─────────────────────────────────────────────────────────
    public SelectionType SelectionType { get; set; } = SelectionType.Tournament;
    public int TournamentSize { get; set; } = 3;

    public BinaryCrossoverType BinaryCrossoverType { get; set; } = BinaryCrossoverType.SinglePoint;
    public RealCrossoverType RealCrossoverType { get; set; } = RealCrossoverType.Blend;

    public BinaryMutationType BinaryMutationType { get; set; } = BinaryMutationType.FlipBit;
    public RealMutationType RealMutationType { get; set; } = RealMutationType.Gaussian;

    // ── Rates ─────────────────────────────────────────────────────────────
    public double CrossoverRate { get; set; } = 0.8;
    public double MutationRate { get; set; } = 0.01;
    public double GaussianSigma { get; set; } = 0.1;

    // ── Termination ───────────────────────────────────────────────────────
    public bool UseTargetFitness { get; set; } = false;
    public double TargetFitness { get; set; } = 1.0;

    // ── Built-in fitness function (used when no custom fn is provided) ─────
    public string BuiltinFitnessName { get; set; } = "OneMax";

    // ── Misc ──────────────────────────────────────────────────────────────
    public int RandomSeed { get; set; } = 42;
    public int LogInterval { get; set; } = 1;

    public EvolutionConfig Clone() => (EvolutionConfig)MemberwiseClone();
}
