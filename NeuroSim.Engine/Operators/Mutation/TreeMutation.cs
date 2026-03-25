// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Operators/Mutation/TreeMutation.cs
using NeuroSim.Engine.Genomes;

namespace NeuroSim.Engine.Operators.Mutation;

/// <summary>Replace a random subtree with a freshly grown random tree.</summary>
public sealed class SubtreeMutation : IMutationOperator<TreeGenome>
{
    public TreeGenome Mutate(TreeGenome genome, double rate, Random rng)
    {
        if (rng.NextDouble() >= rate) return (TreeGenome)genome.Clone();

        var result = (TreeGenome)genome.Clone();
        // Build a tiny random replacement subtree
        var stub = new TreeGenome(1, 3);
        stub.Randomize(rng);
        TreeGenome.SwapSubtree(result, stub.Root.Clone(), rng);
        return result;
    }
}

/// <summary>Replace a random constant leaf with a new random constant.</summary>
public sealed class ConstantPerturbMutation : IMutationOperator<TreeGenome>
{
    private readonly double _sigma;

    public ConstantPerturbMutation(double sigma = 0.5) => _sigma = sigma;

    public TreeGenome Mutate(TreeGenome genome, double rate, Random rng)
    {
        var result = (TreeGenome)genome.Clone();
        var constants = result.Root.AllNodes()
            .Where(n => n.Kind == NodeKind.Constant)
            .ToList();

        foreach (var node in constants)
        {
            if (rng.NextDouble() < rate)
            {
                // TreeNode is record-like (init setters) — replace via parent
                // For simplicity, rebuild with a new value via reflection workaround:
                // We use the mutable ConstValue approach — but TreeNode uses init.
                // We rebuild with a hack: the parent swap via SwapSubtree.
                var replacement = new TreeNode
                {
                    Kind = NodeKind.Constant,
                    ConstValue = node.ConstValue + (rng.NextDouble() * 2 - 1) * _sigma
                };
                TreeGenome.SwapSubtree(result, replacement, rng);
            }
        }
        return result;
    }
}
