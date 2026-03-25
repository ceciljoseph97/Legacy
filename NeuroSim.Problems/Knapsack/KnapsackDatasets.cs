// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Problems/Knapsack/KnapsackDatasets.cs
namespace NeuroSim.Problems.Knapsack;

public static class KnapsackDatasets
{
    public static IReadOnlyList<string> Names { get; } = ["Small 10", "Medium 20", "Hard 30"];

    public static KnapsackProblem Get(string name)
    {
        return name switch
        {
            "Small 10" => Small10(),
            "Medium 20" => Medium20(),
            "Hard 30" => Hard30(),
            _ => Small10()
        };
    }

    private static KnapsackProblem Small10()
    {
        var items = new (int, int)[]
        {
            (23, 92), (31, 57), (29, 49), (44, 68), (53, 60),
            (38, 43), (63, 67), (85, 84), (89, 87), (82, 72)
        };
        return new KnapsackProblem { Name = "Small 10", Capacity = 165, Items = items };
    }

    private static KnapsackProblem Medium20()
    {
        var rng = new Random(42);
        var items = Enumerable.Range(0, 20)
            .Select(_ => (rng.Next(10, 50), rng.Next(20, 100)))
            .ToArray();
        return new KnapsackProblem { Name = "Medium 20", Capacity = 300, Items = items };
    }

    private static KnapsackProblem Hard30()
    {
        var rng = new Random(7);
        var items = Enumerable.Range(0, 30)
            .Select(_ => (rng.Next(15, 60), rng.Next(30, 120)))
            .ToArray();
        return new KnapsackProblem { Name = "Hard 30", Capacity = 500, Items = items };
    }
}
