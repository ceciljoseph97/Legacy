// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Problems/TSP/TspEvolver.cs
namespace NeuroSim.Problems.TSP;

/// <summary>
/// Self-contained TSP evolver supporting all 4 EC paradigms.
/// Uses raw int[] permutations for clarity — no generic genome overhead.
/// </summary>
public sealed class TspEvolver
{
    private readonly TspProblem  _problem;
    private readonly TspRunConfig _cfg;
    private readonly Random      _rng;
    private readonly double[,]   _dist;
    private readonly int         _n;

    private int[][]  _pop  = null!;
    private double[] _fit  = null!;   // negative tour lengths (maximise)

    // GP-specific: each individual = weight vector, tour constructed greedily
    private double[][] _weights = null!;

    public event Action<TspGenStats>? OnGeneration;

    public TspEvolver(TspProblem problem, TspRunConfig cfg)
    {
        _problem = problem;
        _cfg  = cfg;
        _rng  = new Random(cfg.RandomSeed);
        _dist = problem.DistanceMatrix;
        _n    = problem.Cities.Length;
    }

    // ── Entry point ───────────────────────────────────────────────────────

    public async Task RunAsync(CancellationToken ct)
    {
        Initialize();
        switch (_cfg.Paradigm)
        {
            case TspParadigm.GeneticAlgorithm:      await RunGAAsync(ct);  break;
            case TspParadigm.EvolutionStrategy:     await RunESAsync(ct);  break;
            case TspParadigm.EvolutionaryProgramming: await RunEPAsync(ct); break;
            case TspParadigm.GeneticProgramming:    await RunGPAsync(ct);  break;
        }
    }

    // ── Initialisation ────────────────────────────────────────────────────

    private void Initialize()
    {
        int mu = _cfg.PopulationSize;
        _pop = new int[mu][];
        _fit = new double[mu];
        for (int i = 0; i < mu; i++)
        {
            _pop[i] = RandomRoute();
            _fit[i] = Fitness(_pop[i]);
        }

        if (_cfg.Paradigm == TspParadigm.GeneticProgramming)
        {
            _weights = new double[mu][];
            for (int i = 0; i < mu; i++) _weights[i] = RandomWeights();
        }
    }

    // ── Delay helper ──────────────────────────────────────────────────────

    private async Task Delay(CancellationToken ct)
    {
        if (_cfg.GenerationDelayMs > 0)
            await Task.Delay(_cfg.GenerationDelayMs, ct);
        else
            await Task.Yield();
    }

    // ── GA ────────────────────────────────────────────────────────────────

