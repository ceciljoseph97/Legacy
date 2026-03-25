// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Genomes/PermutationGenome.cs
namespace NeuroSim.Engine.Genomes;

/// <summary>
/// Permutation genome — ordered list of indices [0..N-1].
/// Used for combinatorial problems like TSP, scheduling, etc.
/// </summary>
public sealed class PermutationGenome : Genome
{
    private readonly int[] _order;

    public PermutationGenome(int size)
    {
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        _order = Enumerable.Range(0, size).ToArray();
    }

    public PermutationGenome(int[] order) => _order = (int[])order.Clone();

    public override GenomeType Type => GenomeType.Permutation;
    public override int Length => _order.Length;

    public int this[int i] => _order[i];
    public int[] Order => _order;

    public override void Randomize(Random rng)
    {
        // Fisher-Yates shuffle
        for (int i = _order.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (_order[i], _order[j]) = (_order[j], _order[i]);
        }
    }

    public override string Serialize() => string.Join("→", _order);

    public override Genome Clone() => new PermutationGenome(_order);

    /// <summary>Total distance using a caller-supplied distance matrix.</summary>
    public double TourLength(double[,] dist)
    {
        double total = 0;
        for (int i = 0; i < _order.Length; i++)
            total += dist[_order[i], _order[(i + 1) % _order.Length]];
        return total;
    }
}
