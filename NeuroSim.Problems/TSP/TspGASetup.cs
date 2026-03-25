// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Problems/TSP/TspGASetup.cs
using NeuroSim.Engine.Engine;
using NeuroSim.Engine.Genomes;
using NeuroSim.Engine.Operators.Crossover;
using NeuroSim.Engine.Operators.Mutation;
using NeuroSim.Engine.Operators.Selection;
using NeuroSim.Engine.Population;
using NeuroSim.Engine.Operators;

namespace NeuroSim.Problems.TSP;

// ── TSP-specific fitness evaluator ───────────────────────────────────────────

public sealed class TspFitness : IFitnessEvaluator<PermutationGenome>
{
    private readonly double[,] _dist;
    private readonly int _n;

    public string Name => "TSP Distance";
    public string Description => "Minimise total tour distance (maximise -distance).";

    public TspFitness(TspProblem problem)
    {
        _dist = problem.DistanceMatrix;
        _n = problem.Cities.Length;
    }

    public double Evaluate(PermutationGenome genome)
    {
        double total = 0;
        var r = genome.Order;
        for (int i = 0; i < _n; i++)
            total += _dist[r[i], r[(i + 1) % _n]];
        return -total;  // maximise = minimise distance
    }
}

// ── Factory ───────────────────────────────────────────────────────────────────

public static class TspGASetup
{
    public static GAEngineBase CreateEngine(TspProblem problem, EvolutionConfig config)
    {
        int n = problem.Cities.Length;
        var fitness = new TspFitness(problem);

        var selection = new TournamentSelection<PermutationGenome>(config.TournamentSize);
        var crossover = new OrderCrossover();
        var mutation  = new TwoOptMutation();

        return new GAEngine<PermutationGenome>(
            config,
            () => new PermutationGenome(n),
            selection, crossover, mutation, fitness);
    }

    public static EvolutionConfig DefaultConfig(int cityCount) => new()
    {
        GenomeType     = GenomeType.Permutation,
        PopulationSize = Math.Max(50, cityCount * 5),
        MaxGenerations = 500,
        EliteRatio     = 0.05,
        CrossoverRate  = 0.85,
        MutationRate   = 0.15,
        TournamentSize = 4,
        RandomSeed     = 42,
        LogInterval    = 2,
        BuiltinFitnessName = "TSP"
    };
}
