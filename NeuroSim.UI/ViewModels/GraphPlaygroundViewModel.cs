// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/ViewModels/GraphPlaygroundViewModel.cs
using System.Collections.ObjectModel;
using System.Windows;
using NeuroSim.Engine.Engine;
using NeuroSim.Engine.FitnessEvaluators;
using NeuroSim.Engine.Genomes;
using NeuroSim.Engine.Operators;
using NeuroSim.Engine.Operators.Crossover;
using NeuroSim.Engine.Operators.Mutation;
using NeuroSim.Engine.Operators.Selection;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace NeuroSim.UI.ViewModels;

// ── Graph node/edge data models ───────────────────────────────────────────────

public sealed class GraphNodeVm : ViewModelBase
{
    private double _x, _y;
    private string _label;
    public int Id { get; }
    public string Label { get => _label; set => Set(ref _label, value); }
    public double X { get => _x; set => Set(ref _x, value); }
    public double Y { get => _y; set => Set(ref _y, value); }

    public GraphNodeVm(int id, double x, double y)
    {
        Id = id; _label = id.ToString(); _x = x; _y = y;
    }
}

public sealed class GraphEdgeVm
{
    public int From { get; init; }
    public int To   { get; init; }
    public bool IsEvolved { get; init; }  // true = from GA best, false = user-drawn
}

// ── Fitness evaluator for DAG max-edge problem ────────────────────────────────

public sealed class DagMaxEdgeFitness : IFitnessEvaluator<GraphGenome>
{
    public string Name        => "DAG Max Edges";
    public string Description => "Maximise edges in a DAG (directed, acyclic). Penalise cycles.";

    public double Evaluate(GraphGenome g)
    {
        int n = g.NodeCount;
        int edgeCount = 0;
        bool hasCycle = false;

        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                if (i != j && g.HasEdge(i, j)) edgeCount++;

        // Simple DAG check: if edge (i,j) and edge (j,i) both exist → cycle
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
                if (g.HasEdge(i, j) && g.HasEdge(j, i)) { hasCycle = true; break; }

        // Penalise cycles heavily
        return edgeCount - (hasCycle ? n * 10 : 0);
    }
}

// ── Main ViewModel ────────────────────────────────────────────────────────────

public sealed class GraphPlaygroundViewModel : ViewModelBase
{
    private readonly NavigationService _nav;
    private int _nextNodeId = 0;

    // ── Graph structure (user-drawn) ──────────────────────────────────────
    public ObservableCollection<GraphNodeVm> Nodes { get; } = new();
    public ObservableCollection<GraphEdgeVm> Edges { get; } = new();

    // ── Evolved best graph ────────────────────────────────────────────────
    public ObservableCollection<GraphEdgeVm> EvolvedEdges { get; } = new();

    // ── GA Config ─────────────────────────────────────────────────────────
    private int _popSize = 60;
    public int PopulationSize { get => _popSize; set => Set(ref _popSize, Math.Clamp(value, 10, 500)); }

    private int _maxGen = 200;
    public int MaxGenerations { get => _maxGen; set => Set(ref _maxGen, Math.Clamp(value, 10, 5000)); }

    private double _mutRate = 0.1;
    public double MutationRate { get => _mutRate; set => Set(ref _mutRate, Math.Clamp(value, 0, 0.5)); }

    // ── State ─────────────────────────────────────────────────────────────
    private bool _isRunning;
    public bool IsRunning    { get => _isRunning; set { Set(ref _isRunning, value); Notify(nameof(IsEditable)); Notify(nameof(IsNotRunning)); } }
    public bool IsNotRunning => !_isRunning;
    public bool IsEditable   => !_isRunning;

    private string _status = "Click canvas to add nodes. Drag from node to node to add a directed edge.";
    public string Status { get => _status; set => Set(ref _status, value); }

    private string _modeLabel = "MODE: ADD NODE";
    public string ModeLabel { get => _modeLabel; set => Set(ref _modeLabel, value); }

    private bool _addEdgeMode;
    public bool AddEdgeMode
    {
        get => _addEdgeMode;
        set
        {
            Set(ref _addEdgeMode, value);
            ModeLabel = value ? "MODE: ADD EDGE  (drag between nodes)" : "MODE: ADD NODE  (click canvas)";
        }
    }

    private int _currentGen;
    public int CurrentGeneration { get => _currentGen; set => Set(ref _currentGen, value); }

    private string _bestScore = "–";
    public string BestScore { get => _bestScore; set => Set(ref _bestScore, value); }

    private string _edgeInfo = "–  /  –";
    public string EdgeInfo { get => _edgeInfo; set => Set(ref _edgeInfo, value); }

