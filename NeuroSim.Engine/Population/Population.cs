// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Population/Population.cs
using NeuroSim.Engine.Genomes;

namespace NeuroSim.Engine.Population;

/// <summary>Non-generic base so the UI/service layer can hold a reference without knowing G.</summary>
public abstract class PopulationBase
{
    public abstract int Count { get; }
    public abstract double BestFitness { get; }
    public abstract double MeanFitness { get; }
    public abstract double WorstFitness { get; }
    public abstract double StdDevFitness { get; }
    public abstract string BestGenomeStr { get; }
    public abstract IReadOnlyList<double> AllFitnesses { get; }
}

public abstract class PopulationBase<G> : PopulationBase where G : Genome
{
    public abstract IReadOnlyList<Individual<G>> Individuals { get; }
    public abstract Individual<G> Best { get; }
    public override IReadOnlyList<double> AllFitnesses =>
        Individuals.Select(i => i.Fitness).ToList();
}

/// <summary>Concrete typed population.</summary>
public sealed class Population<G> : PopulationBase<G> where G : Genome
{
    private readonly List<Individual<G>> _individuals = new();

    public override IReadOnlyList<Individual<G>> Individuals => _individuals;

    public override int Count => _individuals.Count;

    public void Add(Individual<G> ind) => _individuals.Add(ind);
    public void Clear() => _individuals.Clear();

    public Individual<G> this[int i] => _individuals[i];

    public override Individual<G> Best
    {
        get
        {
            if (_individuals.Count == 0) throw new InvalidOperationException("Empty population.");
            return _individuals.MaxBy(i => i.Fitness)!;
        }
    }

    public override double BestFitness => _individuals.Max(i => i.Fitness);
    public override double WorstFitness => _individuals.Min(i => i.Fitness);

    public override double MeanFitness
    {
        get
        {
            if (_individuals.Count == 0) return 0;
            return _individuals.Average(i => i.Fitness);
        }
    }

    public override double StdDevFitness
    {
        get
        {
            if (_individuals.Count < 2) return 0;
            double mean = MeanFitness;
            double variance = _individuals.Average(i => Math.Pow(i.Fitness - mean, 2));
            return Math.Sqrt(variance);
        }
    }

    public override string BestGenomeStr => Best.Genome.Serialize();
    public override IReadOnlyList<double> AllFitnesses => _individuals.Select(i => i.Fitness).ToList();

    /// <summary>Sort descending by fitness (best first).</summary>
    public void SortByFitness() => _individuals.Sort((a, b) => b.Fitness.CompareTo(a.Fitness));

    /// <summary>Replace individuals, keeping elite from previous generation.</summary>
    public void ReplaceGenerational(List<Individual<G>> newGen, int eliteCount)
    {
        SortByFitness();
        var elite = _individuals.Take(eliteCount).Select(e => e.Clone()).ToList();
        _individuals.Clear();
        _individuals.AddRange(elite);
        _individuals.AddRange(newGen.Take(newGen.Count - eliteCount));
    }
}
