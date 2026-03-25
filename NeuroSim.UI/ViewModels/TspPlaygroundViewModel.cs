// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/ViewModels/TspPlaygroundViewModel.cs
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using NeuroSim.Problems.TSP;
using NeuroSim.UI.Models;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace NeuroSim.UI.ViewModels;

/// <summary>Stores raw city-space coordinates for a route edge.</summary>
public sealed class RouteSegment(double x1, double y1, double x2, double y2)
{
    public double X1 { get; } = x1; public double Y1 { get; } = y1;
    public double X2 { get; } = x2; public double Y2 { get; } = y2;
}

// ── Mutable city (supports name editing + live repaint) ───────────────────────

public sealed class MutableCity : ViewModelBase
{
    private double _x, _y;
    private string _name;
    public string Name { get => _name; set => Set(ref _name, value); }
    public double X    { get => _x;    set => Set(ref _x,    value); }
    public double Y    { get => _y;    set => Set(ref _y,    value); }
    public MutableCity(string name, double x, double y) { _name = name; _x = x; _y = y; }
    public TspCity ToTspCity() => new(Name, X, Y);
}

// ── Distance matrix entry (for the analysis grid) ────────────────────────────

public sealed class DistanceRow
{
    public string From { get; init; } = "";
    public string To   { get; init; } = "";
    public string Distance { get; init; } = "";
    public double RawDist { get; init; }
}

// ── Main ViewModel ────────────────────────────────────────────────────────────

public sealed class TspPlaygroundViewModel : ViewModelBase
{
    private readonly NavigationService _nav;

    // ── Cities ────────────────────────────────────────────────────────────
    public ObservableCollection<MutableCity> Cities { get; } = new();
    private int _nextId = 1;

    // ── Route ─────────────────────────────────────────────────────────────
    public ObservableCollection<RouteSegment> RouteSegments { get; } = new();

    // ── Presets ───────────────────────────────────────────────────────────
    public IReadOnlyList<string> Presets { get; } =
        ["Ulysses 16", "Berlin 52", "Random 20", "Random 30", "Empty canvas"];
    private string _preset = "Ulysses 16";
    public string SelectedPreset { get => _preset; set { Set(ref _preset, value); LoadPreset(value); AutoName(); } }

    // ── Paradigm ──────────────────────────────────────────────────────────
    public IReadOnlyList<TspParadigm> Paradigms { get; } = Enum.GetValues<TspParadigm>().ToList();

    private TspParadigm _paradigm = TspParadigm.GeneticAlgorithm;
    public TspParadigm SelectedParadigm
    {
        get => _paradigm;
        set { Set(ref _paradigm, value); ApplyParadigmDefaults(); RefreshDescriptions(); AutoName(); }
    }

    // ── Operator selectors ────────────────────────────────────────────────
    public ObservableCollection<TspSelectionMethod> AvailableSelections { get; } = new();
    public ObservableCollection<TspCrossoverMethod> AvailableCrossovers { get; } = new();
    public ObservableCollection<TspMutationMethod>  AvailableMutations  { get; } = new();

    private TspSelectionMethod _sel = TspSelectionMethod.Tournament;
    private TspCrossoverMethod _cross = TspCrossoverMethod.OX;
    private TspMutationMethod  _mut  = TspMutationMethod.TwoOpt;

    public TspSelectionMethod SelectedSelection { get => _sel;   set { Set(ref _sel,   value); RefreshDescriptions(); AutoName(); } }
    public TspCrossoverMethod SelectedCrossover { get => _cross; set { Set(ref _cross, value); RefreshDescriptions(); AutoName(); } }
    public TspMutationMethod  SelectedMutation  { get => _mut;   set { Set(ref _mut,   value); RefreshDescriptions(); AutoName(); } }

    public EsStrategy EsStrategy { get; set; } = EsStrategy.MuPlusLambda;
    public IReadOnlyList<EsStrategy> EsStrategies { get; } = Enum.GetValues<EsStrategy>().ToList();

    // ── GA Params ─────────────────────────────────────────────────────────
    private int _popSize = 80;
    public int PopulationSize { get => _popSize; set => Set(ref _popSize, Math.Clamp(value, 10, 2000)); }

    private int _maxGen = 500;
    public int MaxGenerations { get => _maxGen; set => Set(ref _maxGen, Math.Clamp(value, 10, 10000)); }

    private double _mutRate = 0.15;
    public double MutationRate { get => _mutRate; set => Set(ref _mutRate, Math.Clamp(value, 0, 0.5)); }

