// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Genomes/RealValuedGenome.cs
namespace NeuroSim.Engine.Genomes;

/// <summary>Real-valued genome — e.g. [0.3, -1.4, 2.1].</summary>
public sealed class RealValuedGenome : Genome
{
    private readonly double[] _genes;

    public RealValuedGenome(int length, double minValue = -5.0, double maxValue = 5.0)
    {
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
        _genes = new double[length];
        MinValue = minValue;
        MaxValue = maxValue;
    }

    private RealValuedGenome(double[] genes, double min, double max)
    {
        _genes = (double[])genes.Clone();
        MinValue = min;
        MaxValue = max;
    }

    public override GenomeType Type => GenomeType.RealValued;
    public override int Length => _genes.Length;
    public double MinValue { get; }
    public double MaxValue { get; }

    public double this[int i]
    {
        get => _genes[i];
        set => _genes[i] = Math.Clamp(value, MinValue, MaxValue);
    }

    public ReadOnlySpan<double> Genes => _genes;
    public double[] GenesArray => _genes;

    public override void Randomize(Random rng)
    {
        double range = MaxValue - MinValue;
        for (int i = 0; i < _genes.Length; i++)
            _genes[i] = MinValue + rng.NextDouble() * range;
    }

    public override string Serialize()
        => $"[{string.Join(", ", _genes.Select(g => g.ToString("F4")))}]";

    public override Genome Clone() => new RealValuedGenome(_genes, MinValue, MaxValue);
}
