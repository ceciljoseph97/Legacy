// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Operators/Mutation/GraphMutation.cs
using NeuroSim.Engine.Genomes;

namespace NeuroSim.Engine.Operators.Mutation;

/// <summary>Toggle random edges on/off and perturb edge weights.</summary>
public sealed class EdgeToggleMutation : IMutationOperator<GraphGenome>
{
    public GraphGenome Mutate(GraphGenome genome, double rate, Random rng)
    {
        var result = (GraphGenome)genome.Clone();
        for (int i = 0; i < genome.NodeCount; i++)
            for (int j = genome.Directed ? 0 : i + 1; j < genome.NodeCount; j++)
            {
                if (i == j) continue;
                if (rng.NextDouble() < rate)
                {
                    bool present = !result.HasEdge(i, j);
                    double weight = present
                        ? rng.NextDouble()
                        : result.EdgeWeight(i, j);
                    result.SetEdge(i, j, present, weight);
                }
                else if (result.HasEdge(i, j) && rng.NextDouble() < rate * 0.5)
                {
                    // perturb weight
                    result.SetEdge(i, j, true,
                        Math.Clamp(result.EdgeWeight(i, j) + (rng.NextDouble() - 0.5) * 0.2, 0, 1));
                }
            }
        return result;
    }
}
