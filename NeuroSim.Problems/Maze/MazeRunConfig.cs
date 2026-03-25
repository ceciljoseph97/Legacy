// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Problems/Maze/MazeRunConfig.cs
namespace NeuroSim.Problems.Maze;

// ── Genome encoding ────────────────────────────────────────────────────────────
// Each individual  = int[] of length PathLength.
// Values: 0=North  1=East  2=South  3=West
// The agent tries each move; wall hits leave the agent in place (+ penalty).

// ── Paradigm ──────────────────────────────────────────────────────────────────
public enum MazeParadigm
{
    GeneticAlgorithm,         // selection + crossover + mutation
    EvolutionStrategy,        // mutation-heavy, (μ+λ) or (μ,λ), self-adaptive rate
    EvolutionaryProgramming   // each parent → one offspring, tournament among all
}

// ── Operators ─────────────────────────────────────────────────────────────────
public enum MazeSelectionMethod
{
    Tournament,     // k-way tournament
    RouletteWheel,  // fitness-proportionate
    RankBased       // linear rank selection
}

public enum MazeMutationMethod
{
    PointMutation,  // replace random genes with random directions — broad exploration
    Inversion,      // reverse a random sub-sequence  — preserves corridor runs
    SegmentShuffle, // shuffle a random sub-sequence  — disrupts stuck sub-paths
    BlockReset      // reset a random block to random directions — escape local optima
}

public enum MazeCrossoverMethod
{
    TwoPoint,       // two-point cut-and-splice
    SinglePoint,    // single cut
    Uniform,        // each gene chosen randomly from either parent
    None            // mutation only (forced for ES/EP)
}

public enum MazeEsStrategy { MuPlusLambda, MuCommaLambda }

// ── Config ────────────────────────────────────────────────────────────────────
public sealed class MazeRunConfig
{
    public MazeParadigm       Paradigm    { get; set; } = MazeParadigm.GeneticAlgorithm;
    public MazeSelectionMethod Selection  { get; set; } = MazeSelectionMethod.Tournament;
    public MazeMutationMethod  Mutation   { get; set; } = MazeMutationMethod.Inversion;
    public MazeCrossoverMethod Crossover  { get; set; } = MazeCrossoverMethod.TwoPoint;
    public MazeEsStrategy      EsStrategy { get; set; } = MazeEsStrategy.MuPlusLambda;

    public int    PopulationSize   { get; set; } = 100;
    public int    MaxGenerations   { get; set; } = 600;
    public int    PathLength       { get; set; } = 500;  // longer = more room to navigate winding corridors
    public double MutationRate     { get; set; } = 0.05;
    public double CrossoverRate    { get; set; } = 0.85;
    public int    TournamentSize   { get; set; } = 4;
    public int    EliteCount       { get; set; } = 4;
    public int    Lambda           { get; set; } = 5;    // ES: offspring per parent
    public int    EpOpponents      { get; set; } = 5;    // EP: tournament opponents
    public int    RandomSeed       { get; set; } = 42;

    /// <summary>Milliseconds to sleep between generations (0 = full speed).</summary>
    public int    GenerationDelayMs { get; set; } = 50;
}

/// <summary>Stats fired every generation.</summary>
public sealed record MazeGenStats(
    int              Generation,
    double           BestFitness,
    double           MeanFitness,
    bool             GoalReached,
    int              StepsToGoal,    // -1 when goal not yet reached
    (int X, int Y)[] BestPath,        // actual positions walked by best individual
    int[]            BestGenome      // direction sequence 0=N,1=E,2=S,3=W
);
