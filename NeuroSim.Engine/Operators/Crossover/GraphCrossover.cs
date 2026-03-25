// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Operators/Crossover/GraphCrossover.cs
using NeuroSim.Engine.Genomes;

namespace NeuroSim.Engine.Operators.Crossover;

/// <summary>Edge-uniform crossover for graph genomes.</summary>
public sealed class GraphUniformCrossover : ICrossoverOperator<GraphGenome>
{
    public (GraphGenome child1, GraphGenome child2) Crossover(GraphGenome p1, GraphGenome p2, Random rng)
    {
        var c1 = (GraphGenome)p1.Clone();
        var c2 = (GraphGenome)p2.Clone();

        var flat1 = p1.FlattenAdjacency();
        var flat2 = p2.FlattenAdjacency();
        var cf1 = new bool[flat1.Length];
        var cf2 = new bool[flat1.Length];

        for (int i = 0; i < flat1.Length; i++)
        {
            if (rng.Next(2) == 0) { cf1[i] = flat1[i]; cf2[i] = flat2[i]; }
            else { cf1[i] = flat2[i]; cf2[i] = flat1[i]; }
        }

        c1.UnflattenAdjacency(cf1, rng);
        c2.UnflattenAdjacency(cf2, rng);
        return (c1, c2);
    }
}
