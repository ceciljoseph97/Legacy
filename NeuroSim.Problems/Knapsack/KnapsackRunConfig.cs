// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Problems/Knapsack/KnapsackRunConfig.cs
namespace NeuroSim.Problems.Knapsack;

public sealed class KnapsackRunConfig
{
    public int PopulationSize { get; set; } = 80;
    public int MaxGenerations { get; set; } = 200;
    public double MutationRate { get; set; } = 0.02;
    public double CrossoverRate { get; set; } = 0.85;
    public int TournamentSize { get; set; } = 4;
    public int EliteCount { get; set; } = 4;
    public int RandomSeed { get; set; } = 42;
    public int GenerationDelayMs { get; set; } = 0;
}

public sealed record KnapsackGenStats(
    int Generation,
    double BestValue,
    double MeanValue,
    bool[] BestGenome
);