    private async Task RunGAAsync(CancellationToken ct)
    {
        for (int g = 1; g <= _cfg.MaxGenerations && !ct.IsCancellationRequested; g++)
        {
            var offspring = new List<int[]>(_cfg.PopulationSize);
            while (offspring.Count < _cfg.PopulationSize - _cfg.EliteCount)
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
                if (offspring.Count < _cfg.PopulationSize - _cfg.EliteCount) offspring.Add(c2);
            }

            // Elitism: keep top E from current generation
            var sorted = Enumerable.Range(0, _pop.Length).OrderByDescending(i => _fit[i]).ToArray();
            var newPop = sorted.Take(_cfg.EliteCount).Select(i => _pop[i]).ToList();
            newPop.AddRange(offspring);

            _pop = newPop.ToArray();
            _fit = _pop.Select(Fitness).ToArray();
            Report(g);
            await Delay(ct);
        }
    }

    // ── ES: (μ+λ) or (μ,λ) ───────────────────────────────────────────────

    private async Task RunESAsync(CancellationToken ct)
    {
        double sigma = 1.0;  // step size (controls mutation frequency)
        int mu = _cfg.PopulationSize;
        int lambda = mu * _cfg.Lambda;

        double[] successHistory = new double[10];
        int histIdx = 0;

        for (int g = 1; g <= _cfg.MaxGenerations && !ct.IsCancellationRequested; g++)
        {
            double prevBest = _fit.Max();

            // Generate λ offspring — ES uses mutation only
            var offspring = new int[lambda][];
            for (int i = 0; i < lambda; i++)
            {
                int parent = _rng.Next(mu);
                offspring[i] = DoMutate((int[])_pop[parent].Clone());
            }
            var offFit = offspring.Select(Fitness).ToArray();

            // Select μ best from parents+offspring (μ+λ) or offspring only (μ,λ)
            IEnumerable<int[]> pool;
            IEnumerable<double> poolFit;
            if (_cfg.EsStrategy == EsStrategy.MuPlusLambda)
            {
                pool    = _pop.Concat(offspring);
                poolFit = _fit.Concat(offFit);
            }
            else
            {
                pool    = offspring;
                poolFit = offFit;
            }

            var ranked = pool.Zip(poolFit, (r, f) => (r, f))
                              .OrderByDescending(t => t.f).Take(mu).ToArray();
            _pop = ranked.Select(t => t.r).ToArray();
            _fit = ranked.Select(t => t.f).ToArray();

            // 1/5 success rule: adapt step size
            double improved = _fit.Max() > prevBest ? 1.0 : 0.0;
            successHistory[histIdx++ % 10] = improved;
            double rate = successHistory.Average();
            if (rate > 0.2) sigma *= 1.22;
            else if (rate < 0.2) sigma *= 0.82;
            sigma = Math.Clamp(sigma, 0.01, 1.0);
            _cfg.MutationRate = Math.Clamp(sigma * 0.3, 0.01, 0.5);

            Report(g);
            await Delay(ct);
        }
    }

    // ── EP ────────────────────────────────────────────────────────────────

    private async Task RunEPAsync(CancellationToken ct)
    {
        int mu = _cfg.PopulationSize;
        for (int g = 1; g <= _cfg.MaxGenerations && !ct.IsCancellationRequested; g++)
        {
            // Each parent → one mutated offspring (no crossover in EP)
            var offspring = _pop.Select(p => DoMutate((int[])p.Clone())).ToArray();
            var offFit    = offspring.Select(Fitness).ToArray();

            // Pool all parents + offspring
            var allRoutes = _pop.Concat(offspring).ToArray();
            var allFit    = _fit.Concat(offFit).ToArray();

            // Tournament between every individual vs q random opponents
            int q = _cfg.EpOpponents;
            var scores = new int[allRoutes.Length];
            for (int i = 0; i < allRoutes.Length; i++)
                for (int k = 0; k < q; k++)
                {
                    int j = _rng.Next(allRoutes.Length);
                    if (allFit[i] > allFit[j]) scores[i]++;
                }

            // Select μ best by tournament wins
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

    // ── GP: evolve greedy-heuristic weights ───────────────────────────────

    private async Task RunGPAsync(CancellationToken ct)
    {
        // Each individual = weight vector w[0..N-1]
        // Tour: from current city, pick argmax( w[j] / dist(cur, j) ) among unvisited
        int mu = _cfg.PopulationSize;
        var wFit = _weights.Select(w => WFitness(w)).ToArray();

        for (int g = 1; g <= _cfg.MaxGenerations && !ct.IsCancellationRequested; g++)
        {
            var offspring = new double[mu][];
            var oFit      = new double[mu];

            for (int i = 0; i < mu; i++)
            {
                // Blend crossover (BLX-α)
                int p1 = WSelect(wFit), p2 = WSelect(wFit);
                offspring[i] = BlendCrossover(_weights[p1], _weights[p2]);
                // Gaussian mutation
                offspring[i] = WMutate(offspring[i]);
                oFit[i]      = WFitness(offspring[i]);
            }

            // Elitism + replace
            var allW   = _weights.Concat(offspring).ToArray();
            var allFit = wFit.Concat(oFit).ToArray();
            var top    = Enumerable.Range(0, allW.Length).OrderByDescending(i => allFit[i]).Take(mu).ToArray();
            _weights = top.Select(i => allW[i]).ToArray();
            wFit     = top.Select(i => allFit[i]).ToArray();

            // Construct best tour from best weights for display
            _pop[0] = GreedyTour(_weights[0]);
            _fit[0] = Fitness(_pop[0]);
            for (int i = 1; i < mu; i++) { _pop[i] = GreedyTour(_weights[i]); _fit[i] = Fitness(_pop[i]); }

            Report(g);
            await Delay(ct);
        }
    }

    // ── Selection operators ───────────────────────────────────────────────

    private int Select() => _cfg.Selection switch
    {
        TspSelectionMethod.Tournament  => TournamentSelect(_fit),
        TspSelectionMethod.RouletteWheel => RouletteSelect(_fit),
        TspSelectionMethod.RankBased   => RankSelect(_fit),
        _ => TournamentSelect(_fit)
    };

    private int TournamentSelect(double[] fit)
    {
        int best = _rng.Next(fit.Length);
        for (int i = 1; i < _cfg.TournamentSize; i++)
        {
            int cand = _rng.Next(fit.Length);
            if (fit[cand] > fit[best]) best = cand;
        }
        return best;
    }

    private int RouletteSelect(double[] fit)
    {
        double min   = fit.Min();
        double total = fit.Sum(f => f - min + 1e-9);
        double r     = _rng.NextDouble() * total;
        double acc   = 0;
        for (int i = 0; i < fit.Length; i++) { acc += fit[i] - min + 1e-9; if (acc >= r) return i; }
        return fit.Length - 1;
    }

    private int RankSelect(double[] fit)
    {
        var ranked = Enumerable.Range(0, fit.Length).OrderBy(i => fit[i]).ToArray();
        double total = ranked.Length * (ranked.Length + 1) / 2.0;
        double r = _rng.NextDouble() * total;
        double acc = 0;
        for (int rank = 0; rank < ranked.Length; rank++)
        {
            acc += rank + 1;
            if (acc >= r) return ranked[rank];
        }
        return ranked[^1];
    }

    private int WSelect(double[] fit) => TournamentSelect(fit);

    // ── Crossover operators ───────────────────────────────────────────────

    private (int[], int[]) DoCrossover(int[] p1, int[] p2) => _cfg.Crossover switch
    {
        TspCrossoverMethod.OX              => OX(p1, p2),
        TspCrossoverMethod.PMX             => PMX(p1, p2),
        TspCrossoverMethod.EdgeRecombination => ERX(p1, p2),
        _ => ((int[])p1.Clone(), (int[])p2.Clone())
    };

    private (int[], int[]) OX(int[] p1, int[] p2)
    {
        int lo = _rng.Next(_n), hi = _rng.Next(lo, _n);
        return (OXChild(p1, p2, lo, hi), OXChild(p2, p1, lo, hi));
    }

    private int[] OXChild(int[] donor, int[] recv, int lo, int hi)
    {
        var child = new int[_n]; Array.Fill(child, -1);
        var inSeg = new HashSet<int>();
        for (int i = lo; i <= hi; i++) { child[i] = donor[i]; inSeg.Add(donor[i]); }
        var q = new Queue<int>(recv.Where(x => !inSeg.Contains(x)));
        for (int i = 0; i < _n; i++) if (child[i] == -1) child[i] = q.Dequeue();
        return child;
    }

    private (int[], int[]) PMX(int[] p1, int[] p2)
    {
        int lo = _rng.Next(_n), hi = _rng.Next(lo, _n);
        return (PMXChild(p1, p2, lo, hi), PMXChild(p2, p1, lo, hi));
    }

    private int[] PMXChild(int[] donor, int[] recv, int lo, int hi)
    {
        var child = new int[_n]; Array.Fill(child, -1);
        for (int i = lo; i <= hi; i++) child[i] = donor[i];
        var donorSeg = new HashSet<int>(donor[lo..(hi + 1)]);
        for (int i = 0; i < _n; i++)
        {
            if (i >= lo && i <= hi) continue;
            int val = recv[i];
            while (donorSeg.Contains(val))
            {
                int pos = Array.IndexOf(donor, val, lo, hi - lo + 1);
                val = recv[pos];
            }
            child[i] = val;
        }
        return child;
    }

    private (int[], int[]) ERX(int[] p1, int[] p2)
    {
        // Edge recombination crossover
        var adj = new HashSet<int>[_n];
        for (int i = 0; i < _n; i++) adj[i] = new HashSet<int>();

        void AddEdges(int[] tour)
        {
            for (int i = 0; i < _n; i++)
            {
                int a = tour[i], b = tour[(i + 1) % _n];
                adj[a].Add(b); adj[b].Add(a);
            }
        }
        AddEdges(p1); AddEdges(p2);

        int[] Build()
        {
            var child = new int[_n];
            var used  = new bool[_n];
            child[0] = p1[0]; used[child[0]] = true;

            for (int i = 1; i < _n; i++)
            {
                var cur  = child[i - 1];
                var cands = adj[cur].Where(c => !used[c]).ToList();

                if (cands.Count == 0)
                {
                    // Pick random unused city
                    for (int k = 0; k < _n; k++) if (!used[k]) { child[i] = k; break; }
                }
                else
                {
                    // Pick neighbour with fewest available adjacencies
                    child[i] = cands.MinBy(c => adj[c].Count(x => !used[x]));
                }
                used[child[i]] = true;
            }
            return child;
        }

        return (Build(), Build());
    }

    // ── Mutation operators ────────────────────────────────────────────────

    private int[] DoMutate(int[] route) => _cfg.Mutation switch
    {
        TspMutationMethod.TwoOpt   => TwoOpt(route),
        TspMutationMethod.Swap     => SwapMut(route),
        TspMutationMethod.Relocate => Relocate(route),
        TspMutationMethod.OrOpt    => OrOpt(route),
        _ => TwoOpt(route)
    };

    private int[] TwoOpt(int[] r)
    {
        if (_rng.NextDouble() >= _cfg.MutationRate) return r;
        var res = (int[])r.Clone();
        int i = _rng.Next(_n), j = _rng.Next(i + 1, Math.Min(i + _n / 2, _n));
        Array.Reverse(res, i, j - i);
        return res;
    }

    private int[] SwapMut(int[] r)
    {
        var res = (int[])r.Clone();
        for (int i = 0; i < _n; i++)
            if (_rng.NextDouble() < _cfg.MutationRate)
            {
                int j = _rng.Next(_n);
                (res[i], res[j]) = (res[j], res[i]);
            }
        return res;
    }

    private int[] Relocate(int[] r)
    {
        if (_rng.NextDouble() >= _cfg.MutationRate) return r;
        var list = new List<int>(r);
        int from = _rng.Next(_n);
        int city = list[from];
        list.RemoveAt(from);
        int to = _rng.Next(_n - 1);
        list.Insert(to, city);
        return list.ToArray();
    }

    private int[] OrOpt(int[] r)
    {
        // Move a chain of 2-3 cities
        if (_rng.NextDouble() >= _cfg.MutationRate) return r;
        var res  = (int[])r.Clone();
        int len  = _rng.Next(2, 4);
        int from = _rng.Next(_n - len);
        var seg  = res.Skip(from).Take(len).ToArray();
        var rest = res.Take(from).Concat(res.Skip(from + len)).ToList();
        int ins  = _rng.Next(rest.Count + 1);
        rest.InsertRange(ins, seg);
        return rest.ToArray();
    }

    // ── GP helpers ────────────────────────────────────────────────────────

    private double[] RandomWeights()
    {
        var w = new double[_n];
        for (int i = 0; i < _n; i++) w[i] = _rng.NextDouble() * 2;
        return w;
    }

    private double[] BlendCrossover(double[] a, double[] b)
    {
        const double alpha = 0.3;
        var child = new double[_n];
        for (int i = 0; i < _n; i++)
        {
            double lo = Math.Min(a[i], b[i]) - alpha * Math.Abs(a[i] - b[i]);
            double hi = Math.Max(a[i], b[i]) + alpha * Math.Abs(a[i] - b[i]);
            child[i] = lo + _rng.NextDouble() * (hi - lo);
        }
        return child;
    }

    private double[] WMutate(double[] w)
    {
        var res = (double[])w.Clone();
        for (int i = 0; i < _n; i++)
            if (_rng.NextDouble() < _cfg.MutationRate)
                res[i] = Math.Max(0.01, res[i] + (_rng.NextDouble() - 0.5) * 0.4);
        return res;
    }

    private double WFitness(double[] w) => -_problem.RouteLength(GreedyTour(w));

    private int[] GreedyTour(double[] w)
    {
        var visited = new bool[_n];
        var tour = new int[_n];
        tour[0] = 0; visited[0] = true;

        for (int step = 1; step < _n; step++)
        {
            int cur = tour[step - 1];
            double bestScore = double.NegativeInfinity;
            int bestCity = -1;

            for (int j = 0; j < _n; j++)
            {
                if (visited[j]) continue;
                double d = _dist[cur, j];
                double score = d < 1e-9 ? w[j] * 1e9 : w[j] / d;
                if (score > bestScore) { bestScore = score; bestCity = j; }
            }
            tour[step] = bestCity;
            visited[bestCity] = true;
        }
        return tour;
    }

    // ── Utilities ─────────────────────────────────────────────────────────

    private double Fitness(int[] route) => -_problem.RouteLength(route);

    private int[] RandomRoute()
    {
        var r = Enumerable.Range(0, _n).ToArray();
        for (int i = _n - 1; i > 0; i--) { int j = _rng.Next(i + 1); (r[i], r[j]) = (r[j], r[i]); }
        return r;
    }

    private void Report(int gen)
    {
        double bestDist  = -_fit.Max();
        double meanDist  = -_fit.Average();
        double worstDist = -_fit.Min();

        double variance  = _fit.Average(f => Math.Pow(-f - meanDist, 2));
        double diversity = Math.Sqrt(variance) / (meanDist < 1e-9 ? 1 : meanDist);

        int bestIdx = Array.IndexOf(_fit, _fit.Max());
        int[] bestTour = (int[])_pop[bestIdx].Clone();

        OnGeneration?.Invoke(new TspGenStats(gen, bestDist, meanDist, worstDist, diversity, bestTour));
    }
}
