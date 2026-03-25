// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Problems/Maze/MazeEvolver.cs
namespace NeuroSim.Problems.Maze;

/// <summary>
/// Maze solver supporting GA, ES, and EP paradigms.
/// Genome = int[] direction sequence (0=N 1=E 2=S 3=W), length = PathLength.
///
/// Operator design notes:
///   Maze paths are sequence problems — the ORDER of moves matters.
///   Random point-mutation works for exploration but destroys corridor runs.
///   Inversion  (reverse a sub-sequence) is the best default: it preserves
///     both ends of the path while restructuring the middle.
///   SegmentShuffle and BlockReset are useful when the population is stuck.
///   Uniform crossover outperforms two-point for this encoding because it
///     doesn't create a hard "break point" that severs a working corridor run.
/// </summary>
public sealed class MazeEvolver
{
    private readonly MazeProblem   _maze;
    private readonly MazeRunConfig _cfg;
    private readonly Random        _rng;

    private int[][]  _pop = null!;
    private double[] _fit = null!;

    public event Action<MazeGenStats>? OnGeneration;

    public MazeEvolver(MazeProblem maze, MazeRunConfig cfg)
    {
        _maze = maze;
        _cfg  = cfg;
        _rng  = new Random(cfg.RandomSeed);
    }

    // ── Entry point ───────────────────────────────────────────────────────

    public async Task RunAsync(CancellationToken ct)
    {
        Initialise();
        switch (_cfg.Paradigm)
        {
            case MazeParadigm.GeneticAlgorithm:        await RunGAAsync(ct); break;
            case MazeParadigm.EvolutionStrategy:        await RunESAsync(ct); break;
            case MazeParadigm.EvolutionaryProgramming:  await RunEPAsync(ct); break;
        }
    }

    // ── Initialisation ────────────────────────────────────────────────────

    private void Initialise()
    {
        _pop = new int[_cfg.PopulationSize][];
        for (int i = 0; i < _cfg.PopulationSize; i++)
            _pop[i] = RandomPath();
        _fit = _pop.Select(Fitness).ToArray();
    }

    // ── GA ────────────────────────────────────────────────────────────────

    private async Task RunGAAsync(CancellationToken ct)
    {
        int mu = _cfg.PopulationSize;
        for (int g = 1; g <= _cfg.MaxGenerations && !ct.IsCancellationRequested; g++)
        {
            var offspring = new List<int[]>(mu);
            while (offspring.Count < mu - _cfg.EliteCount)
            {
                int p1 = Select(), p2 = Select();
                int[] c1, c2;
                if (_rng.NextDouble() < _cfg.CrossoverRate)
                    (c1, c2) = DoCrossover(_pop[p1], _pop[p2]);
                else
                    (c1, c2) = ((int[])_pop[p1].Clone(), (int[])_pop[p2].Clone());

                c1 = DoMutate(c1);
                c2 = DoMutate(c2);
                offspring.Add(c1);
                if (offspring.Count < mu - _cfg.EliteCount) offspring.Add(c2);
            }

            // Elitism
            var eliteIdx = Enumerable.Range(0, mu).OrderByDescending(i => _fit[i])
                                     .Take(_cfg.EliteCount).ToArray();
            var newPop = eliteIdx.Select(i => _pop[i]).ToList();
            newPop.AddRange(offspring);

            _pop = newPop.ToArray();
            _fit = _pop.Select(Fitness).ToArray();
            Report(g);
            await Delay(ct);
        }
    }

    // ── ES: (μ+λ) or (μ,λ) with self-adaptive mutation ───────────────────

