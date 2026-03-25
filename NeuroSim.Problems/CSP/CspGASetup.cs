// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Problems/CSP/CspGASetup.cs
using NeuroSim.Engine.Engine;
using NeuroSim.Engine.Genomes;
using NeuroSim.Engine.Operators;
using NeuroSim.Engine.Operators.Crossover;
using NeuroSim.Engine.Operators.Mutation;
using NeuroSim.Engine.Operators.Selection;

namespace NeuroSim.Problems.CSP;

// ── CSP fitness: minimise bins used (maximise -binCount) ─────────────────────

public sealed class CspFitness : IFitnessEvaluator<PermutationGenome>
{
    private readonly BinPackProblem _problem;

    public string Name => "Bin Count";
    public string Description => "Minimise number of bins used.";

    public CspFitness(BinPackProblem problem) => _problem = problem;

    public double Evaluate(PermutationGenome genome)
    {
        var assignment = _problem.Decode(genome.Order);
        // Penalise more bins, reward better fill
        return -(assignment.BinCount - assignment.AverageFill * 0.1);
    }
}

// ── Factory ───────────────────────────────────────────────────────────────────

public static class CspGASetup
{
    public static GAEngineBase CreateEngine(BinPackProblem problem, EvolutionConfig config)
    {
        var fitness   = new CspFitness(problem);
        var selection = new TournamentSelection<PermutationGenome>(config.TournamentSize);
        var crossover = new OrderCrossover();
        var mutation  = new RelocateMutation();

        return new GAEngine<PermutationGenome>(
            config,
            () => new PermutationGenome(problem.Items.Length),
            selection, crossover, mutation, fitness);
    }

    public static EvolutionConfig DefaultConfig(int itemCount) => new()
    {
        GenomeType     = GenomeType.Permutation,
        PopulationSize = 80,
        MaxGenerations = 300,
        EliteRatio     = 0.08,
        CrossoverRate  = 0.9,
        MutationRate   = 0.2,
        TournamentSize = 3,
        RandomSeed     = 42,
        LogInterval    = 1,
        BuiltinFitnessName = "CSP"
    };
}
