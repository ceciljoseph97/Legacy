// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Engine/GAEngineFactory.cs
using NeuroSim.Engine.Genomes;
using NeuroSim.Engine.Operators;
using NeuroSim.Engine.Operators.Crossover;
using NeuroSim.Engine.Operators.Mutation;
using NeuroSim.Engine.Operators.Selection;
using NeuroSim.Engine.FitnessEvaluators;

namespace NeuroSim.Engine.Engine;

/// <summary>Wires all components together from an <see cref="EvolutionConfig"/>.</summary>
public static class GAEngineFactory
{
    public static GAEngineBase Create(
        EvolutionConfig config,
        Func<Genome, double>? customFitness = null)
    {
        return config.GenomeType switch
        {
            GenomeType.Binary => BuildBinary(config, customFitness),
            GenomeType.RealValued => BuildReal(config, customFitness),
            GenomeType.Tree => BuildTree(config, customFitness),
            GenomeType.Graph => BuildGraph(config, customFitness),
            _ => throw new NotSupportedException($"GenomeType {config.GenomeType} not supported.")
        };
    }

    // ── Binary ────────────────────────────────────────────────────────────

    private static GAEngineBase BuildBinary(EvolutionConfig cfg, Func<Genome, double>? custom)
    {
        var selection = MakeSelection<BinaryGenome>(cfg);
        var crossover = cfg.BinaryCrossoverType switch
        {
            BinaryCrossoverType.SinglePoint => (ICrossoverOperator<BinaryGenome>)new SinglePointCrossover(),
            BinaryCrossoverType.TwoPoint => new TwoPointCrossover(),
            BinaryCrossoverType.Uniform => new UniformCrossover(),
            _ => new SinglePointCrossover()
        };
        var mutation = new FlipBitMutation();
        var fitness = custom != null
            ? new DelegateFitnessEvaluator<BinaryGenome>(custom)
            : (IFitnessEvaluator<BinaryGenome>)BuiltinFitnessRegistry.GetBinary(cfg.BuiltinFitnessName);

        return new GAEngine<BinaryGenome>(cfg,
            () => new BinaryGenome(cfg.GenomeLength),
            selection, crossover, mutation, fitness);
    }

    // ── Real-Valued ───────────────────────────────────────────────────────

    private static GAEngineBase BuildReal(EvolutionConfig cfg, Func<Genome, double>? custom)
    {
        var selection = MakeSelection<RealValuedGenome>(cfg);
        var crossover = cfg.RealCrossoverType switch
        {
            RealCrossoverType.Blend => (ICrossoverOperator<RealValuedGenome>)new BlendCrossover(),
            RealCrossoverType.SBX => new SBXCrossover(),
            RealCrossoverType.Arithmetic => new ArithmeticCrossover(),
            _ => new BlendCrossover()
        };
        var mutation = cfg.RealMutationType switch
        {
            RealMutationType.Gaussian => (IMutationOperator<RealValuedGenome>)new GaussianMutation(cfg.GaussianSigma),
            RealMutationType.Polynomial => new PolynomialMutation(),
            _ => new GaussianMutation(cfg.GaussianSigma)
        };
        var fitness = custom != null
            ? new DelegateFitnessEvaluator<RealValuedGenome>(custom)
            : (IFitnessEvaluator<RealValuedGenome>)BuiltinFitnessRegistry.GetReal(cfg.BuiltinFitnessName);

        return new GAEngine<RealValuedGenome>(cfg,
            () => new RealValuedGenome(cfg.GenomeLength, cfg.RealMinValue, cfg.RealMaxValue),
            selection, crossover, mutation, fitness);
    }

    // ── Tree ──────────────────────────────────────────────────────────────

    private static GAEngineBase BuildTree(EvolutionConfig cfg, Func<Genome, double>? custom)
    {
        var selection = MakeSelection<TreeGenome>(cfg);
        var crossover = new SubtreeCrossover();
        var mutation = new SubtreeMutation();
        var fitness = custom != null
            ? new DelegateFitnessEvaluator<TreeGenome>(custom)
            : (IFitnessEvaluator<TreeGenome>)BuiltinFitnessRegistry.GetTree(cfg.BuiltinFitnessName);

        return new GAEngine<TreeGenome>(cfg,
            () => new TreeGenome(cfg.NumTreeVariables, cfg.MaxTreeDepth),
            selection, crossover, mutation, fitness);
    }

    // ── Graph ─────────────────────────────────────────────────────────────

    private static GAEngineBase BuildGraph(EvolutionConfig cfg, Func<Genome, double>? custom)
    {
        var selection = MakeSelection<GraphGenome>(cfg);
        var crossover = new GraphUniformCrossover();
        var mutation = new EdgeToggleMutation();
        var fitness = custom != null
            ? new DelegateFitnessEvaluator<GraphGenome>(custom)
            : (IFitnessEvaluator<GraphGenome>)BuiltinFitnessRegistry.GetGraph(cfg.BuiltinFitnessName);

        return new GAEngine<GraphGenome>(cfg,
            () => new GraphGenome(cfg.GraphNodeCount, cfg.GraphDirected),
            selection, crossover, mutation, fitness);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static ISelectionOperator<G> MakeSelection<G>(EvolutionConfig cfg) where G : Genome
        => cfg.SelectionType switch
        {
            SelectionType.Tournament => (ISelectionOperator<G>)new TournamentSelection<G>(cfg.TournamentSize),
            SelectionType.RouletteWheel => new RouletteWheelSelection<G>(),
            SelectionType.Rank => new RankSelection<G>(),
            _ => new TournamentSelection<G>(cfg.TournamentSize)
        };
}
