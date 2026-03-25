// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Genomes/GraphGenome.cs
using System.Text;

namespace NeuroSim.Engine.Genomes;

/// <summary>Graph genome — adjacency matrix + edge weights. Useful for network design.</summary>
public sealed class GraphGenome : Genome
{
    private readonly bool[,] _adj;
    private readonly double[,] _weights;

    public int NodeCount { get; }
    public bool Directed { get; }

    public GraphGenome(int nodeCount, bool directed = false)
    {
        if (nodeCount <= 0) throw new ArgumentOutOfRangeException(nameof(nodeCount));
        NodeCount = nodeCount;
        Directed = directed;
        _adj = new bool[nodeCount, nodeCount];
        _weights = new double[nodeCount, nodeCount];
    }

    private GraphGenome(GraphGenome src)
    {
        NodeCount = src.NodeCount;
        Directed = src.Directed;
        _adj = (bool[,])src._adj.Clone();
        _weights = (double[,])src._weights.Clone();
    }

    public override GenomeType Type => GenomeType.Graph;

    // Length = number of possible edges
    public override int Length => Directed
        ? NodeCount * (NodeCount - 1)
        : NodeCount * (NodeCount - 1) / 2;

    public bool HasEdge(int i, int j) => _adj[i, j];
    public double EdgeWeight(int i, int j) => _weights[i, j];

    public void SetEdge(int i, int j, bool present, double weight = 1.0)
    {
        _adj[i, j] = present;
        _weights[i, j] = weight;
        if (!Directed)
        {
            _adj[j, i] = present;
            _weights[j, i] = weight;
        }
    }

    public int EdgeCount()
    {
        int count = 0;
        for (int i = 0; i < NodeCount; i++)
            for (int j = Directed ? 0 : i + 1; j < NodeCount; j++)
                if (i != j && _adj[i, j]) count++;
        return count;
    }

    public IEnumerable<(int from, int to, double weight)> Edges()
    {
        for (int i = 0; i < NodeCount; i++)
            for (int j = Directed ? 0 : i + 1; j < NodeCount; j++)
                if (i != j && _adj[i, j])
                    yield return (i, j, _weights[i, j]);
    }

    public List<int> Neighbors(int node)
    {
        var n = new List<int>();
        for (int j = 0; j < NodeCount; j++)
            if (j != node && _adj[node, j]) n.Add(j);
        return n;
    }

    public override void Randomize(Random rng)
    {
        for (int i = 0; i < NodeCount; i++)
            for (int j = Directed ? 0 : i + 1; j < NodeCount; j++)
            {
                if (i == j) continue;
                bool present = rng.NextDouble() < 0.3;
                double w = rng.NextDouble();
                SetEdge(i, j, present, w);
            }
    }

    public override string Serialize()
    {
        var sb = new StringBuilder();
        sb.Append($"Graph(N={NodeCount}, E={EdgeCount()}): ");
        foreach (var (f, t, w) in Edges())
            sb.Append($"{f}->{t}({w:F2}) ");
        return sb.ToString().TrimEnd();
    }

    public override Genome Clone() => new GraphGenome(this);

    /// <summary>Get adjacency matrix as flat bool array for crossover.</summary>
    public bool[] FlattenAdjacency()
    {
        var flat = new List<bool>();
        for (int i = 0; i < NodeCount; i++)
            for (int j = Directed ? 0 : i + 1; j < NodeCount; j++)
                if (i != j) flat.Add(_adj[i, j]);
        return flat.ToArray();
    }

    /// <summary>Restore adjacency from flat bool array.</summary>
    public void UnflattenAdjacency(bool[] flat, Random rng)
    {
        int k = 0;
        for (int i = 0; i < NodeCount && k < flat.Length; i++)
            for (int j = Directed ? 0 : i + 1; j < NodeCount && k < flat.Length; j++)
            {
                if (i == j) continue;
                double w = HasEdge(i, j) ? EdgeWeight(i, j) : rng.NextDouble();
                SetEdge(i, j, flat[k++], w);
            }
    }
}
