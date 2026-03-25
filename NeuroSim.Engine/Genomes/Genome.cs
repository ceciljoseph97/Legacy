// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Genomes/Genome.cs
namespace NeuroSim.Engine.Genomes;

public enum GenomeType { Binary, RealValued, Tree, Graph, Permutation }

/// <summary>
/// Abstract base for all genome representations.
/// Each subclass encodes a candidate solution in the search space.
/// </summary>
public abstract class Genome
{
    public abstract GenomeType Type { get; }

    /// <summary>Logical length (number of genes / nodes / edges).</summary>
    public abstract int Length { get; }

    public abstract void Randomize(Random rng);

    /// <summary>Human-readable encoding — used in logs and fitness callbacks.</summary>
    public abstract string Serialize();

    public abstract Genome Clone();

    public override string ToString() => Serialize();
}
