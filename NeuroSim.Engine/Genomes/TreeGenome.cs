// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Genomes/TreeGenome.cs
using System.Text;

namespace NeuroSim.Engine.Genomes;

// ── Node types ───────────────────────────────────────────────────────────────

public enum NodeKind { Function, Variable, Constant }

public sealed class TreeNode
{
    public NodeKind Kind { get; init; }
    public string Label { get; init; } = "";
    public double ConstValue { get; init; }
    public int VarIndex { get; init; }
    public int Arity { get; init; }
    public List<TreeNode> Children { get; init; } = new();

    // ── Evaluate ─────────────────────────────────────────────────────────────

    public double Evaluate(double[] vars) => Kind switch
    {
        NodeKind.Constant => ConstValue,
        NodeKind.Variable => vars[VarIndex],
        NodeKind.Function => EvalFunc(vars),
        _ => 0
    };

    private double EvalFunc(double[] vars)
    {
        var c = Children.Select(ch => ch.Evaluate(vars)).ToArray();
        return Label switch
        {
            "+" => c[0] + c[1],
            "-" => c[0] - c[1],
            "*" => c[0] * c[1],
            "/" => Math.Abs(c[1]) < 1e-9 ? 1.0 : c[0] / c[1],
            "sin" => Math.Sin(c[0]),
            "cos" => Math.Cos(c[0]),
            "exp" => Math.Exp(Math.Clamp(c[0], -10, 10)),
            "log" => c[0] <= 0 ? 0.0 : Math.Log(c[0]),
            "sqrt" => Math.Sqrt(Math.Abs(c[0])),
            "neg" => -c[0],
            _ => 0
        };
    }

    // ── Structural helpers ────────────────────────────────────────────────────

    public int Size() => 1 + Children.Sum(c => c.Size());
    public int Depth() => Children.Count == 0 ? 0 : 1 + Children.Max(c => c.Depth());

    public List<TreeNode> AllNodes()
    {
        var list = new List<TreeNode> { this };
        foreach (var ch in Children) list.AddRange(ch.AllNodes());
        return list;
    }

    public TreeNode Clone()
    {
        return new TreeNode
        {
            Kind = Kind,
            Label = Label,
            ConstValue = ConstValue,
            VarIndex = VarIndex,
            Arity = Arity,
            Children = Children.Select(c => c.Clone()).ToList()
        };
    }

    public override string ToString()
    {
        if (Kind == NodeKind.Constant) return ConstValue.ToString("F3");
        if (Kind == NodeKind.Variable) return $"x{VarIndex}";
        if (Children.Count == 1) return $"{Label}({Children[0]})";
        return $"({Children[0]} {Label} {Children[1]})";
    }
}

// ── TreeGenome ────────────────────────────────────────────────────────────────

/// <summary>Expression-tree genome for genetic programming.</summary>
public sealed class TreeGenome : Genome
{
    private static readonly (string name, int arity)[] DefaultFunctions =
    [
        ("+", 2), ("-", 2), ("*", 2), ("/", 2),
        ("sin", 1), ("cos", 1), ("sqrt", 1), ("neg", 1)
    ];

    private readonly int _numVars;
    private readonly int _maxDepth;

    public TreeNode Root { get; private set; }

    public TreeGenome(int numVariables = 1, int maxDepth = 5)
    {
        _numVars = numVariables;
        _maxDepth = maxDepth;
        Root = BuildRamped(new Random(), maxDepth);
    }

    private TreeGenome(TreeNode root, int numVars, int maxDepth)
    {
        Root = root;
        _numVars = numVars;
        _maxDepth = maxDepth;
    }

    public override GenomeType Type => GenomeType.Tree;
    public override int Length => Root.Size();

    public override void Randomize(Random rng) => Root = BuildRamped(rng, _maxDepth);

    public double Evaluate(double[] vars) => Root.Evaluate(vars);

    public override string Serialize() => Root.ToString();

    public override Genome Clone() => new TreeGenome(Root.Clone(), _numVars, _maxDepth);

    // ── Random tree generation ─────────────────────────────────────────────

    private TreeNode BuildRamped(Random rng, int maxDepth)
    {
        bool full = rng.Next(2) == 0;
        return Build(rng, 0, maxDepth, full);
    }

    private TreeNode Build(Random rng, int depth, int maxDepth, bool full)
    {
        bool mustBeLeaf = depth >= maxDepth;
        bool chooseLeaf = !full || mustBeLeaf
            ? mustBeLeaf || rng.NextDouble() < 0.3
            : false;

        if (chooseLeaf || mustBeLeaf)
            return MakeLeaf(rng);

        var (name, arity) = DefaultFunctions[rng.Next(DefaultFunctions.Length)];
        var node = new TreeNode { Kind = NodeKind.Function, Label = name, Arity = arity };
        for (int i = 0; i < arity; i++)
            node.Children.Add(Build(rng, depth + 1, maxDepth, full));
        return node;
    }

    private TreeNode MakeLeaf(Random rng)
    {
        if (_numVars > 0 && rng.NextDouble() < 0.7)
            return new TreeNode { Kind = NodeKind.Variable, VarIndex = rng.Next(_numVars), Label = $"x{rng.Next(_numVars)}" };
        return new TreeNode { Kind = NodeKind.Constant, ConstValue = rng.NextDouble() * 10 - 5 };
    }

    /// <summary>Swap a random subtree — used by subtree crossover/mutation.</summary>
    public static void SwapSubtree(TreeGenome genome, TreeNode replacement, Random rng)
    {
        var nodes = genome.Root.AllNodes();
        if (nodes.Count <= 1) { genome.Root = replacement; return; }

        // pick a random non-root node's parent and replace one child
        var candidates = nodes.Skip(1).ToList();
        var target = candidates[rng.Next(candidates.Count)];
        ReplaceInTree(genome.Root, target, replacement);
    }

    private static bool ReplaceInTree(TreeNode current, TreeNode target, TreeNode replacement)
    {
        for (int i = 0; i < current.Children.Count; i++)
        {
            if (current.Children[i] == target) { current.Children[i] = replacement; return true; }
            if (ReplaceInTree(current.Children[i], target, replacement)) return true;
        }
        return false;
    }
}
