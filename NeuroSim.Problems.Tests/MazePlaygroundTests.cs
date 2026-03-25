// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Problems.Tests/MazePlaygroundTests.cs
using NeuroSim.Problems.Maze;
using Xunit;

namespace NeuroSim.Problems.Tests;

public sealed class MazePlaygroundTests
{
    [Fact]
    public void MazeProblem_IsWall_OutOfBoundsReturnsTrue()
    {
        var maze = CreateSimpleMaze();
        Assert.True(maze.IsWall(-1, 0));
        Assert.True(maze.IsWall(0, -1));
        Assert.True(maze.IsWall(5, 0));
        Assert.True(maze.IsWall(0, 5));
    }

    [Fact]
    public void MazeDatasets_Generate_ProducesValidMaze()
    {
        var maze = MazeDatasets.Generate("Test", 15, 15, seed: 42);
        Assert.Equal(15, maze.Width);
        Assert.Equal(15, maze.Height);
        Assert.False(maze.IsWall(maze.Start.X, maze.Start.Y));
        Assert.False(maze.IsWall(maze.Goal.X, maze.Goal.Y));
    }

    [Fact]
    public void EditableMaze_SerialiseDeserialise_RoundTrip()
    {
        var original = new EditableMaze(10, 10);
        original.Start = (1, 1);
        original.Goal = (8, 8);
        original.ToggleWall(5, 5);

        var serialised = original.Serialise();
        var restored = EditableMaze.Deserialise(serialised);

        Assert.Equal(original.Width, restored.Width);
        Assert.Equal(original.Height, restored.Height);
        Assert.Equal(original.Start, restored.Start);
        Assert.Equal(original.Goal, restored.Goal);
        Assert.Equal(original.IsWall(5, 5), restored.IsWall(5, 5));
    }

    [Fact]
    public void EditableMaze_ToMazeProblem_PreservesStartGoal()
    {
        var editable = new EditableMaze(12, 12);
        editable.Start = (2, 3);
        editable.Goal = (9, 10);

        var problem = editable.ToMazeProblem();
        Assert.Equal((2, 3), problem.Start);
        Assert.Equal((9, 10), problem.Goal);
        Assert.Equal(12, problem.Width);
        Assert.Equal(12, problem.Height);
    }

    [Fact]
    public async Task MazeEvolver_GA_RunsAndFiresOnGeneration()
    {
        var maze = MazeDatasets.Get("Classic 15×15");
        var cfg = new MazeRunConfig
        {
            Paradigm = MazeParadigm.GeneticAlgorithm,
            PopulationSize = 20,
            MaxGenerations = 5,
            PathLength = 100,
            GenerationDelayMs = 0,
            RandomSeed = 42
        };
        var evolver = new MazeEvolver(maze, cfg);

        var stats = new List<MazeGenStats>();
        evolver.OnGeneration += s => stats.Add(s);

        await evolver.RunAsync(CancellationToken.None);

        Assert.Equal(5, stats.Count);
        Assert.All(stats, s => Assert.Equal(cfg.PathLength, s.BestGenome.Length));
        Assert.All(stats, s => Assert.NotNull(s.BestPath));
    }

    [Fact]
    public async Task MazeEvolver_ES_RunsAndReports()
    {
        var maze = MazeDatasets.Get("Classic 15×15");
        var cfg = new MazeRunConfig
        {
            Paradigm = MazeParadigm.EvolutionStrategy,
            PopulationSize = 15,
            MaxGenerations = 3,
            PathLength = 80,
            GenerationDelayMs = 0,
            RandomSeed = 7
        };
        var evolver = new MazeEvolver(maze, cfg);
        var stats = new List<MazeGenStats>();
        evolver.OnGeneration += s => stats.Add(s);

        await evolver.RunAsync(CancellationToken.None);

        Assert.Equal(3, stats.Count);
        Assert.All(stats, s => Assert.True(s.BestGenome.Length > 0));
    }

    [Fact]
    public async Task MazeEvolver_EP_RunsAndReports()
    {
        var maze = MazeDatasets.Get("Classic 15×15");
        var cfg = new MazeRunConfig
        {
            Paradigm = MazeParadigm.EvolutionaryProgramming,
            PopulationSize = 12,
            MaxGenerations = 2,
            PathLength = 50,
            GenerationDelayMs = 0,
            RandomSeed = 123
        };
        var evolver = new MazeEvolver(maze, cfg);
        var stats = new List<MazeGenStats>();
        evolver.OnGeneration += s => stats.Add(s);

        await evolver.RunAsync(CancellationToken.None);

        Assert.Equal(2, stats.Count);
    }

    [Fact]
    public async Task MazeEvolver_SimpleCorridor_CanReachGoal()
    {
        // Minimal 3x3 maze: start (0,1), goal (2,1), no walls in between
        var walls = new bool[3, 3];
        walls[1, 0] = true;
        walls[1, 2] = true;
        var maze = new MazeProblem
        {
            Width = 3,
            Height = 3,
            Walls = walls,
            Start = (0, 1),
            Goal = (2, 1)
        };
        var cfg = new MazeRunConfig
        {
            Paradigm = MazeParadigm.GeneticAlgorithm,
            PopulationSize = 50,
            MaxGenerations = 100,
            PathLength = 10,
            MutationRate = 0.2,
            GenerationDelayMs = 0,
            RandomSeed = 1
        };
        var evolver = new MazeEvolver(maze, cfg);
        bool goalReached = false;
        evolver.OnGeneration += s => { if (s.GoalReached) goalReached = true; };

        await evolver.RunAsync(CancellationToken.None);

        Assert.True(goalReached, "Evolution should find a path to goal in simple corridor");
    }

    private static MazeProblem CreateSimpleMaze()
    {
        var walls = new bool[5, 5];
        return new MazeProblem
        {
            Width = 5,
            Height = 5,
            Walls = walls,
            Start = (1, 1),
            Goal = (3, 3)
        };
    }
}
