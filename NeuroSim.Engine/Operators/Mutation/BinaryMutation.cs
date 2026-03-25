// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Operators/Mutation/BinaryMutation.cs
using NeuroSim.Engine.Genomes;

namespace NeuroSim.Engine.Operators.Mutation;

/// <summary>Flip each bit independently with probability <c>rate</c>.</summary>
public sealed class FlipBitMutation : IMutationOperator<BinaryGenome>
{
    public BinaryGenome Mutate(BinaryGenome genome, double rate, Random rng)
    {
        var result = (BinaryGenome)genome.Clone();
        for (int i = 0; i < result.Length; i++)
            if (rng.NextDouble() < rate)
                result[i] = !result[i];
        return result;
    }
}
