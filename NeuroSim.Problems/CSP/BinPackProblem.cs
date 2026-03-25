// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Problems/CSP/BinPackProblem.cs
namespace NeuroSim.Problems.CSP;

public sealed record BinPackItem(int Id, int Size, string Label = "");

public sealed class BinPackProblem
{
    public string Name { get; init; } = "Bin Packing";
    public BinPackItem[] Items { get; init; } = Array.Empty<BinPackItem>();
    public int BinCapacity { get; init; } = 100;

    public static BinPackProblem Sample => new()
    {
        Name = "CSP Sample (20 items, capacity 100)",
        BinCapacity = 100,
        Items = Enumerable.Range(0, 20)
            .Select(i => new BinPackItem(i, new[] { 10, 15, 20, 25, 30, 35, 40, 45, 50 }[i % 9], $"I{i}"))
            .ToArray()
    };

    public static BinPackProblem Random(int itemCount, int seed = 42)
    {
        var rng = new Random(seed);
        return new BinPackProblem
        {
            Name = $"Random {itemCount} items",
            BinCapacity = 100,
            Items = Enumerable.Range(0, itemCount)
                .Select(i => new BinPackItem(i, rng.Next(5, 51), $"I{i}"))
                .ToArray()
        };
    }

    /// <summary>Decode a permutation into bin assignments using First-Fit-Decreasing.</summary>
    public BinAssignment Decode(int[] order)
    {
        var bins = new List<List<BinPackItem>>();
        foreach (int idx in order)
        {
            var item = Items[idx];
            bool placed = false;
            foreach (var bin in bins)
            {
                int used = bin.Sum(x => x.Size);
                if (used + item.Size <= BinCapacity) { bin.Add(item); placed = true; break; }
            }
            if (!placed) bins.Add(new List<BinPackItem> { item });
        }
        return new BinAssignment(bins, BinCapacity);
    }
}

public sealed class BinAssignment
{
    public IReadOnlyList<IReadOnlyList<BinPackItem>> Bins { get; }
    public int BinCapacity { get; }
    public int BinCount => Bins.Count;
    public double AverageFill => Bins.Count == 0 ? 0
        : Bins.Average(b => b.Sum(i => i.Size) / (double)BinCapacity);

    public BinAssignment(List<List<BinPackItem>> bins, int capacity)
    {
        Bins = bins.Select(b => (IReadOnlyList<BinPackItem>)b.AsReadOnly()).ToList().AsReadOnly();
        BinCapacity = capacity;
    }
}
