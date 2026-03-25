// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Problems/TSP/TspConfig.cs
namespace NeuroSim.Problems.TSP;

public enum TspParadigm
{
    GeneticAlgorithm,
    EvolutionStrategy,
    EvolutionaryProgramming,
    GeneticProgramming
}

public enum TspSelectionMethod { Tournament, RouletteWheel, RankBased }
public enum TspCrossoverMethod { OX, PMX, EdgeRecombination, None }
public enum TspMutationMethod  { TwoOpt, Swap, Relocate, OrOpt }
public enum EsStrategy         { MuPlusLambda, MuCommaLambda }

public sealed class TspRunConfig
{
    public TspParadigm      Paradigm       { get; set; } = TspParadigm.GeneticAlgorithm;
    public TspSelectionMethod Selection    { get; set; } = TspSelectionMethod.Tournament;
    public TspCrossoverMethod Crossover    { get; set; } = TspCrossoverMethod.OX;
    public TspMutationMethod  Mutation     { get; set; } = TspMutationMethod.TwoOpt;
    public EsStrategy         EsStrategy  { get; set; } = EsStrategy.MuPlusLambda;

    public int    PopulationSize  { get; set; } = 80;
    public int    MaxGenerations  { get; set; } = 500;
    public double MutationRate    { get; set; } = 0.15;
    public double CrossoverRate   { get; set; } = 0.85;
    public int    TournamentSize  { get; set; } = 4;
    public int    EliteCount      { get; set; } = 4;
    public int    Lambda          { get; set; } = 5;   // ES offspring per parent
    public int    EpOpponents     { get; set; } = 5;   // EP tournament opponents
    public int    RandomSeed      { get; set; } = 42;

    /// <summary>Milliseconds to sleep between reported generations (0 = full speed).</summary>
    public int    GenerationDelayMs { get; set; } = 0;
}

/// <summary>Stats reported each generation.</summary>
public sealed record TspGenStats(
    int    Generation,
    double BestDistance,
    double MeanDistance,
    double WorstDistance,
    double Diversity,     // normalised std dev of tour lengths
    int[]  BestTour);