    private double _crossRate = 0.85;
    public double CrossoverRate { get => _crossRate; set => Set(ref _crossRate, Math.Clamp(value, 0, 1)); }

    private int _tournK = 4;
    public int TournamentSize { get => _tournK; set => Set(ref _tournK, Math.Clamp(value, 2, 20)); }

    private int _seed = 42;
    public int RandomSeed { get => _seed; set => Set(ref _seed, value); }

    // ── Speed control ─────────────────────────────────────────────────────
    private int _speedDelayMs = 50;
    public int SpeedDelayMs
    {
        get => _speedDelayMs;
        set { Set(ref _speedDelayMs, Math.Clamp(value, 0, 2000)); Notify(nameof(SpeedLabel)); }
    }
    public string SpeedLabel => _speedDelayMs == 0 ? "Full speed" :
                                _speedDelayMs < 100  ? $"{_speedDelayMs} ms" :
                                $"{_speedDelayMs / 1000.0:F1} s / gen";

    // ── Display toggles ───────────────────────────────────────────────────
    private bool _showDist = true;
    public bool ShowEdgeDistances { get => _showDist; set { Set(ref _showDist, value); RefreshRouteDisplay(); } }

    private bool _colorEdges = true;
    public bool ColorEdgesByLength { get => _colorEdges; set { Set(ref _colorEdges, value); RefreshRouteDisplay(); } }

    private bool _showDistMatrix;
    public bool ShowDistanceMatrix { get => _showDistMatrix; set => Set(ref _showDistMatrix, value); }

    // ── State ─────────────────────────────────────────────────────────────
    private bool _isRunning;
    public bool IsRunning  { get => _isRunning; set { Set(ref _isRunning, value); Notify(nameof(IsEditable)); Notify(nameof(IsNotRunning)); } }
    public bool IsNotRunning => !_isRunning;
    public bool IsEditable   => !_isRunning;

    private string _status = "Load a preset or right-click the map to add cities.";
    public string Status { get => _status; set => Set(ref _status, value); }

    private int _currentGen;
    public int CurrentGeneration { get => _currentGen; set => Set(ref _currentGen, value); }

    private string _bestDistStr = "–";
    public string BestDistStr { get => _bestDistStr; set => Set(ref _bestDistStr, value); }

    private string _meanDistStr = "–";
    public string MeanDistStr { get => _meanDistStr; set => Set(ref _meanDistStr, value); }

    private string _diversityStr = "–";
    public string DiversityStr { get => _diversityStr; set => Set(ref _diversityStr, value); }

    private double _progressPct;
    public double ProgressPct { get => _progressPct; set => Set(ref _progressPct, value); }

    // ── Descriptions ─────────────────────────────────────────────────────
    private string _paradigmDesc = "";
    public string ParadigmDesc { get => _paradigmDesc; set => Set(ref _paradigmDesc, value); }

    private string _selDesc = "";
    public string SelectionDesc { get => _selDesc; set => Set(ref _selDesc, value); }

    private string _crossDesc = "";
    public string CrossoverDesc { get => _crossDesc; set => Set(ref _crossDesc, value); }

    private string _mutDesc = "";
    public string MutationDesc { get => _mutDesc; set => Set(ref _mutDesc, value); }

    // ── Distance matrix ───────────────────────────────────────────────────
    public ObservableCollection<DistanceRow> DistanceRows { get; } = new();

    // ── Last best tour stored for edge-distance rendering ─────────────────
    private int[] _bestTour = Array.Empty<int>();
    public int[] BestTour => _bestTour;

    // ── Snapshot tab ──────────────────────────────────────────────────────
    private string _snapshotName = "My Experiment";
    public string SnapshotName { get => _snapshotName; set => Set(ref _snapshotName, value); }

    private string _loadedSnapshotInfo = "";
    public string LoadedSnapshotInfo { get => _loadedSnapshotInfo; set => Set(ref _loadedSnapshotInfo, value); }

    private bool _hasLoadedSnapshot;
    public bool HasLoadedSnapshot { get => _hasLoadedSnapshot; set => Set(ref _hasLoadedSnapshot, value); }

    // ── Generation history (for save/load) ───────────────────────────────
    private readonly List<TspGenStats> _history = new();

    // ── Commands ──────────────────────────────────────────────────────────
    public RelayCommand RunCommand           { get; }
    public RelayCommand StopCommand          { get; }
    public RelayCommand ResetCommand         { get; }
    public RelayCommand ClearCommand         { get; }
    public RelayCommand RefreshMatrixCommand { get; }
    public RelayCommand SaveSnapshotCommand  { get; }
    public RelayCommand LoadSnapshotCommand  { get; }
    public RelayCommand AddCityCommand       { get; }

