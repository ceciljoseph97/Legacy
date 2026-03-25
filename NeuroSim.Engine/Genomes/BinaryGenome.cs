// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Genomes/BinaryGenome.cs
using System.Text;

namespace NeuroSim.Engine.Genomes;

/// <summary>Bit-string genome — e.g. 1010100110.</summary>
public sealed class BinaryGenome : Genome
{
    private readonly bool[] _genes;

    public BinaryGenome(int length)
    {
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
        _genes = new bool[length];
    }

    private BinaryGenome(bool[] genes) => _genes = (bool[])genes.Clone();

    public override GenomeType Type => GenomeType.Binary;
    public override int Length => _genes.Length;

    public bool this[int i]
    {
        get => _genes[i];
        set => _genes[i] = value;
    }

    public ReadOnlySpan<bool> Genes => _genes;

    public bool[] GenesArray => _genes;

    public override void Randomize(Random rng)
    {
        for (int i = 0; i < _genes.Length; i++)
            _genes[i] = rng.Next(2) == 1;
    }

    public override string Serialize()
    {
        var sb = new StringBuilder(_genes.Length);
        foreach (bool g in _genes) sb.Append(g ? '1' : '0');
        return sb.ToString();
    }

    public override Genome Clone() => new BinaryGenome(_genes);

    public static BinaryGenome FromString(ReadOnlySpan<char> s)
    {
        var g = new BinaryGenome(s.Length);
        for (int i = 0; i < s.Length; i++) g[i] = s[i] == '1';
        return g;
    }

    /// <summary>Count of 1-bits (used by OneMax fitness etc.).</summary>
    public int CountOnes()
    {
        int n = 0;
        foreach (bool b in _genes) if (b) n++;
        return n;
    }
}
