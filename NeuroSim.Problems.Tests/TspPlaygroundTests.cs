// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Problems.Tests/TspPlaygroundTests.cs
using NeuroSim.Problems.TSP;
using Xunit;

namespace NeuroSim.Problems.Tests;

public sealed class TspPlaygroundTests
{
    [Fact]
    public void TspProblem_Euclidean_ReturnsCorrectDistance()
    {
        var a = new TspCity("A", 0, 0);
        var b = new TspCity("B", 3, 4);
        Assert.Equal(5.0, TspProblem.Euclidean(a, b), precision: 4);
    }

    [Fact]
    public void TspProblem_RouteLength_IdentityPermutation()
    {
        var problem = new TspProblem
        {
            Cities =
            [
                new TspCity("A", 0, 0),
                new TspCity("B", 1, 0),
                new TspCity("C", 1, 1),
                new TspCity("D", 0, 1)
            ]
        };
        var route = new[] { 0, 1, 2, 3 };
        double len = problem.RouteLength(route);
        Assert.True(len > 0);
        Assert.Equal(4.0, len, precision: 2); // square perimeter
    }

    [Fact]
    public void TspProblem_DistanceMatrix_IsSymmetric()
    {
        var problem = TspDatasets.Ulysses16;
        var d = problem.DistanceMatrix;
        int n = problem.Cities.Length;
        for (int i = 0; i < n; i++)
        {
            Assert.Equal(0, d[i, i], precision: 6);
            for (int j = i + 1; j < n; j++)
                Assert.Equal(d[i, j], d[j, i], precision: 6);
        }
    }

    [Fact]
    public async Task TspEvolver_GA_RunsAndFiresOnGeneration()
    {
        var problem = TspDatasets.Ulysses16;
        var cfg = new TspRunConfig
        {
            Paradigm = TspParadigm.GeneticAlgorithm,
            PopulationSize = 20,
            MaxGenerations = 5,
            GenerationDelayMs = 0,
            RandomSeed = 123
        };
        var evolver = new TspEvolver(problem, cfg);

        var stats = new List<TspGenStats>();
        evolver.OnGeneration += s => stats.Add(s);

        await evolver.RunAsync(CancellationToken.None);

        Assert.Equal(5, stats.Count);
        Assert.All(stats, s => Assert.Equal(problem.Cities.Length, s.BestTour.Length));
        Assert.All(stats, s => Assert.True(s.BestDistance > 0));
    }

    [Fact]
    public async Task TspEvolver_DeterministicWithSeed_SameFirstGenStats()
    {
        var problem = TspDatasets.Ulysses16;
        var cfg = new TspRunConfig
        {
            Paradigm = TspParadigm.GeneticAlgorithm,
            PopulationSize = 30,
            MaxGenerations = 2,
            GenerationDelayMs = 0,
            RandomSeed = 999
        };
        var run1 = new List<TspGenStats>();
        var run2 = new List<TspGenStats>();
        {
            var e1 = new TspEvolver(problem, cfg);
            e1.OnGeneration += s => run1.Add(s);
            await e1.RunAsync(CancellationToken.None);
        }
        {
            var e2 = new TspEvolver(problem, cfg);
            e2.OnGeneration += s => run2.Add(s);
            await e2.RunAsync(CancellationToken.None);
        }
        Assert.Equal(run1[0].BestDistance, run2[0].BestDistance, precision: 6);
        Assert.Equal(run1[0].MeanDistance, run2[0].MeanDistance, precision: 6);
    }

    [Fact]
    public async Task TspEvolver_ES_RunsAndReports()
    {
        var problem = TspDatasets.Ulysses16;
        var cfg = new TspRunConfig
        {
            Paradigm = TspParadigm.EvolutionStrategy,
            PopulationSize = 15,
            MaxGenerations = 3,
            GenerationDelayMs = 0,
            RandomSeed = 42
        };
        var evolver = new TspEvolver(problem, cfg);
        var stats = new List<TspGenStats>();
        evolver.OnGeneration += s => stats.Add(s);

        await evolver.RunAsync(CancellationToken.None);

        Assert.Equal(3, stats.Count);
        Assert.All(stats, s => Assert.True(s.BestTour.Length == problem.Cities.Length));
    }

    [Fact]
    public async Task TspEvolver_GP_RunsAndReports()
    {
        var problem = TspDatasets.Ulysses16;
        var cfg = new TspRunConfig
        {
            Paradigm = TspParadigm.GeneticProgramming,
            PopulationSize = 12,
            MaxGenerations = 2,
            GenerationDelayMs = 0,
            RandomSeed = 7
        };
        var evolver = new TspEvolver(problem, cfg);
        var stats = new List<TspGenStats>();
        evolver.OnGeneration += s => stats.Add(s);

        await evolver.RunAsync(CancellationToken.None);

        Assert.Equal(2, stats.Count);
    }
}
