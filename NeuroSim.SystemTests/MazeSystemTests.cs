// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.SystemTests/MazeSystemTests.cs
using System.Text.Json;
using NeuroSim.Problems.Maze;
using Xunit;

namespace NeuroSim.SystemTests;

/// <summary>
/// Sample System Test for the Maze playground. Need to verify further if this is correct.
/// System tests for Maze playground: full evolution runs, snapshot format.
/// </summary>
public sealed class MazeSystemTests
{
    [Fact]
    public async Task FullRun_GA_100Generations_OnTrivialMaze_ReachesGoal()
    {
        var maze = CreateTrivialCorridor();
        var cfg = new MazeRunConfig
        {
            Paradigm = MazeParadigm.GeneticAlgorithm,
            PopulationSize = 80,
            MaxGenerations = 100,
            PathLength = 20,
            MutationRate = 0.1,
            GenerationDelayMs = 0,
            RandomSeed = 123
        };
        var evolver = new MazeEvolver(maze, cfg);
        bool goalReached = false;
        evolver.OnGeneration += s => { if (s.GoalReached) goalReached = true; };

        await evolver.RunAsync(CancellationToken.None);

        Assert.True(goalReached, "Evolution should find path to goal in trivial corridor");
    }

    [Fact]
    public async Task FullRun_ES_50Generations_OnTrivialMaze_ReachesGoal()
    {
        var maze = CreateTrivialCorridor();
        var cfg = new MazeRunConfig
        {
            Paradigm = MazeParadigm.EvolutionStrategy,
            PopulationSize = 50,
            MaxGenerations = 50,
            PathLength = 15,
            GenerationDelayMs = 0,
            RandomSeed = 42
        };
        var evolver = new MazeEvolver(maze, cfg);
        bool goalReached = false;
        evolver.OnGeneration += s => { if (s.GoalReached) goalReached = true; };

        await evolver.RunAsync(CancellationToken.None);

        Assert.True(goalReached);
    }

    [Fact]
    public async Task FullRun_OnPresetMaze_CompletesWithoutError()
    {
        var maze = MazeDatasets.Get("Classic 15×15");
        var cfg = new MazeRunConfig
        {
            Paradigm = MazeParadigm.GeneticAlgorithm,
            PopulationSize = 40,
            MaxGenerations = 20,
            PathLength = 200,
            GenerationDelayMs = 0,
            RandomSeed = 1
        };
        var evolver = new MazeEvolver(maze, cfg);
        var stats = new List<MazeGenStats>();
        evolver.OnGeneration += s => stats.Add(s);

        await evolver.RunAsync(CancellationToken.None);

        Assert.Equal(20, stats.Count);
        Assert.All(stats, s => Assert.Equal(cfg.PathLength, s.BestGenome.Length));
    }

    [Fact]
    public void SnapshotFormat_JsonRoundTrip_PreservesData()
    {
        var snap = new MazeSnapshotDto
        {
            Name = "MazeTest",
            SavedAt = DateTime.UtcNow,
            MazeLabel = "Classic 15×15",
            BestFitness = 150.5,
            GenerationsRun = 80,
            GoalReached = true,
            StepsToGoal = 42,
            History =
            [
                new MazeGenRecordDto { Generation = 1, BestFitness = 80, MeanFitness = 50 },
                new MazeGenRecordDto { Generation = 80, BestFitness = 150.5, MeanFitness = 120 }
            ]
        };
        var opts = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(snap, opts);
        var restored = JsonSerializer.Deserialize<MazeSnapshotDto>(json);
        Assert.NotNull(restored);
        Assert.Equal(snap.BestFitness, restored.BestFitness, precision: 2);
        Assert.Equal(snap.GoalReached, restored.GoalReached);
        Assert.Equal(snap.History.Count, restored.History.Count);
    }

    [Fact]
    public async Task EditableMaze_SerialiseToTemp_Deserialise_Matches()
    {
        var original = new EditableMaze(10, 10);
        original.Start = (1, 1);
        original.Goal = (8, 8);
        original.ToggleWall(4, 4);
        original.ToggleWall(5, 5);

        var path = Path.Combine(Path.GetTempPath(), $"NeuroSim_Maze_{Guid.NewGuid():N}.txt");
        try
        {
            var serialised = original.Serialise();
            await File.WriteAllTextAsync(path, serialised);
            var loaded = await File.ReadAllTextAsync(path);
            var restored = EditableMaze.Deserialise(loaded);

            Assert.Equal(original.Width, restored.Width);
            Assert.Equal(original.Height, restored.Height);
            Assert.Equal(original.Start, restored.Start);
            Assert.Equal(original.Goal, restored.Goal);
            Assert.Equal(original.IsWall(4, 4), restored.IsWall(4, 4));
            Assert.Equal(original.IsWall(5, 5), restored.IsWall(5, 5));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static MazeProblem CreateTrivialCorridor()
    {
        var walls = new bool[5, 5];
        for (int x = 0; x < 5; x++)
            for (int y = 0; y < 5; y++)
                walls[x, y] = true;
        for (int x = 0; x < 5; x++)
            walls[x, 2] = false; // horizontal corridor
        return new MazeProblem
        {
            Width = 5,
            Height = 5,
            Walls = walls,
            Start = (0, 2),
            Goal = (4, 2)
        };
    }

    private sealed class MazeSnapshotDto
    {
        public string Name { get; set; } = "";
        public DateTime SavedAt { get; set; }
        public string MazeLabel { get; set; } = "";
        public double BestFitness { get; set; }
        public int GenerationsRun { get; set; }
        public bool GoalReached { get; set; }
        public int StepsToGoal { get; set; }
        public List<MazeGenRecordDto> History { get; set; } = [];
    }

    private sealed class MazeGenRecordDto
    {
        public int Generation { get; set; }
        public double BestFitness { get; set; }
        public double MeanFitness { get; set; }
    }
}
