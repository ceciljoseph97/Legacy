// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Operators/Crossover/PermutationCrossover.cs
using NeuroSim.Engine.Genomes;

namespace NeuroSim.Engine.Operators.Crossover;

/// <summary>Order Crossover (OX) — preserves relative order of elements.</summary>
public sealed class OrderCrossover : ICrossoverOperator<PermutationGenome>
{
    public (PermutationGenome child1, PermutationGenome child2) Crossover(
        PermutationGenome p1, PermutationGenome p2, Random rng)
    {
        int n = p1.Length;
        int lo = rng.Next(n);
        int hi = rng.Next(lo, n);
        return (OX(p1.Order, p2.Order, lo, hi),
                OX(p2.Order, p1.Order, lo, hi));
    }

    private static PermutationGenome OX(int[] donor, int[] receiver, int lo, int hi)
    {
        int n = donor.Length;
        var child = new int[n];
        Array.Fill(child, -1);

        var inSegment = new HashSet<int>();
        for (int i = lo; i <= hi; i++) { child[i] = donor[i]; inSegment.Add(donor[i]); }

        var remaining = receiver.Where(x => !inSegment.Contains(x)).ToQueue();
        for (int i = 0; i < n; i++)
            if (child[i] == -1) child[i] = remaining.Dequeue();

        return new PermutationGenome(child);
    }
}

/// <summary>Partially Mapped Crossover (PMX).</summary>
public sealed class PMXCrossover : ICrossoverOperator<PermutationGenome>
{
    public (PermutationGenome child1, PermutationGenome child2) Crossover(
        PermutationGenome p1, PermutationGenome p2, Random rng)
    {
        int n = p1.Length;
        int lo = rng.Next(n);
        int hi = rng.Next(lo, n);
        return (PMX(p1.Order, p2.Order, lo, hi),
                PMX(p2.Order, p1.Order, lo, hi));
    }

    private static PermutationGenome PMX(int[] donor, int[] receiver, int lo, int hi)
    {
        int n = donor.Length;
        var child = new int[n];
        Array.Fill(child, -1);

        // Copy segment from donor
        for (int i = lo; i <= hi; i++) child[i] = donor[i];

        // Map conflicts
        var donorSeg = new HashSet<int>(donor[lo..(hi + 1)]);
        for (int i = 0; i < n; i++)
        {
            if (i >= lo && i <= hi) continue;
            int val = receiver[i];
            while (donorSeg.Contains(val))
            {
                int idx = Array.IndexOf(donor, val, lo, hi - lo + 1);
                val = receiver[idx];
            }
            child[i] = val;
        }
        return new PermutationGenome(child);
    }
}

// Helper
file static class Extensions
{
    public static Queue<T> ToQueue<T>(this IEnumerable<T> src) => new(src);
}
