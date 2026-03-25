// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.SystemTests/TspSystemTests.cs
using System.Text.Json;
using NeuroSim.Problems.TSP;
using Xunit;

namespace NeuroSim.SystemTests;

/// <summary>
/// System tests for TSP playground: full evolution runs, snapshot format.
/// </summary>
public sealed class TspSystemTests
{
    [Fact]
    public async Task FullRun_GA_50Generations_BestImprovesOrStaysReasonable()
    {
        var problem = TspDatasets.Ulysses16;
        var cfg = new TspRunConfig
        {
            Paradigm = TspParadigm.GeneticAlgorithm,
            PopulationSize = 60,
            MaxGenerations = 50,
            GenerationDelayMs = 0,
            RandomSeed = 42
        };
        var evolver = new TspEvolver(problem, cfg);
        var stats = new List<TspGenStats>();
        evolver.OnGeneration += s => stats.Add(s);

        await evolver.RunAsync(CancellationToken.None);

        Assert.Equal(50, stats.Count);
        double firstBest = stats[0].BestDistance;
        double lastBest = stats[^1].BestDistance;
        Assert.True(lastBest <= firstBest * 1.01, $"Best should improve or stay similar: first={firstBest:F1} last={lastBest:F1}");
        Assert.True(lastBest > 0);
    }

    [Fact]
    public async Task FullRun_ES_30Generations_Completes()
    {
        var problem = TspDatasets.Ulysses16;
        var cfg = new TspRunConfig
        {
            Paradigm = TspParadigm.EvolutionStrategy,
            PopulationSize = 30,
            MaxGenerations = 30,
            GenerationDelayMs = 0,
            RandomSeed = 7
        };
        var evolver = new TspEvolver(problem, cfg);
        var stats = new List<TspGenStats>();
        evolver.OnGeneration += s => stats.Add(s);

        await evolver.RunAsync(CancellationToken.None);

        Assert.Equal(30, stats.Count);
        Assert.True(stats[^1].BestDistance > 0);
    }

    [Fact]
    public void SnapshotFormat_JsonRoundTrip_PreservesData()
    {
        var snap = new TspSnapshotDto
        {
            Name = "TestRun",
            SavedAt = DateTime.UtcNow,
            DatasetLabel = "Ulysses16",
            BestTour = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15],
            BestDistance = 73.99,
            GenerationsRun = 50,
            History =
            [
                new GenRecordDto { Generation = 1, BestDist = 85.2, MeanDist = 92.1 },
                new GenRecordDto { Generation = 50, BestDist = 73.99, MeanDist = 78.5 }
            ]
        };
        var opts = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(snap, opts);
        var restored = JsonSerializer.Deserialize<TspSnapshotDto>(json);
        Assert.NotNull(restored);
        Assert.Equal(snap.BestTour.Length, restored.BestTour.Length);
        Assert.Equal(snap.BestDistance, restored.BestDistance, precision: 2);
        Assert.Equal(snap.History.Count, restored.History.Count);
    }

    [Fact]
    public async Task SnapshotFormat_WriteToTempFile_ReadBack()
    {
        var path = Path.Combine(Path.GetTempPath(), $"NeuroSim_Tsp_{Guid.NewGuid():N}.json");
        try
        {
            var snap = new TspSnapshotDto
            {
                Name = "FileTest",
                BestTour = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15],
                BestDistance = 74.0,
                GenerationsRun = 100
            };
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(snap, new JsonSerializerOptions { WriteIndented = true }));
            var json = await File.ReadAllTextAsync(path);
            var restored = JsonSerializer.Deserialize<TspSnapshotDto>(json);
            Assert.NotNull(restored);
            Assert.Equal(snap.BestDistance, restored.BestDistance, precision: 1);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // DTOs matching the UI snapshot format for compatibility testing
    private sealed class TspSnapshotDto
    {
        public string Name { get; set; } = "";
        public DateTime SavedAt { get; set; }
        public string DatasetLabel { get; set; } = "";
        public int[] BestTour { get; set; } = [];
        public double BestDistance { get; set; }
        public int GenerationsRun { get; set; }
        public List<GenRecordDto> History { get; set; } = [];
    }

    private sealed class GenRecordDto
    {
        public int Generation { get; set; }
        public double BestDist { get; set; }
        public double MeanDist { get; set; }
    }
}
