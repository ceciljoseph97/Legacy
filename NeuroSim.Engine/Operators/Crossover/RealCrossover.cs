// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Operators/Crossover/RealCrossover.cs
using NeuroSim.Engine.Genomes;

namespace NeuroSim.Engine.Operators.Crossover;

/// <summary>BLX-α blend crossover for real-valued genomes.</summary>
public sealed class BlendCrossover : ICrossoverOperator<RealValuedGenome>
{
    private readonly double _alpha;

    public BlendCrossover(double alpha = 0.5) => _alpha = alpha;

    public (RealValuedGenome child1, RealValuedGenome child2) Crossover(
        RealValuedGenome p1, RealValuedGenome p2, Random rng)
    {
        var c1 = (RealValuedGenome)p1.Clone();
        var c2 = (RealValuedGenome)p2.Clone();
        for (int i = 0; i < p1.Length; i++)
        {
            double lo = Math.Min(p1[i], p2[i]);
            double hi = Math.Max(p1[i], p2[i]);
            double range = hi - lo;
            double blendLo = lo - _alpha * range;
            double blendHi = hi + _alpha * range;
            c1[i] = blendLo + rng.NextDouble() * (blendHi - blendLo);
            c2[i] = blendLo + rng.NextDouble() * (blendHi - blendLo);
        }
        return (c1, c2);
    }
}

/// <summary>Simulated binary crossover (SBX).</summary>
public sealed class SBXCrossover : ICrossoverOperator<RealValuedGenome>
{
    private readonly double _eta;

    public SBXCrossover(double eta = 2.0) => _eta = eta;

    public (RealValuedGenome child1, RealValuedGenome child2) Crossover(
        RealValuedGenome p1, RealValuedGenome p2, Random rng)
    {
        var c1 = (RealValuedGenome)p1.Clone();
        var c2 = (RealValuedGenome)p2.Clone();
        for (int i = 0; i < p1.Length; i++)
        {
            double u = rng.NextDouble();
            double beta = u < 0.5
                ? Math.Pow(2 * u, 1.0 / (_eta + 1))
                : Math.Pow(1.0 / (2 * (1 - u)), 1.0 / (_eta + 1));

            c1[i] = 0.5 * ((1 + beta) * p1[i] + (1 - beta) * p2[i]);
            c2[i] = 0.5 * ((1 - beta) * p1[i] + (1 + beta) * p2[i]);
        }
        return (c1, c2);
    }
}

/// <summary>Arithmetic (linear interpolation) crossover.</summary>
public sealed class ArithmeticCrossover : ICrossoverOperator<RealValuedGenome>
{
    public (RealValuedGenome child1, RealValuedGenome child2) Crossover(
        RealValuedGenome p1, RealValuedGenome p2, Random rng)
    {
        double alpha = rng.NextDouble();
        var c1 = (RealValuedGenome)p1.Clone();
        var c2 = (RealValuedGenome)p2.Clone();
        for (int i = 0; i < p1.Length; i++)
        {
            c1[i] = alpha * p1[i] + (1 - alpha) * p2[i];
            c2[i] = (1 - alpha) * p1[i] + alpha * p2[i];
        }
        return (c1, c2);
    }
}
