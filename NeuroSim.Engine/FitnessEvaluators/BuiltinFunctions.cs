// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/FitnessEvaluators/BuiltinFunctions.cs
using NeuroSim.Engine.Genomes;
using NeuroSim.Engine.Operators;

namespace NeuroSim.Engine.FitnessEvaluators;

// ── Delegate wrapper ──────────────────────────────────────────────────────────

public sealed class DelegateFitnessEvaluator<G> : IFitnessEvaluator<G> where G : Genome
{
    private readonly Func<Genome, double> _fn;
    public string Name => "Custom";
    public string Description => "User-defined fitness function.";
    public DelegateFitnessEvaluator(Func<Genome, double> fn) => _fn = fn;
    public double Evaluate(G genome) => _fn(genome);
}

// ── Binary genome evaluators ──────────────────────────────────────────────────

/// <summary>OneMax: maximise number of 1-bits.</summary>
public sealed class OneMaxFitness : IFitnessEvaluator<BinaryGenome>
{
    public string Name => "OneMax";
    public string Description => "Maximise the number of 1-bits.";
    public double Evaluate(BinaryGenome g) => (double)g.CountOnes() / g.Length;
}

/// <summary>Trap function: deceptive landscape.</summary>
public sealed class TrapFitness : IFitnessEvaluator<BinaryGenome>
{
    private readonly int _blockSize;
    public string Name => "Trap";
    public string Description => "Deceptive trap function.";
    public TrapFitness(int blockSize = 5) => _blockSize = blockSize;
    public double Evaluate(BinaryGenome g)
    {
        double total = 0;
        int blocks = g.Length / _blockSize;
        for (int b = 0; b < blocks; b++)
        {
            int ones = 0;
            for (int i = b * _blockSize; i < (b + 1) * _blockSize; i++)
                if (g[i]) ones++;
            total += ones == _blockSize ? _blockSize : _blockSize - 1 - ones;
        }
        return total / (blocks * _blockSize);
    }
}

// ── Real-valued genome evaluators ─────────────────────────────────────────────

/// <summary>Sphere function: f(x) = -sum(x_i^2), maximised at 0.</summary>
public sealed class SphereFitness : IFitnessEvaluator<RealValuedGenome>
{
    public string Name => "Sphere";
    public string Description => "Minimise sum of squares — maximise -Sphere.";
    public double Evaluate(RealValuedGenome g)
    {
        double sum = 0;
        foreach (double x in g.Genes) sum += x * x;
        return -sum;
    }
}

/// <summary>Rastrigin: multimodal test function.</summary>
public sealed class RastriginFitness : IFitnessEvaluator<RealValuedGenome>
{
    public string Name => "Rastrigin";
    public string Description => "Highly multimodal — many local optima.";
    public double Evaluate(RealValuedGenome g)
    {
        double sum = 10 * g.Length;
        foreach (double x in g.Genes)
            sum += x * x - 10 * Math.Cos(2 * Math.PI * x);
        return -sum;
    }
}

/// <summary>Ackley: smooth global with rough local structure.</summary>
public sealed class AckleyFitness : IFitnessEvaluator<RealValuedGenome>
{
    public string Name => "Ackley";
    public string Description => "Near-neutral search space with steep global optimum.";
    public double Evaluate(RealValuedGenome g)
    {
        double sumSq = 0, sumCos = 0;
        foreach (double x in g.Genes) { sumSq += x * x; sumCos += Math.Cos(2 * Math.PI * x); }
        double n = g.Length;
        double val = -20 * Math.Exp(-0.2 * Math.Sqrt(sumSq / n))
                     - Math.Exp(sumCos / n) + 20 + Math.E;
        return -val;
    }
}

// ── Tree genome evaluators ────────────────────────────────────────────────────

/// <summary>Symbolic regression: fit y = x^2 + x + 1 on [-1,1].</summary>
public sealed class SymbolicRegressionFitness : IFitnessEvaluator<TreeGenome>
{
    private static readonly (double x, double y)[] DataPoints;

    static SymbolicRegressionFitness()
    {
        DataPoints = Enumerable.Range(-10, 21)
            .Select(i => { double x = i * 0.1; return (x, x * x + x + 1); })
            .ToArray();
    }

    public string Name => "SymbolicRegression";
    public string Description => "Fit y = x² + x + 1 on [-1,1].";

    public double Evaluate(TreeGenome g)
    {
        double error = 0;
        foreach (var (x, y) in DataPoints)
        {
            try
            {
                double pred = g.Evaluate(new[] { x });
                if (double.IsNaN(pred) || double.IsInfinity(pred)) { error += 1000; continue; }
                error += Math.Abs(pred - y);
            }
            catch { error += 1000; }
        }
        return -error / DataPoints.Length;
    }
}

// ── Graph genome evaluators ───────────────────────────────────────────────────

/// <summary>Max-spanning tree proxy: reward high total edge weight with connectivity.</summary>
public sealed class MaxSpanningFitness : IFitnessEvaluator<GraphGenome>
{
    public string Name => "MaxSpanning";
    public string Description => "Maximise total connected edge weight.";

    public double Evaluate(GraphGenome g)
    {
        double totalWeight = g.Edges().Sum(e => e.weight);
        double connectivityBonus = IsConnected(g) ? 1.0 : 0.0;
        return totalWeight + connectivityBonus * 5;
    }

    private static bool IsConnected(GraphGenome g)
    {
        if (g.NodeCount == 0) return true;
        var visited = new HashSet<int> { 0 };
        var queue = new Queue<int>();
        queue.Enqueue(0);
        while (queue.Count > 0)
        {
            int n = queue.Dequeue();
            foreach (int nb in g.Neighbors(n))
                if (visited.Add(nb)) queue.Enqueue(nb);
        }
        return visited.Count == g.NodeCount;
    }
}

// ── Registry ──────────────────────────────────────────────────────────────────

public static class BuiltinFitnessRegistry
{
    public static IFitnessEvaluator<BinaryGenome> GetBinary(string name) => name switch
    {
        "Trap" => new TrapFitness(),
        _ => new OneMaxFitness()
    };

    public static IFitnessEvaluator<RealValuedGenome> GetReal(string name) => name switch
    {
        "Rastrigin" => new RastriginFitness(),
        "Ackley" => new AckleyFitness(),
        _ => new SphereFitness()
    };

    public static IFitnessEvaluator<TreeGenome> GetTree(string _)
        => new SymbolicRegressionFitness();

    public static IFitnessEvaluator<GraphGenome> GetGraph(string _)
        => new MaxSpanningFitness();

    public static IReadOnlyList<string> BinaryOptions => ["OneMax", "Trap"];
    public static IReadOnlyList<string> RealOptions => ["Sphere", "Rastrigin", "Ackley"];
    public static IReadOnlyList<string> TreeOptions => ["SymbolicRegression"];
    public static IReadOnlyList<string> GraphOptions => ["MaxSpanning"];
}
