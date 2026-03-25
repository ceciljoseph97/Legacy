// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Operators/Crossover/TreeCrossover.cs
using NeuroSim.Engine.Genomes;

namespace NeuroSim.Engine.Operators.Crossover;

/// <summary>Standard subtree crossover for GP trees.</summary>
public sealed class SubtreeCrossover : ICrossoverOperator<TreeGenome>
{
    public (TreeGenome child1, TreeGenome child2) Crossover(TreeGenome p1, TreeGenome p2, Random rng)
    {
        var c1 = (TreeGenome)p1.Clone();
        var c2 = (TreeGenome)p2.Clone();

        var nodes1 = c1.Root.AllNodes();
        var nodes2 = c2.Root.AllNodes();

        // pick random subtree from each
        var sub1 = nodes1[rng.Next(nodes1.Count)].Clone();
        var sub2 = nodes2[rng.Next(nodes2.Count)].Clone();

        // swap: insert sub2 into c1, sub1 into c2
        if (nodes1.Count > 1)
            TreeGenome.SwapSubtree(c1, sub2, rng);
        else
            c1.Root.Children.Clear(); // edge case: root-only tree

        if (nodes2.Count > 1)
            TreeGenome.SwapSubtree(c2, sub1, rng);

        return (c1, c2);
    }
}