    private async Task RunESAsync(CancellationToken ct)
    {
        int mu     = _cfg.PopulationSize;
        int lambda = mu * _cfg.Lambda;
        double sigma = _cfg.MutationRate;

        double[] successHistory = new double[10];
        int histIdx = 0;

        for (int g = 1; g <= _cfg.MaxGenerations && !ct.IsCancellationRequested; g++)
        {
            double prevBest = _fit.Max();

            // Generate λ offspring from random parents — mutation-only for ES
            var offspring = new int[lambda][];
            for (int i = 0; i < lambda; i++)
            {
                int parent = _rng.Next(mu);
                offspring[i] = DoMutate((int[])_pop[parent].Clone());
            }
            var offFit = offspring.Select(Fitness).ToArray();

            // Pool selection
            IEnumerable<int[]>  pool    = _cfg.EsStrategy == MazeEsStrategy.MuPlusLambda
                ? _pop.Concat(offspring) : offspring;
            IEnumerable<double> poolFit = _cfg.EsStrategy == MazeEsStrategy.MuPlusLambda
                ? _fit.Concat(offFit)    : offFit;

            var ranked = pool.Zip(poolFit, (r, f) => (r, f))
                              .OrderByDescending(t => t.f).Take(mu).ToArray();
            _pop = ranked.Select(t => t.r).ToArray();
            _fit = ranked.Select(t => t.f).ToArray();

            // 1/5 success rule: adapt sigma
            double improved = _fit.Max() > prevBest ? 1.0 : 0.0;
            successHistory[histIdx++ % 10] = improved;
            double rate = successHistory.Average();
            sigma = rate > 0.2 ? sigma * 1.22 : sigma * 0.82;
            sigma = Math.Clamp(sigma, 0.01, 0.5);
            _cfg.MutationRate = sigma;

            Report(g);
            await Delay(ct);
        }
    }

    // ── EP: each parent spawns one offspring, tournament among combined pool

    private async Task RunEPAsync(CancellationToken ct)
    {
        int mu = _cfg.PopulationSize;
        for (int g = 1; g <= _cfg.MaxGenerations && !ct.IsCancellationRequested; g++)
        {
            var offspring = _pop.Select(p => DoMutate((int[])p.Clone())).ToArray();
            var offFit    = offspring.Select(Fitness).ToArray();

            var allRoutes = _pop.Concat(offspring).ToArray();
            var allFit    = _fit.Concat(offFit).ToArray();

            // Tournament: each individual vs q random opponents
            int q      = _cfg.EpOpponents;
            var scores = new int[allRoutes.Length];
            for (int i = 0; i < allRoutes.Length; i++)
                for (int k = 0; k < q; k++)
                {
                    int j = _rng.Next(allRoutes.Length);
                    if (allFit[i] > allFit[j]) scores[i]++;
                }

            var ranked = Enumerable.Range(0, allRoutes.Length)
                                   .OrderByDescending(i => scores[i])
                                   .ThenByDescending(i => allFit[i])
                                   .Take(mu).ToArray();
            _pop = ranked.Select(i => allRoutes[i]).ToArray();
            _fit = ranked.Select(i => allFit[i]).ToArray();

            Report(g);
            await Delay(ct);
        }
    }

    // ── Selection operators ───────────────────────────────────────────────

    private int Select() => _cfg.Selection switch
    {
        MazeSelectionMethod.Tournament   => TournamentSelect(),
        MazeSelectionMethod.RouletteWheel => RouletteSelect(),
        MazeSelectionMethod.RankBased    => RankSelect(),
        _                                => TournamentSelect()
    };

    private int TournamentSelect()
    {
        int best = _rng.Next(_pop.Length);
        for (int k = 1; k < _cfg.TournamentSize; k++)
        {
            int c = _rng.Next(_pop.Length);
            if (_fit[c] > _fit[best]) best = c;
        }
        return best;
    }

    private int RouletteSelect()
    {
        double min  = _fit.Min();
        double[] shifted = _fit.Select(f => f - min + 1e-6).ToArray();
        double total = shifted.Sum();
        double r = _rng.NextDouble() * total;
        double cum = 0;
        for (int i = 0; i < shifted.Length; i++)
        {
            cum += shifted[i];
            if (cum >= r) return i;
        }
        return _pop.Length - 1;
    }

