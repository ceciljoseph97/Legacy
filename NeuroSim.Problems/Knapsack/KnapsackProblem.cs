// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Problems/Knapsack/KnapsackProblem.cs
namespace NeuroSim.Problems.Knapsack;

/// <summary>0/1 Knapsack: each item has weight and value. Maximise value subject to capacity.</summary>
public sealed class KnapsackProblem
{
    public string Name { get; init; } = "Knapsack";
    public int Capacity { get; init; }
    public (int Weight, int Value)[] Items { get; init; } = [];

    public int ItemCount => Items.Length;

    /// <summary>Fitness: total value if weight <= capacity, else 0.</summary>
    public double Fitness(bool[] genome)
    {
        if (genome.Length != Items.Length) return 0;
        int w = 0, v = 0;
        for (int i = 0; i < Items.Length; i++)
        {
            if (genome[i]) { w += Items[i].Weight; v += Items[i].Value; }
        }
        return w <= Capacity ? v : 0;
    }

    public int TotalWeight(bool[] genome)
    {
        int w = 0;
        for (int i = 0; i < Items.Length && i < genome.Length; i++)
            if (genome[i]) w += Items[i].Weight;
        return w;
    }

    public int TotalValue(bool[] genome)
    {
        int v = 0;
        for (int i = 0; i < Items.Length && i < genome.Length; i++)
            if (genome[i]) v += Items[i].Value;
        return v;
    }
}
