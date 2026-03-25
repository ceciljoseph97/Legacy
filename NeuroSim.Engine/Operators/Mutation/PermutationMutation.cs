// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Operators/Mutation/PermutationMutation.cs
using NeuroSim.Engine.Genomes;

namespace NeuroSim.Engine.Operators.Mutation;

/// <summary>Swap two random positions.</summary>
public sealed class SwapMutation : IMutationOperator<PermutationGenome>
{
    public PermutationGenome Mutate(PermutationGenome genome, double rate, Random rng)
    {
        var result = (PermutationGenome)genome.Clone();
        var order = result.Order;
        for (int i = 0; i < order.Length; i++)
        {
            if (rng.NextDouble() < rate)
            {
                int j = rng.Next(order.Length);
                (order[i], order[j]) = (order[j], order[i]);
            }
        }
        return result;
    }
}

/// <summary>Reverse a random sub-tour (2-opt move).</summary>
public sealed class TwoOptMutation : IMutationOperator<PermutationGenome>
{
    public PermutationGenome Mutate(PermutationGenome genome, double rate, Random rng)
    {
        if (rng.NextDouble() >= rate) return (PermutationGenome)genome.Clone();

        var result = (PermutationGenome)genome.Clone();
        var order = result.Order;
        int n = order.Length;

        int i = rng.Next(n);
        int j = rng.Next(i + 1, Math.Min(i + n / 2, n));

        Array.Reverse(order, i, j - i);
        return result;
    }
}

/// <summary>Relocate: remove city at random, reinsert at different position.</summary>
public sealed class RelocateMutation : IMutationOperator<PermutationGenome>
{
    public PermutationGenome Mutate(PermutationGenome genome, double rate, Random rng)
    {
        if (rng.NextDouble() >= rate) return (PermutationGenome)genome.Clone();

        var order = (int[])genome.Order.Clone();
        int from = rng.Next(order.Length);
        int to = rng.Next(order.Length);
        if (from == to) return new PermutationGenome(order);

        int city = order[from];
        var list = new List<int>(order);
        list.RemoveAt(from);
        list.Insert(to > from ? to - 1 : to, city);
        return new PermutationGenome(list.ToArray());
    }
}