    private int RankSelect()
    {
        int n    = _pop.Length;
        var rank = Enumerable.Range(0, n).OrderBy(i => _fit[i]).ToArray();
        double total = n * (n + 1) / 2.0;
        double r = _rng.NextDouble() * total;
        double cum = 0;
        for (int i = 0; i < n; i++)
        {
            cum += i + 1;
            if (cum >= r) return rank[i];
        }
        return rank[n - 1];
    }

    // ── Crossover operators ───────────────────────────────────────────────

    private (int[] c1, int[] c2) DoCrossover(int[] a, int[] b) => _cfg.Crossover switch
    {
        MazeCrossoverMethod.TwoPoint   => TwoPointCrossover(a, b),
        MazeCrossoverMethod.SinglePoint => SinglePointCrossover(a, b),
        MazeCrossoverMethod.Uniform    => UniformCrossover(a, b),
        _                              => TwoPointCrossover(a, b)
    };

    private (int[] c1, int[] c2) TwoPointCrossover(int[] a, int[] b)
    {
        int n = a.Length;
        int p1 = _rng.Next(n), p2 = _rng.Next(n);
        if (p1 > p2) (p1, p2) = (p2, p1);
        var c1 = (int[])a.Clone(); var c2 = (int[])b.Clone();
        for (int i = p1; i <= p2; i++) { c1[i] = b[i]; c2[i] = a[i]; }
        return (c1, c2);
    }

    private (int[] c1, int[] c2) SinglePointCrossover(int[] a, int[] b)
    {
        int p = _rng.Next(a.Length);
        var c1 = new int[a.Length]; var c2 = new int[a.Length];
        for (int i = 0; i < a.Length; i++) { c1[i] = i < p ? a[i] : b[i]; c2[i] = i < p ? b[i] : a[i]; }
        return (c1, c2);
    }

    private (int[] c1, int[] c2) UniformCrossover(int[] a, int[] b)
    {
        // Each gene independently picked from either parent — less destructive for corridors
        var c1 = new int[a.Length]; var c2 = new int[a.Length];
        for (int i = 0; i < a.Length; i++)
        {
            if (_rng.NextDouble() < 0.5) { c1[i] = a[i]; c2[i] = b[i]; }
            else                          { c1[i] = b[i]; c2[i] = a[i]; }
        }
        return (c1, c2);
    }

    // ── Mutation operators ────────────────────────────────────────────────

    private int[] DoMutate(int[] path) => _cfg.Mutation switch
    {
        MazeMutationMethod.PointMutation  => PointMutation(path),
        MazeMutationMethod.Inversion      => InversionMutation(path),
        MazeMutationMethod.SegmentShuffle => SegmentShuffleMutation(path),
        MazeMutationMethod.BlockReset     => BlockResetMutation(path),
        _                                 => InversionMutation(path)
    };

    /// <summary>
    /// Replace individual genes randomly.
    /// Good for broad exploration, poor at preserving corridor sequences.
    /// </summary>
    private int[] PointMutation(int[] path)
    {
        var m = (int[])path.Clone();
        for (int i = 0; i < m.Length; i++)
            if (_rng.NextDouble() < _cfg.MutationRate)
                m[i] = _rng.Next(4);
        return m;
    }

    /// <summary>
    /// Reverse a random sub-sequence.
    /// Preserves both the prefix and suffix of the path while restructuring the middle.
    /// Best operator for maze — similar role to 2-opt in TSP.
    /// </summary>
    private int[] InversionMutation(int[] path)
    {
        var m  = (int[])path.Clone();
        int segLen = Math.Max(2, (int)(path.Length * _cfg.MutationRate * 10));
        segLen = Math.Clamp(segLen, 2, path.Length / 2);
        int start  = _rng.Next(path.Length - segLen);
        int end    = start + segLen;
        Array.Reverse(m, start, end - start);
        return m;
    }

