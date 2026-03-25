// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Engine/GAEngine.cs
using System.Diagnostics;
using NeuroSim.Engine.Genomes;
using NeuroSim.Engine.Operators;
using NeuroSim.Engine.Population;

namespace NeuroSim.Engine.Engine;

/// <summary>Non-generic abstract base so the UI can hold one reference regardless of genome type.</summary>
public abstract class GAEngineBase
{
    public EvolutionConfig Config { get; }
    public EvolutionLog Log { get; } = new();
    public abstract PopulationBase CurrentPopulation { get; }
    public abstract string BestGenomeStr { get; }
    public abstract double BestFitness { get; }
    public bool IsRunning { get; protected set; }

    public event EventHandler<GenerationStats>? OnGenerationComplete;
    public event EventHandler? OnComplete;

    protected GAEngineBase(EvolutionConfig config) => Config = config;

    public abstract void Initialize();
    public abstract Task RunAsync(CancellationToken ct = default);
    public void Stop() => IsRunning = false;

    public IReadOnlyList<double> GetAllFitnesses() =>
        CurrentPopulation.AllFitnesses;

    protected void RaiseGenerationComplete(GenerationStats stats) =>
        OnGenerationComplete?.Invoke(this, stats);
    protected void RaiseComplete() => OnComplete?.Invoke(this, EventArgs.Empty);
}

/// <summary>Typed GA engine for a specific genome type.</summary>
public sealed class GAEngine<G> : GAEngineBase where G : Genome
{
    private readonly ISelectionOperator<G> _selection;
    private readonly ICrossoverOperator<G> _crossover;
    private readonly IMutationOperator<G> _mutation;
    private readonly IFitnessEvaluator<G> _fitness;
    private readonly Func<G> _genomeFactory;
    private readonly Random _rng;
    private Population<G> _population = new();

    public override PopulationBase CurrentPopulation => _population;
    public override string BestGenomeStr => _population.Count > 0 ? _population.BestGenomeStr : "";
    public override double BestFitness => _population.Count > 0 ? _population.BestFitness : double.NegativeInfinity;

    public GAEngine(
        EvolutionConfig config,
        Func<G> genomeFactory,
        ISelectionOperator<G> selection,
        ICrossoverOperator<G> crossover,
        IMutationOperator<G> mutation,
        IFitnessEvaluator<G> fitness)
        : base(config)
    {
        _genomeFactory = genomeFactory;
        _selection = selection;
        _crossover = crossover;
        _mutation = mutation;
        _fitness = fitness;
        _rng = new Random(config.RandomSeed);
    }

    public override void Initialize()
    {
        _population.Clear();
        Log.Clear();
        for (int i = 0; i < Config.PopulationSize; i++)
        {
            var g = _genomeFactory();
            g.Randomize(_rng);
            _population.Add(new Individual<G>(g));
        }
        EvaluateAll();
    }

    public override async Task RunAsync(CancellationToken ct = default)
    {
        IsRunning = true;
        int totalEvals = Config.PopulationSize;
        var sw = Stopwatch.StartNew();

        for (int gen = 1; gen <= Config.MaxGenerations && IsRunning && !ct.IsCancellationRequested; gen++)
        {
            var genSw = Stopwatch.StartNew();
            EvolveGeneration(ref totalEvals);
            genSw.Stop();

            if (gen % Config.LogInterval == 0)
            {
                var stats = new GenerationStats(
                    gen,
                    _population.BestFitness,
                    _population.MeanFitness,
                    _population.StdDevFitness,
                    _population.WorstFitness,
                    totalEvals,
                    genSw.Elapsed.TotalMilliseconds,
                    _population.BestGenomeStr);

                Log.Record(stats);
                RaiseGenerationComplete(stats);
            }

            if (Config.UseTargetFitness && _population.BestFitness >= Config.TargetFitness)
                break;

            // Yield to not starve the UI thread
            await Task.Yield();
        }

        IsRunning = false;
        RaiseComplete();
    }

    // ── Core evolution step ───────────────────────────────────────────────

    private void EvolveGeneration(ref int totalEvals)
    {
        int popSize = Config.PopulationSize;
        int eliteCount = Math.Max(1, (int)(popSize * Config.EliteRatio));
        int offspring = popSize - eliteCount;

        // Selection + Crossover + Mutation
        var newGen = new List<Individual<G>>(offspring);
        while (newGen.Count < offspring)
        {
            var parents = _selection.Select(_population, 2, _rng);
            G child1, child2;

            if (_rng.NextDouble() < Config.CrossoverRate)
            {
                (child1, child2) = _crossover.Crossover(parents[0].Genome, parents[1].Genome, _rng);
            }
            else
            {
                child1 = (G)parents[0].Genome.Clone();
                child2 = (G)parents[1].Genome.Clone();
            }

            child1 = _mutation.Mutate(child1, Config.MutationRate, _rng);
            child2 = _mutation.Mutate(child2, Config.MutationRate, _rng);

            newGen.Add(new Individual<G>(child1));
            if (newGen.Count < offspring) newGen.Add(new Individual<G>(child2));
        }

        // Evaluate new individuals
        foreach (var ind in newGen)
        {
            ind.Fitness = _fitness.Evaluate(ind.Genome);
            ind.Age = 0;
            totalEvals++;
        }

        // Replace with elitism
        _population.ReplaceGenerational(newGen, eliteCount);

        // Age existing individuals
        foreach (var ind in _population.Individuals) ind.Age++;
    }

    private void EvaluateAll()
    {
        foreach (var ind in _population.Individuals)
            ind.Fitness = _fitness.Evaluate(ind.Genome);
    }
}