    private CancellationTokenSource? _cts;

    // ── Plots ─────────────────────────────────────────────────────────────
    public PlotModel FitnessPlot   { get; } = BuildFitnessPlot();
    public PlotModel DiversityPlot { get; } = BuildDiversityPlot();
    private LineSeries _bestSeries = null!, _meanSeries = null!, _worstSeries = null!;
    private LineSeries _divSeries  = null!;

    // ── Modular analysis (PlaygroundAnalysisView) ───────────────────────────
    public ObservableCollection<StatBadge> StatBadges { get; } = new();
    public ObservableCollection<ChartSlot> Charts { get; } = new();

    /// <summary>Best genome for display. Permutation (city order).</summary>
    public GenomeDisplayInfo GenomeDisplay => new()
    {
        DisplayType = GenomeDisplayType.Permutation,
        Title       = "Best tour (genome)",
        Permutation = _bestTour.Length > 0 ? _bestTour : null
    };

    public TspPlaygroundViewModel(NavigationService nav)
    {
        _nav = nav;
        RunCommand           = new RelayCommand(StartRun,     () => Cities.Count >= 3 && !IsRunning);
        StopCommand          = new RelayCommand(() => _cts?.Cancel(), () => IsRunning);
        ResetCommand         = new RelayCommand(Reset,        () => !IsRunning);
        ClearCommand         = new RelayCommand(ClearAll,     () => !IsRunning);
        RefreshMatrixCommand = new RelayCommand(BuildDistanceMatrix);
        SaveSnapshotCommand  = new RelayCommand(SaveSnapshot, () => _history.Count > 0 || Cities.Count > 0);
        LoadSnapshotCommand  = new RelayCommand(LoadSnapshot);
        AddCityCommand       = new RelayCommand(PromptAddCity, () => IsEditable);

        _bestSeries  = (LineSeries)FitnessPlot.Series[0];
        _meanSeries  = (LineSeries)FitnessPlot.Series[1];
        _worstSeries = (LineSeries)FitnessPlot.Series[2];
        _divSeries   = (LineSeries)DiversityPlot.Series[0];

        Charts.Add(new ChartSlot { Title = "Convergence — Best / Mean / Worst", Plot = FitnessPlot });
        Charts.Add(new ChartSlot { Title = "Population Diversity (σ/μ)", Plot = DiversityPlot });

        ApplyParadigmDefaults();
        RefreshDescriptions();
        LoadPreset("Ulysses 16");
        AutoName();
        RefreshStatBadges();
    }

    private void RefreshStatBadges()
    {
        StatBadges.Clear();
        StatBadges.Add(new StatBadge { Label = "BEST", Value = BestDistStr, SubLabel = "tour distance" });
        StatBadges.Add(new StatBadge { Label = "MEAN", Value = MeanDistStr, SubLabel = "population avg" });
        StatBadges.Add(new StatBadge { Label = "GENERATION", Value = CurrentGeneration.ToString(), SubLabel = "of evolution" });
        StatBadges.Add(new StatBadge { Label = "DIVERSITY σ/μ", Value = DiversityStr, SubLabel = "population spread" });
    }

    // ── Paradigm defaults ─────────────────────────────────────────────────