    /// <summary>
    /// Randomly shuffle moves within a sub-sequence.
    /// Useful for escaping plateaus — breaks stuck sub-paths without losing good moves.
    /// </summary>
    private int[] SegmentShuffleMutation(int[] path)
    {
        var m      = (int[])path.Clone();
        int segLen = Math.Max(2, (int)(path.Length * _cfg.MutationRate * 12));
        segLen = Math.Clamp(segLen, 2, path.Length / 2);
        int start  = _rng.Next(path.Length - segLen);
        // Fisher-Yates on the sub-segment
        for (int i = segLen - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (m[start + i], m[start + j]) = (m[start + j], m[start + i]);
        }
        return m;
    }

    /// <summary>
    /// Replace a contiguous block with random directions.
    /// Aggressive diversifier — helps escape deep local optima.
    /// </summary>
    private int[] BlockResetMutation(int[] path)
    {
        var m      = (int[])path.Clone();
        int blockLen = Math.Max(4, (int)(path.Length * _cfg.MutationRate * 15));
        blockLen = Math.Clamp(blockLen, 4, path.Length / 3);
        int start    = _rng.Next(path.Length - blockLen);
        for (int i = start; i < start + blockLen; i++)
            m[i] = _rng.Next(4);
        return m;
    }

    // ── Fitness ───────────────────────────────────────────────────────────
    // Goal reached  →  1000 - steps * 0.5  (fewer steps = better)
    // Not reached   →  -euclidean_dist - wallHits * 0.05

    private double Fitness(int[] path)
    {
        var (fx, fy, wallHits, goalReached, steps, _) = Simulate(path);
        if (goalReached) return 1000.0 - steps * 0.5;
        double dx = fx - _maze.Goal.X, dy = fy - _maze.Goal.Y;
        return -Math.Sqrt(dx * dx + dy * dy) - wallHits * 0.05;
    }

    // ── Simulation ────────────────────────────────────────────────────────

    private (int FX, int FY, int WallHits, bool GoalReached, int Steps, (int X, int Y)[] Path)
        Simulate(int[] path)
    {
        int x = _maze.Start.X, y = _maze.Start.Y;
        int wallHits = 0;
        bool reached = false;
        int steps    = path.Length;

        int[] dx = { 0, 1, 0, -1 };   // N E S W
        int[] dy = { -1, 0, 1, 0 };

        var positions = new List<(int, int)>(path.Length + 1) { (x, y) };

        for (int i = 0; i < path.Length; i++)
        {
            int nx = x + dx[path[i]], ny = y + dy[path[i]];
            if (_maze.IsWall(nx, ny)) wallHits++;
            else { x = nx; y = ny; }

            positions.Add((x, y));

            if (x == _maze.Goal.X && y == _maze.Goal.Y)
            {
                reached = true;
                steps   = i + 1;
                break;
            }
        }
        return (x, y, wallHits, reached, steps, positions.ToArray());
    }

    // ── Reporting ─────────────────────────────────────────────────────────

    private void Report(int gen)
    {
        double bestF = _fit.Max();
        double meanF = _fit.Average();
        int bestIdx  = Array.IndexOf(_fit, bestF);
        var (_, _, _, goalReached, steps, bestPath) = Simulate(_pop[bestIdx]);
        OnGeneration?.Invoke(new MazeGenStats(
            gen, bestF, meanF, goalReached,
            goalReached ? steps : -1,
            bestPath,
            (int[])_pop[bestIdx].Clone()));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private int[] RandomPath()
    {
        var p = new int[_cfg.PathLength];
        for (int i = 0; i < p.Length; i++) p[i] = _rng.Next(4);
        return p;
    }

    private async Task Delay(CancellationToken ct)
    {
        if (_cfg.GenerationDelayMs > 0)
            await Task.Delay(_cfg.GenerationDelayMs, ct);
        else
            await Task.Yield();
    }
}