    // ── Commands ──────────────────────────────────────────────────────────
    public RelayCommand RunCommand     { get; }
    public RelayCommand StopCommand    { get; }
    public RelayCommand ResetCommand   { get; }
    public RelayCommand ClearCommand   { get; }
    public RelayCommand ToggleModeCommand { get; }

    private CancellationTokenSource? _cts;
    private GAEngineBase? _engine;

    // ── Plot ──────────────────────────────────────────────────────────────
    public PlotModel FitnessPlot { get; } = BuildPlot();
    private LineSeries _bestSeries = null!;

    public GraphPlaygroundViewModel(NavigationService nav)
    {
        _nav = nav;
        RunCommand        = new RelayCommand(StartRun,  () => Nodes.Count >= 2 && !IsRunning);
        StopCommand       = new RelayCommand(() => _cts?.Cancel(), () => IsRunning);
        ResetCommand      = new RelayCommand(Reset, () => !IsRunning);
        ClearCommand      = new RelayCommand(ClearAll, () => !IsRunning);
        ToggleModeCommand = new RelayCommand(() => AddEdgeMode = !AddEdgeMode);
        _bestSeries       = (LineSeries)FitnessPlot.Series[0];

        // Seed with a small example graph
        LoadExample();
    }

    // ── Node / edge mutations (called from canvas) ────────────────────────

    public void AddNode(double x, double y)
    {
        if (!IsEditable) return;
        Nodes.Add(new GraphNodeVm(_nextNodeId++, x, y));
        UpdateInfo();
        RunCommand.Refresh();
    }

    public void MoveNode(int id, double x, double y)
    {
        var node = Nodes.FirstOrDefault(n => n.Id == id);
        if (node is null || !IsEditable) return;
        node.X = x; node.Y = y;
    }

    public void RemoveNode(int id)
    {
        if (!IsEditable) return;
        var node = Nodes.FirstOrDefault(n => n.Id == id);
        if (node is null) return;
        Nodes.Remove(node);
        // Remove all edges involving this node
        var toRemove = Edges.Where(e => e.From == id || e.To == id).ToList();
        foreach (var e in toRemove) Edges.Remove(e);
        UpdateInfo(); RunCommand.Refresh();
    }

    public void AddEdge(int fromId, int toId)
    {
        if (!IsEditable || fromId == toId) return;
        if (Edges.Any(e => e.From == fromId && e.To == toId)) return;
        Edges.Add(new GraphEdgeVm { From = fromId, To = toId });
        UpdateInfo();
    }

    private void UpdateInfo() =>
        EdgeInfo = $"{Nodes.Count} nodes  ·  {Edges.Count} edges";

    // ── Example preload ───────────────────────────────────────────────────

    private void LoadExample()
    {
        // Simple diamond DAG: 0→1, 0→2, 1→3, 2→3
        double cx = 300, cy = 250;
        Nodes.Add(new GraphNodeVm(0, cx,       cy - 100));
        Nodes.Add(new GraphNodeVm(1, cx - 90,  cy));
        Nodes.Add(new GraphNodeVm(2, cx + 90,  cy));
        Nodes.Add(new GraphNodeVm(3, cx,       cy + 100));
        _nextNodeId = 4;
        Edges.Add(new GraphEdgeVm { From = 0, To = 1 });
        Edges.Add(new GraphEdgeVm { From = 0, To = 2 });
        Edges.Add(new GraphEdgeVm { From = 1, To = 3 });
        Edges.Add(new GraphEdgeVm { From = 2, To = 3 });
        UpdateInfo();
    }

    // ── Build GraphGenome from current drawing ────────────────────────────

    private GraphGenome BuildGenome()
    {
        int n = Nodes.Count;
        // Map node IDs to indices 0..n-1
        var idToIdx = Nodes.Select((nd, i) => (nd.Id, i)).ToDictionary(t => t.Id, t => t.i);
        var g = new GraphGenome(n, directed: true);
        foreach (var e in Edges)
        {
            if (idToIdx.TryGetValue(e.From, out int fi) && idToIdx.TryGetValue(e.To, out int ti))
                g.SetEdge(fi, ti, true);
        }
        return g;
    }

    // ── GA run ────────────────────────────────────────────────────────────