    private void ApplyParadigmDefaults()
    {
        AvailableSelections.Clear();
        AvailableCrossovers.Clear();
        AvailableMutations.Clear();

        switch (_paradigm)
        {
            case TspParadigm.GeneticAlgorithm:
                foreach (var s in Enum.GetValues<TspSelectionMethod>()) AvailableSelections.Add(s);
                foreach (var c in Enum.GetValues<TspCrossoverMethod>()) if (c != TspCrossoverMethod.None) AvailableCrossovers.Add(c);
                foreach (var m in Enum.GetValues<TspMutationMethod>())  AvailableMutations.Add(m);
                SelectedSelection = TspSelectionMethod.Tournament;
                SelectedCrossover = TspCrossoverMethod.OX;
                SelectedMutation  = TspMutationMethod.TwoOpt;
                PopulationSize    = 80; MutationRate = 0.15; CrossoverRate = 0.85;
                break;

            case TspParadigm.EvolutionStrategy:
                AvailableSelections.Add(TspSelectionMethod.Tournament);
                AvailableSelections.Add(TspSelectionMethod.RankBased);
                AvailableCrossovers.Add(TspCrossoverMethod.None);  // ES: mutation-only
                foreach (var m in Enum.GetValues<TspMutationMethod>()) AvailableMutations.Add(m);
                SelectedSelection = TspSelectionMethod.RankBased;
                SelectedCrossover = TspCrossoverMethod.None;
                SelectedMutation  = TspMutationMethod.TwoOpt;
                PopulationSize    = 30; MutationRate = 0.3; CrossoverRate = 0;
                break;

            case TspParadigm.EvolutionaryProgramming:
                AvailableSelections.Add(TspSelectionMethod.Tournament);  // tournament between parents+offspring
                AvailableCrossovers.Add(TspCrossoverMethod.None);        // EP: no crossover
                AvailableMutations.Add(TspMutationMethod.Swap);
                AvailableMutations.Add(TspMutationMethod.TwoOpt);
                AvailableMutations.Add(TspMutationMethod.Relocate);
                SelectedSelection = TspSelectionMethod.Tournament;
                SelectedCrossover = TspCrossoverMethod.None;
                SelectedMutation  = TspMutationMethod.Swap;
                PopulationSize    = 40; MutationRate = 0.4; CrossoverRate = 0;
                break;

            case TspParadigm.GeneticProgramming:
                AvailableSelections.Add(TspSelectionMethod.Tournament);
                AvailableCrossovers.Add(TspCrossoverMethod.OX);           // GP uses OX as "structural crossover"
                AvailableCrossovers.Add(TspCrossoverMethod.PMX);
                AvailableMutations.Add(TspMutationMethod.Relocate);
                AvailableMutations.Add(TspMutationMethod.OrOpt);
                SelectedSelection = TspSelectionMethod.Tournament;
                SelectedCrossover = TspCrossoverMethod.OX;
                SelectedMutation  = TspMutationMethod.Relocate;
                PopulationSize    = 50; MutationRate = 0.2; CrossoverRate = 0.9;
                break;
        }
        Notify(nameof(ShowEsOptions));
    }

    public bool ShowEsOptions => _paradigm == TspParadigm.EvolutionStrategy;

    // ── City interactions (called from canvas) ────────────────────────────

    public void AddCity(double rawX, double rawY, string name = "")
    {
        if (!IsEditable) return;
        string cityName = string.IsNullOrWhiteSpace(name) ? $"C{_nextId}" : name;
        Cities.Add(new MutableCity(cityName, rawX, rawY));
        _nextId++;
        Status = $"{Cities.Count} cities — press Run to evolve.";
        RunCommand.Refresh();
        RouteSegments.Clear();
        BuildDistanceMatrix();
    }

    public void MoveCity(int index, double rawX, double rawY)
    {
        if (index < 0 || index >= Cities.Count || !IsEditable) return;
        Cities[index].X = rawX;
        Cities[index].Y = rawY;
        RefreshRouteDisplay();
        BuildDistanceMatrix();
    }

    public void RemoveCity(int index)
    {
        if (index < 0 || index >= Cities.Count || !IsEditable) return;
        Cities.RemoveAt(index);
        RouteSegments.Clear();
        BuildDistanceMatrix();
        RunCommand.Refresh();
        Status = $"{Cities.Count} cities.";
    }

    public void RenameCity(int index, string name)
    {
        if (index < 0 || index >= Cities.Count) return;
        Cities[index].Name = name;
        BuildDistanceMatrix();
    }

    // ── Add city via button (prompts dialog at map centre) ────────────────

    public event Action<double, double>? RequestCityDialog;

    private void PromptAddCity()
    {
        // Signal canvas to open dialog at centre; canvas raises the event with actual position
        RequestCityDialog?.Invoke(500, 500);
    }

    // ── Auto-name ─────────────────────────────────────────────────────────

    private void AutoName()
    {
        var preset = _preset?.Replace(" ", "") ?? "Custom";
        var paradigmShort = _paradigm switch
        {
            TspParadigm.GeneticAlgorithm        => "GA",
            TspParadigm.EvolutionStrategy        => "ES",
            TspParadigm.EvolutionaryProgramming  => "EP",
            TspParadigm.GeneticProgramming       => "GP",
            _ => "GA"
        };
        var crossShort = _cross switch
        {
            TspCrossoverMethod.OX               => "OX",
            TspCrossoverMethod.PMX              => "PMX",
            TspCrossoverMethod.EdgeRecombination => "ERX",
            TspCrossoverMethod.None             => "NoX",
            _ => ""
        };
        var mutShort = _mut switch
        {
            TspMutationMethod.TwoOpt   => "2Opt",
            TspMutationMethod.Swap     => "Swap",
            TspMutationMethod.Relocate => "Reloc",
            TspMutationMethod.OrOpt    => "OrOpt",
            _ => ""
        };
        SnapshotName = $"{paradigmShort}_{preset}_{crossShort}_{mutShort}";
    }

