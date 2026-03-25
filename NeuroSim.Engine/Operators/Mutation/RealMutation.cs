// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Operators/Mutation/RealMutation.cs
using NeuroSim.Engine.Genomes;

namespace NeuroSim.Engine.Operators.Mutation;

/// <summary>Gaussian perturbation: x += N(0, sigma).</summary>
public sealed class GaussianMutation : IMutationOperator<RealValuedGenome>
{
    private readonly double _sigma;

    public GaussianMutation(double sigma = 0.1) => _sigma = sigma;

    public RealValuedGenome Mutate(RealValuedGenome genome, double rate, Random rng)
    {
        var result = (RealValuedGenome)genome.Clone();
        for (int i = 0; i < result.Length; i++)
            if (rng.NextDouble() < rate)
                result[i] += SampleGaussian(rng) * _sigma;
        return result;
    }

    // Box-Muller
    private static double SampleGaussian(Random rng)
    {
        double u1 = 1 - rng.NextDouble();
        double u2 = 1 - rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }
}

/// <summary>Polynomial mutation (DE/NSGA-II style).</summary>
public sealed class PolynomialMutation : IMutationOperator<RealValuedGenome>
{
    private readonly double _eta;

    public PolynomialMutation(double eta = 20.0) => _eta = eta;

    public RealValuedGenome Mutate(RealValuedGenome genome, double rate, Random rng)
    {
        var result = (RealValuedGenome)genome.Clone();
        double range = genome.MaxValue - genome.MinValue;
        for (int i = 0; i < result.Length; i++)
        {
            if (rng.NextDouble() >= rate) continue;
            double u = rng.NextDouble();
            double delta = u < 0.5
                ? Math.Pow(2 * u, 1.0 / (_eta + 1)) - 1
                : 1 - Math.Pow(2 * (1 - u), 1.0 / (_eta + 1));
            result[i] += delta * range;
        }
        return result;
    }
}
