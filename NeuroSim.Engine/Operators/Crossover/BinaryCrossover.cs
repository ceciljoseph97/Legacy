// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Operators/Crossover/BinaryCrossover.cs
using NeuroSim.Engine.Genomes;

namespace NeuroSim.Engine.Operators.Crossover;

public sealed class SinglePointCrossover : ICrossoverOperator<BinaryGenome>
{
    public (BinaryGenome child1, BinaryGenome child2) Crossover(BinaryGenome p1, BinaryGenome p2, Random rng)
    {
        int point = rng.Next(1, p1.Length);
        var c1 = (BinaryGenome)p1.Clone();
        var c2 = (BinaryGenome)p2.Clone();
        for (int i = point; i < p1.Length; i++) { c1[i] = p2[i]; c2[i] = p1[i]; }
        return (c1, c2);
    }
}

public sealed class TwoPointCrossover : ICrossoverOperator<BinaryGenome>
{
    public (BinaryGenome child1, BinaryGenome child2) Crossover(BinaryGenome p1, BinaryGenome p2, Random rng)
    {
        int a = rng.Next(1, p1.Length - 1);
        int b = rng.Next(a + 1, p1.Length);
        var c1 = (BinaryGenome)p1.Clone();
        var c2 = (BinaryGenome)p2.Clone();
        for (int i = a; i < b; i++) { c1[i] = p2[i]; c2[i] = p1[i]; }
        return (c1, c2);
    }
}

public sealed class UniformCrossover : ICrossoverOperator<BinaryGenome>
{
    public (BinaryGenome child1, BinaryGenome child2) Crossover(BinaryGenome p1, BinaryGenome p2, Random rng)
    {
        var c1 = (BinaryGenome)p1.Clone();
        var c2 = (BinaryGenome)p2.Clone();
        for (int i = 0; i < p1.Length; i++)
            if (rng.Next(2) == 0) { c1[i] = p2[i]; c2[i] = p1[i]; }
        return (c1, c2);
    }
}