    // ── Run ───────────────────────────────────────────────────────────────


    private async void StartRun()
    {
        IsRunning = true;
        ClearPlots();
        RouteSegments.Clear();
        _history.Clear();
        BestDistStr  = "–"; MeanDistStr = "–"; DiversityStr = "–";
        CurrentGeneration = 0; ProgressPct = 0;
        SaveSnapshotCommand.Refresh();

        var problem = new TspProblem
        {
            Name   = "Custom",
            Cities = Cities.Select(c => c.ToTspCity()).ToArray()
        };

        var cfg = new TspRunConfig
        {
            Paradigm           = SelectedParadigm,
            Selection          = SelectedSelection,
            Crossover          = SelectedCrossover,
            Mutation           = SelectedMutation,
            EsStrategy         = EsStrategy,
            PopulationSize     = PopulationSize,
            MaxGenerations     = MaxGenerations,
            MutationRate       = MutationRate,
            CrossoverRate      = CrossoverRate,
            TournamentSize     = TournamentSize,
            RandomSeed         = RandomSeed,
            GenerationDelayMs  = SpeedDelayMs
        };

        var evolver = new TspEvolver(problem, cfg);
        evolver.OnGeneration += OnGeneration;

        Status = $"[{_paradigm}] Running — 0 / {MaxGenerations}";
        _cts   = new CancellationTokenSource();

        try { await evolver.RunAsync(_cts.Token); }
        catch (OperationCanceledException) { Status = "Stopped."; }
        finally
        {
            IsRunning = false; _cts?.Dispose(); _cts = null; ProgressPct = 100;
            SaveSnapshotCommand.Refresh();
        }
    }

    private void OnGeneration(TspGenStats s)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _history.Add(s);

            CurrentGeneration = s.Generation;
            BestDistStr       = $"{s.BestDistance:F1}";
            MeanDistStr       = $"{s.MeanDistance:F1}";
            DiversityStr      = $"{s.Diversity:P1}";
            ProgressPct       = s.Generation * 100.0 / MaxGenerations;

            Status = $"[{_paradigm}]  Gen {s.Generation} / {MaxGenerations}  ·  Best: {s.BestDistance:F1}  ·  Mean: {s.MeanDistance:F1}";

            _bestSeries.Points.Add(new DataPoint(s.Generation,  s.BestDistance));
            _meanSeries.Points.Add(new DataPoint(s.Generation,  s.MeanDistance));
            _worstSeries.Points.Add(new DataPoint(s.Generation, s.WorstDistance));
            FitnessPlot.InvalidatePlot(true);

            _divSeries.Points.Add(new DataPoint(s.Generation, s.Diversity));
            DiversityPlot.InvalidatePlot(true);