    private async void StartRun()
    {
        IsRunning = true;
        _bestSeries.Points.Clear();
        FitnessPlot.InvalidatePlot(false);
        EvolvedEdges.Clear();
        CurrentGeneration = 0;

        int n = Nodes.Count;
        var seedGenome = BuildGenome();

        var config = new EvolutionConfig
        {
            GenomeType     = GenomeType.Graph,
            GraphNodeCount = n,
            GraphDirected  = true,
            PopulationSize = PopulationSize,
            MaxGenerations = MaxGenerations,
            MutationRate   = MutationRate,
            CrossoverRate  = 0.7,
            EliteRatio     = 0.1,
            TournamentSize = 4,
            RandomSeed     = 42,
            LogInterval    = 2,
            BuiltinFitnessName = "MaxSpanning"
        };

        var fitness   = new DagMaxEdgeFitness();
        var selection = new TournamentSelection<GraphGenome>(4);
        var crossover = new GraphUniformCrossover();
        var mutation  = new EdgeToggleMutation();

        _cts    = new CancellationTokenSource();
        _engine = new GAEngine<GraphGenome>(
            config,
            () => { var g = new GraphGenome(n, true); g.Randomize(new Random()); return g; },
            selection, crossover, mutation, fitness);

        _engine.OnGenerationComplete += OnGeneration;
        _engine.OnComplete           += OnComplete;
        _engine.Initialize();

        Status = $"Evolving DAG — 0 / {MaxGenerations}";
        try { await _engine.RunAsync(_cts.Token); }
        catch (OperationCanceledException) { Status = "Stopped."; }
        finally { IsRunning = false; _cts?.Dispose(); _cts = null; }
    }

    private void OnGeneration(object? _, GenerationStats s)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentGeneration = s.Generation;
            BestScore = $"{s.BestFitness:F1}";
            Status = $"Gen {s.Generation}  ·  Best score: {s.BestFitness:F1}";
            _bestSeries.Points.Add(new DataPoint(s.Generation, s.BestFitness));
            FitnessPlot.InvalidatePlot(true);
            if (s.Generation % 10 == 0) RefreshEvolvedGraph(s.BestGenomeStr);
        });
    }

    private void OnComplete(object? _, EventArgs __)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Status = $"Complete!  Best score: {-_engine!.BestFitness:F1}";
            RefreshEvolvedGraph(_engine.BestGenomeStr);
        });
    }

    private void RefreshEvolvedGraph(string genomeStr)
    {
        // Format: "Graph(N=4, E=3): 0->1(0.50) 0->2(0.30) 1->3(0.70) ..."
        EvolvedEdges.Clear();
        var nodeList = Nodes.ToList();
        if (nodeList.Count == 0) return;

        // Parse "i->j(w)" tokens
        var tokens = genomeStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var tok in tokens)
        {
            var arrowIdx = tok.IndexOf("->", StringComparison.Ordinal);
            if (arrowIdx < 0) continue;
            var fromStr = tok[..arrowIdx];
            var rest    = tok[(arrowIdx + 2)..];
            var parenIdx = rest.IndexOf('(');
            var toStr = parenIdx >= 0 ? rest[..parenIdx] : rest;

            if (!int.TryParse(fromStr, out int fi) || !int.TryParse(toStr, out int ti)) continue;
            if (fi < 0 || fi >= nodeList.Count || ti < 0 || ti >= nodeList.Count) continue;

            EvolvedEdges.Add(new GraphEdgeVm
            {
                From = nodeList[fi].Id, To = nodeList[ti].Id, IsEvolved = true
            });
        }
    }

    private void Reset()
    {
        EvolvedEdges.Clear();
        _bestSeries.Points.Clear();
        FitnessPlot.InvalidatePlot(false);
        BestScore = "–";
        Status = "Reset. Click canvas to add nodes.";
        CurrentGeneration = 0;
    }

    private void ClearAll()
    {
        Nodes.Clear(); Edges.Clear(); EvolvedEdges.Clear();
        _nextNodeId = 0;
        Reset();
        RunCommand.Refresh();
    }

    private static PlotModel BuildPlot()
    {
        var m = new PlotModel
        {
            Title = "Fitness (DAG Score)",
            Background = OxyColor.FromRgb(0x0A,0x0A,0x0F),
            TextColor  = OxyColor.FromRgb(0xF1,0xF5,0xF9),
            PlotAreaBorderColor = OxyColor.FromRgb(0x2D,0x2D,0x3E),
            TitleFontSize = 12, TitleColor = OxyColor.FromRgb(0xA7,0x8B,0xFA),
        };
        m.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Generation",
            TextColor = OxyColor.FromRgb(0x64,0x74,0x8B), AxislineColor = OxyColor.FromRgb(0x2D,0x2D,0x3E),
            TicklineColor = OxyColor.FromRgb(0x2D,0x2D,0x3E) });
        m.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Score",
            TextColor = OxyColor.FromRgb(0x64,0x74,0x8B), AxislineColor = OxyColor.FromRgb(0x2D,0x2D,0x3E),
            TicklineColor = OxyColor.FromRgb(0x2D,0x2D,0x3E) });
        m.Series.Add(new LineSeries { Color = OxyColor.FromRgb(0x38,0xBD,0xF8), StrokeThickness = 2 });
        return m;
    }
}