            _bestTour = s.BestTour;
            RefreshRouteDisplay();
            RefreshStatBadges();
            Notify(nameof(GenomeDisplay));
        });
    }

    // ── Save / Load Snapshot ──────────────────────────────────────────────

    private void SaveSnapshot()
    {
        var dlg = new SaveFileDialog
        {
            Title      = "Save Experiment Snapshot",
            Filter     = "TSP Snapshot (*.tspsnap)|*.tspsnap|JSON files (*.json)|*.json",
            FileName   = $"{_snapshotName.Replace(' ', '_')}_{DateTime.Now:yyyyMMdd_HHmmss}",
            DefaultExt = ".tspsnap"
        };
        if (dlg.ShowDialog() != true) return;

        var snap = new ExperimentSnapshot
        {
            Name             = SnapshotName,
            SavedAt          = DateTime.Now,
            DatasetLabel     = SelectedPreset,
            Paradigm         = SelectedParadigm,
            Selection        = SelectedSelection,
            Crossover        = SelectedCrossover,
            Mutation         = SelectedMutation,
            EsStrategy       = EsStrategy,
            PopulationSize   = PopulationSize,
            MaxGenerations   = MaxGenerations,
            MutationRate     = MutationRate,
            CrossoverRate    = CrossoverRate,
            TournamentSize   = TournamentSize,
            RandomSeed       = RandomSeed,
            Cities           = Cities.Select(c => new CitySnapshot { Name = c.Name, X = c.X, Y = c.Y }).ToList(),
            BestTour         = (int[])_bestTour.Clone(),
            BestDistance     = _history.Count > 0 ? _history[^1].BestDistance : 0,
            MeanDistanceFinal = _history.Count > 0 ? _history[^1].MeanDistance : 0,
            GenerationsRun   = CurrentGeneration,
            History          = _history.Select(s => new GenerationRecord
            {
                Generation = s.Generation,
                BestDist   = s.BestDistance,
                MeanDist   = s.MeanDistance,
                WorstDist  = s.WorstDistance,
                Diversity  = s.Diversity
            }).ToList()
        };

        var opts = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(dlg.FileName, JsonSerializer.Serialize(snap, opts));
        Status = $"Snapshot saved → {Path.GetFileName(dlg.FileName)}";
    }

    private void LoadSnapshot()
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Load Experiment Snapshot",
            Filter = "TSP Snapshot (*.tspsnap)|*.tspsnap|JSON files (*.json)|*.json"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var json = File.ReadAllText(dlg.FileName);
            var snap = JsonSerializer.Deserialize<ExperimentSnapshot>(json);
            if (snap is null) { Status = "Failed to load snapshot."; return; }

            // Restore cities
            Cities.Clear(); RouteSegments.Clear(); _history.Clear();
            foreach (var c in snap.Cities) Cities.Add(new MutableCity(c.Name, c.X, c.Y));
            _nextId = Cities.Count + 1;

            // Restore config
            SelectedParadigm  = snap.Paradigm;
            SelectedSelection = snap.Selection;
            SelectedCrossover = snap.Crossover;
            SelectedMutation  = snap.Mutation;
            EsStrategy        = snap.EsStrategy;
            PopulationSize    = snap.PopulationSize;
            MaxGenerations    = snap.MaxGenerations;
            MutationRate      = snap.MutationRate;
            CrossoverRate     = snap.CrossoverRate;
            TournamentSize    = snap.TournamentSize;
            RandomSeed        = snap.RandomSeed;
            SnapshotName      = snap.Name;

            // Restore plots from history
            ClearPlots();
            foreach (var r in snap.History)
            {
                _bestSeries.Points.Add(new DataPoint(r.Generation,  r.BestDist));
                _meanSeries.Points.Add(new DataPoint(r.Generation,  r.MeanDist));
                _worstSeries.Points.Add(new DataPoint(r.Generation, r.WorstDist));
                _divSeries.Points.Add(new DataPoint(r.Generation,   r.Diversity));
                _history.Add(new TspGenStats(r.Generation, r.BestDist, r.MeanDist, r.WorstDist, r.Diversity, snap.BestTour));
            }
            FitnessPlot.InvalidatePlot(true);
            DiversityPlot.InvalidatePlot(true);

            // Restore best tour
            _bestTour = snap.BestTour;
            RefreshRouteDisplay();
            Notify(nameof(GenomeDisplay));

            BestDistStr       = snap.BestDistance > 0 ? $"{snap.BestDistance:F1}" : "–";
            MeanDistStr       = snap.MeanDistanceFinal > 0 ? $"{snap.MeanDistanceFinal:F1}" : "–";
            DiversityStr      = snap.History.Count > 0 ? $"{snap.History[^1].Diversity:P1}" : "–";
            CurrentGeneration = snap.GenerationsRun;
            ProgressPct       = 100;

            HasLoadedSnapshot = true;
            LoadedSnapshotInfo = $"Loaded: {snap.Name}  ·  {snap.SavedAt:dd MMM yyyy HH:mm}  ·  {snap.Cities.Count} cities  ·  Gen {snap.GenerationsRun}  ·  Best {snap.BestDistance:F1}";
            BuildDistanceMatrix();
            RunCommand.Refresh();
            SaveSnapshotCommand.Refresh();
            RefreshStatBadges();
            Status = $"Snapshot loaded: {snap.Name} ({snap.SavedAt:dd MMM HH:mm})";
        }
        catch (Exception ex)
        {
            Status = $"Load failed: {ex.Message}";
        }
    }

    // ── Route display ─────────────────────────────────────────────────────

    public void RefreshRouteDisplay()
    {
        RouteSegments.Clear();
        if (_bestTour.Length != Cities.Count) return;
        var cities = Cities.ToList();
        for (int i = 0; i < _bestTour.Length; i++)
        {
            int a = _bestTour[i], b = _bestTour[(i + 1) % _bestTour.Length];
            if (a < cities.Count && b < cities.Count)
                RouteSegments.Add(new RouteSegment(cities[a].X, cities[a].Y, cities[b].X, cities[b].Y));
        }
    }

    // ── Distance matrix ───────────────────────────────────────────────────

    public void BuildDistanceMatrix()
    {
        DistanceRows.Clear();
        if (Cities.Count < 2) return;
        var list = Cities.ToList();
        for (int i = 0; i < Math.Min(list.Count, 20); i++)
            for (int j = i + 1; j < Math.Min(list.Count, 20); j++)
            {
                double d = TspProblem.Euclidean(list[i].ToTspCity(), list[j].ToTspCity());
                DistanceRows.Add(new DistanceRow
                {
                    From = list[i].Name, To = list[j].Name,
                    Distance = $"{d:F1}", RawDist = d
                });
            }
    }

    // ── Preset loader ─────────────────────────────────────────────────────

    private void LoadPreset(string name)
    {
        Cities.Clear(); RouteSegments.Clear(); _nextId = 1;
        TspProblem? p = name switch
        {
            "Ulysses 16" => TspDatasets.Ulysses16,
            "Berlin 52"  => TspDatasets.Berlin52,
            "Random 20"  => TspDatasets.Random(20),
            "Random 30"  => TspDatasets.Random(30),
            _ => null
        };
        if (p is not null)
        {
            // Normalise city coords to [50..950] canvas space
            double minX = p.Cities.Min(c => c.X), maxX = p.Cities.Max(c => c.X);
            double minY = p.Cities.Min(c => c.Y), maxY = p.Cities.Max(c => c.Y);
            double rangeX = maxX - minX, rangeY = maxY - minY;
            double scale = Math.Max(rangeX, rangeY);
            if (scale < 1e-9) scale = 1;

            foreach (var c in p.Cities)
            {
                double nx = (c.X - minX) / scale * 900 + 50;
                double ny = (c.Y - minY) / scale * 900 + 50;
                Cities.Add(new MutableCity(c.Name, nx, ny));
            }
            _nextId = Cities.Count + 1;
        }
        Status = p is null ? "Empty canvas — right-click to add cities." : $"Loaded {Cities.Count} cities.";
        RunCommand.Refresh();
        BuildDistanceMatrix();
    }

    private void Reset()
    {
        _bestTour = Array.Empty<int>();
        RouteSegments.Clear(); ClearPlots();
        BestDistStr = "–"; MeanDistStr = "–"; DiversityStr = "–";
        CurrentGeneration = 0; ProgressPct = 0;
        RefreshStatBadges();
        Status = $"{Cities.Count} cities — press Run.";
    }

    private void ClearAll()
    {
        Cities.Clear(); RouteSegments.Clear(); DistanceRows.Clear();
        _nextId = 1; Reset();
        RunCommand.Refresh();
    }

    private void ClearPlots()
    {
        _bestSeries.Points.Clear(); _meanSeries.Points.Clear(); _worstSeries.Points.Clear();
        FitnessPlot.InvalidatePlot(false);
        _divSeries.Points.Clear(); DiversityPlot.InvalidatePlot(false);
    }

    // ── Operator descriptions ─────────────────────────────────────────────

    private void RefreshDescriptions()
    {
        ParadigmDesc = _paradigm switch
        {
            TspParadigm.GeneticAlgorithm =>
                "GA (Holland, 1962) — Recombination-first. Parents exchange route segments (crossover) then small perturbations are applied. Exploits known good sub-tours via selection pressure.",
            TspParadigm.EvolutionStrategy =>
                "ES (Rechenberg & Schwefel, 1964 / 1973) — Mutation-first, self-adaptive step size. (μ+λ) keeps best from parents+offspring; (μ,λ) uses offspring only. No crossover required.",
            TspParadigm.EvolutionaryProgramming =>
                "EP (Fogel, 1966) — Each parent creates exactly one offspring via mutation. All 2μ compete in a stochastic tournament. No crossover — only mutation drives adaptation.",
            TspParadigm.GeneticProgramming =>
                "GP (Friedberg 1958 / Koza 1992) — Evolves a weight-vector heuristic that guides nearest-neighbour tour construction. Each individual encodes city attraction scores; BLX crossover recombines programs.",
            _ => ""
        };

        SelectionDesc = _sel switch
        {
            TspSelectionMethod.Tournament   => "Tournament (k): Pick k random individuals; the one with the shortest tour wins and becomes a parent.",
            TspSelectionMethod.RouletteWheel => "Roulette Wheel: Each individual selected proportionally to fitness. High-fitness individuals chosen more often but not exclusively.",
            TspSelectionMethod.RankBased    => "Rank-Based: Individuals ranked by fitness. Selection probability ∝ rank — avoids super-individual dominance.",
            _ => ""
        };

        CrossoverDesc = _cross switch
        {
            TspCrossoverMethod.OX  => "Order Crossover (OX): Copy a random segment from P1. Fill remaining positions with cities from P2 in their original relative order. Preserves sub-tours.",
            TspCrossoverMethod.PMX => "Partially Mapped Crossover (PMX): Map segment from P1, resolve conflicts with P2 via a position-swap mapping. Stronger locality than OX.",
            TspCrossoverMethod.EdgeRecombination => "Edge Recombination (ERX): Build offspring by preferring edges that appear in either parent. Preserves edge structure — strong at combining good edges.",
            TspCrossoverMethod.None => "No crossover — this paradigm uses mutation only. Each offspring is a mutated copy of a single parent.",
            _ => ""
        };

        MutationDesc = _mut switch
        {
            TspMutationMethod.TwoOpt   => "2-Opt: Reverse a random sub-sequence i..j in the tour. Eliminates edge crossings — very effective local improvement.",
            TspMutationMethod.Swap     => "Random Swap: Exchange two randomly chosen cities. Simple, high diversity, weaker local improvement than 2-opt.",
            TspMutationMethod.Relocate => "Relocate: Remove one city and reinsert it at a different position. Good for fixing poorly-positioned cities.",
            TspMutationMethod.OrOpt    => "Or-Opt: Relocate a chain of 2–3 consecutive cities as a block. Balances segment integrity with position improvement.",
            _ => ""
        };
    }

    // ── Plots ─────────────────────────────────────────────────────────────

    private static PlotModel BuildFitnessPlot()
    {
        var m = new PlotModel
        {
            Title = "Tour Distance", Background = OxyColor.FromRgb(0x0A,0x0A,0x0F),
            TextColor = OxyColor.FromRgb(0xF1,0xF5,0xF9),
            PlotAreaBorderColor = OxyColor.FromRgb(0x2D,0x2D,0x3E),
            TitleFontSize = 12, TitleColor = OxyColor.FromRgb(0xA7,0x8B,0xFA),
        };
        var axCfg = (LinearAxis ax) => {
            ax.TextColor = OxyColor.FromRgb(0x64,0x74,0x8B);
            ax.AxislineColor = OxyColor.FromRgb(0x2D,0x2D,0x3E);
            ax.TicklineColor = OxyColor.FromRgb(0x2D,0x2D,0x3E);
            ax.MajorGridlineStyle = LineStyle.Dot;
            ax.MajorGridlineColor = OxyColor.FromArgb(25,0xFF,0xFF,0xFF);
        };
        var xAx = new LinearAxis { Position = AxisPosition.Bottom, Title = "Generation" }; axCfg(xAx); m.Axes.Add(xAx);
        var yAx = new LinearAxis { Position = AxisPosition.Left,   Title = "Distance"  }; axCfg(yAx); m.Axes.Add(yAx);
        m.Series.Add(new LineSeries { Title = "Best",  Color = OxyColor.FromRgb(0x22,0xC5,0x5E), StrokeThickness = 2 });
        m.Series.Add(new LineSeries { Title = "Mean",  Color = OxyColor.FromRgb(0xF5,0x9E,0x0B), StrokeThickness = 1.5, LineStyle = LineStyle.Dash });
        m.Series.Add(new LineSeries { Title = "Worst", Color = OxyColor.FromRgb(0xEF,0x44,0x44), StrokeThickness = 1,   LineStyle = LineStyle.Dot  });
        m.IsLegendVisible = true;
        return m;
    }

    private static PlotModel BuildDiversityPlot()
    {
        var m = new PlotModel
        {
            Title = "Population Diversity", Background = OxyColor.FromRgb(0x0A,0x0A,0x0F),
            TextColor = OxyColor.FromRgb(0xF1,0xF5,0xF9),
            PlotAreaBorderColor = OxyColor.FromRgb(0x2D,0x2D,0x3E),
            TitleFontSize = 12, TitleColor = OxyColor.FromRgb(0xA7,0x8B,0xFA),
        };
        m.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Generation",
            TextColor = OxyColor.FromRgb(0x64,0x74,0x8B), AxislineColor = OxyColor.FromRgb(0x2D,0x2D,0x3E), TicklineColor = OxyColor.FromRgb(0x2D,0x2D,0x3E) });
        m.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "σ / μ",
            TextColor = OxyColor.FromRgb(0x64,0x74,0x8B), AxislineColor = OxyColor.FromRgb(0x2D,0x2D,0x3E), TicklineColor = OxyColor.FromRgb(0x2D,0x2D,0x3E) });
        m.Series.Add(new LineSeries { Color = OxyColor.FromRgb(0x38,0xBD,0xF8), StrokeThickness = 1.5 });
        return m;
    }
}
