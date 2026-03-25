// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/ViewModels/MazePlaygroundViewModel.cs
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using NeuroSim.Problems.Maze;
using NeuroSim.UI.Models;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace NeuroSim.UI.ViewModels;

/// <summary>Edit tool for the maze canvas.</summary>
public enum MazeEditMode { SetStart, SetGoal, ToggleWall }

/// <summary>
/// Maze playground ViewModel.
/// Extends PlaygroundViewModelBase — only maze-specific logic here.
/// </summary>
public sealed class MazePlaygroundViewModel : PlaygroundViewModelBase
{
    // ── Editable maze ─────────────────────────────────────────────────────

    private EditableMaze _editableMaze;
    /// <summary>Current maze (from editable). Exposed as MazeProblem for canvas/evolver.</summary>
    public MazeProblem? Maze => _editableMaze.ToMazeProblem();

    // ── Edit mode ─────────────────────────────────────────────────────────

    public IReadOnlyList<MazeEditMode> EditModes { get; } =
        [MazeEditMode.SetStart, MazeEditMode.SetGoal, MazeEditMode.ToggleWall];

    private MazeEditMode _editMode = MazeEditMode.ToggleWall;
    public MazeEditMode EditMode
    {
        get => _editMode;
        set => Set(ref _editMode, value);
    }

    // ── Maze preset ───────────────────────────────────────────────────────

    public IReadOnlyList<string> MazePresets { get; } = MazeDatasets.Names;

    private string _selectedPreset = MazeDatasets.Names[0];
    public string SelectedPreset
    {
        get => _selectedPreset;
        set { Set(ref _selectedPreset, value); LoadPreset(value); }
    }

    // ── Paradigm & operators ──────────────────────────────────────────────

    public IReadOnlyList<MazeParadigm> Paradigms { get; } =
        [MazeParadigm.GeneticAlgorithm, MazeParadigm.EvolutionStrategy, MazeParadigm.EvolutionaryProgramming];

    private MazeParadigm _paradigm = MazeParadigm.GeneticAlgorithm;
    public MazeParadigm SelectedParadigm
    {
        get => _paradigm;
        set { Set(ref _paradigm, value); ApplyParadigmDefaults(); }
    }

    public IReadOnlyList<MazeSelectionMethod> SelectionMethods { get; } =
        [MazeSelectionMethod.Tournament, MazeSelectionMethod.RouletteWheel, MazeSelectionMethod.RankBased];

    private MazeSelectionMethod _selection = MazeSelectionMethod.Tournament;
    public MazeSelectionMethod SelectedSelection { get => _selection; set => Set(ref _selection, value); }

    public IReadOnlyList<MazeCrossoverMethod> CrossoverMethods { get; } =
        [MazeCrossoverMethod.Uniform, MazeCrossoverMethod.TwoPoint, MazeCrossoverMethod.SinglePoint, MazeCrossoverMethod.None];

    private MazeCrossoverMethod _crossover = MazeCrossoverMethod.Uniform;
    public MazeCrossoverMethod SelectedCrossover { get => _crossover; set => Set(ref _crossover, value); }

    public bool CrossoverVisible => _paradigm == MazeParadigm.GeneticAlgorithm;

    public IReadOnlyList<MazeMutationMethod> MutationMethods { get; } =
        [MazeMutationMethod.Inversion, MazeMutationMethod.PointMutation,
         MazeMutationMethod.SegmentShuffle, MazeMutationMethod.BlockReset];

    private MazeMutationMethod _mutation = MazeMutationMethod.Inversion;
    public MazeMutationMethod SelectedMutation { get => _mutation; set => Set(ref _mutation, value); }

    public IReadOnlyList<MazeEsStrategy> EsStrategies { get; } =
        [MazeEsStrategy.MuPlusLambda, MazeEsStrategy.MuCommaLambda];

    private MazeEsStrategy _esStrategy = MazeEsStrategy.MuPlusLambda;
    public MazeEsStrategy EsStrategy { get => _esStrategy; set => Set(ref _esStrategy, value); }

    public bool EsOptionsVisible => _paradigm == MazeParadigm.EvolutionStrategy;

    // ── Parameters ────────────────────────────────────────────────────────

    private int _popSize = 100;
    public int PopulationSize { get => _popSize; set => Set(ref _popSize, Math.Clamp(value, 10, 2000)); }

    private int _maxGen = 600;
    public int MaxGenerations { get => _maxGen; set => Set(ref _maxGen, Math.Clamp(value, 10, 5000)); }

    private int _pathLen = 500;
    public int PathLength { get => _pathLen; set => Set(ref _pathLen, Math.Clamp(value, 20, 5000)); }

    private double _mutRate = 0.05;
    public double MutationRate { get => _mutRate; set => Set(ref _mutRate, Math.Clamp(value, 0.001, 1)); }

    private double _crossRate = 0.85;
    public double CrossoverRate { get => _crossRate; set => Set(ref _crossRate, Math.Clamp(value, 0, 1)); }

    private int _tournK = 4;
    public int TournamentSize { get => _tournK; set => Set(ref _tournK, Math.Clamp(value, 2, 20)); }

    private int _lambda = 5;
    public int Lambda { get => _lambda; set => Set(ref _lambda, Math.Clamp(value, 1, 20)); }

    private int _seed = 42;
    public int RandomSeed { get => _seed; set => Set(ref _seed, value); }

    // ── Live stats ────────────────────────────────────────────────────────

    private string _bestFitStr = "–";
    private string _meanFitStr = "–";
    private string _goalStr    = "–";
    private int    _currentGen;

    public string BestFitnessStr    { get => _bestFitStr;  private set => Set(ref _bestFitStr,  value); }
    public string MeanFitnessStr    { get => _meanFitStr;  private set => Set(ref _meanFitStr,  value); }
    public string GoalStatusStr     { get => _goalStr;     private set => Set(ref _goalStr,     value); }
    public int    CurrentGeneration { get => _currentGen; private set => Set(ref _currentGen, value); }

    private (int X, int Y)[]? _agentPath;
    public (int X, int Y)[]? AgentPath { get => _agentPath; private set => Set(ref _agentPath, value); }

    private int[] _bestGenome = Array.Empty<int>();
    /// <summary>Best genome for display. Direction sequence 0=N,1=E,2=S,3=W.</summary>
    public GenomeDisplayInfo GenomeDisplay => new()
    {
        DisplayType       = GenomeDisplayType.DirectionSequence,
        Title              = "Best path (genome)",
        DirectionSequence  = _bestGenome.Length > 0 ? _bestGenome : null
    };

    // ── Snapshot ─────────────────────────────────────────────────────────

    private string _snapshotName = "Maze_Custom";
    public string SnapshotName { get => _snapshotName; set => Set(ref _snapshotName, value); }

    private readonly List<MazeGenStats> _history = new();

    // ── Commands ──────────────────────────────────────────────────────────

    public RelayCommand RunCommand          { get; }
    public RelayCommand ResetCommand        { get; }
    public RelayCommand SaveSnapshotCommand { get; }
    public RelayCommand LoadSnapshotCommand { get; }
    public RelayCommand NewBlankMazeCommand { get; }

    // ── Plots ─────────────────────────────────────────────────────────────

    public PlotModel FitnessPlot { get; } = BuildFitnessPlot();
    private readonly LineSeries _bestSeries;
    private readonly LineSeries _meanSeries;

    // ── Modular analysis (PlaygroundAnalysisView) ───────────────────────────
    public ObservableCollection<StatBadge> StatBadges { get; } = new();
    public ObservableCollection<ChartSlot> Charts { get; } = new();

    // ── Constructor ───────────────────────────────────────────────────────

    public MazePlaygroundViewModel(NavigationService nav) : base(nav)
    {
        _editableMaze = EditableMaze.FromMazeProblem(MazeDatasets.Get(MazeDatasets.Names[0]));

        RunCommand           = new RelayCommand(() => StartEvolution(), () => !IsRunning);
        ResetCommand         = new RelayCommand(DoReset, () => !IsRunning);
        SaveSnapshotCommand  = new RelayCommand(SaveSnapshot, () => _history.Count > 0 || _editableMaze != null);
        LoadSnapshotCommand  = new RelayCommand(LoadSnapshot);
        NewBlankMazeCommand  = new RelayCommand(NewBlankMaze, () => IsEditable);

        _bestSeries = (LineSeries)FitnessPlot.Series[0];
        _meanSeries = (LineSeries)FitnessPlot.Series[1];

        Charts.Add(new ChartSlot { Title = "Fitness Convergence", Plot = FitnessPlot });
        RefreshStatBadges();
        Status = "Click cells to edit (Start / Goal / Wall). Load preset or run evolution.";
    }

    private void RefreshStatBadges()
    {
        StatBadges.Clear();
        StatBadges.Add(new StatBadge { Label = "BEST FITNESS", Value = BestFitnessStr, SubLabel = "" });
        StatBadges.Add(new StatBadge { Label = "MEAN FITNESS", Value = MeanFitnessStr, SubLabel = "" });
        StatBadges.Add(new StatBadge { Label = "GENERATION", Value = CurrentGeneration.ToString(), SubLabel = "" });
        StatBadges.Add(new StatBadge { Label = "GOAL", Value = GoalStatusStr, SubLabel = "" });
    }

    // ── Cell click (from MazeCanvas) ───────────────────────────────────────

    /// <summary>Called when user clicks a grid cell. (x,y) in maze coordinates.</summary>
    public void OnCellClicked(int x, int y)
    {
        if (!_editableMaze.IsInBounds(x, y) || !IsEditable) return;

        switch (_editMode)
        {
            case MazeEditMode.SetStart:
                _editableMaze.Start = (x, y);
                Status = $"Start set to ({x},{y})";
                break;
            case MazeEditMode.SetGoal:
                _editableMaze.Goal = (x, y);
                Status = $"Goal set to ({x},{y})";
                break;
            case MazeEditMode.ToggleWall:
                _editableMaze.ToggleWall(x, y);
                Status = $"Toggled wall at ({x},{y})";
                break;
        }
        Notify(nameof(Maze));
    }

    // ── PlaygroundViewModelBase overrides ─────────────────────────────────

    protected override int TotalGenerations => MaxGenerations;

    protected override void ResetForNewRun()
    {
        CurrentGeneration = 0;
        BestFitnessStr    = "–";
        MeanFitnessStr    = "–";
        GoalStatusStr     = "–";
        AgentPath         = null;
        _bestGenome       = Array.Empty<int>();
        Notify(nameof(GenomeDisplay));
        _history.Clear();
        _bestSeries.Points.Clear();
        _meanSeries.Points.Clear();
        FitnessPlot.InvalidatePlot(true);
        RefreshStatBadges();
    }

    protected override async Task RunEvolutionAsync(CancellationToken ct)
    {
        var problem = _editableMaze.ToMazeProblem();

        var cfg = new MazeRunConfig
        {
            Paradigm          = SelectedParadigm,
            Selection         = SelectedSelection,
            Crossover         = SelectedCrossover,
            Mutation          = SelectedMutation,
            EsStrategy        = EsStrategy,
            PopulationSize    = PopulationSize,
            MaxGenerations    = MaxGenerations,
            PathLength        = PathLength,
            MutationRate      = MutationRate,
            CrossoverRate     = CrossoverRate,
            TournamentSize    = TournamentSize,
            Lambda            = Lambda,
            RandomSeed        = RandomSeed,
            GenerationDelayMs = SpeedDelayMs,
        };

        var evolver = new MazeEvolver(problem, cfg);
        evolver.OnGeneration += OnGeneration;

        Status = $"[{_paradigm}] Running — 0 / {MaxGenerations}";
        await evolver.RunAsync(ct);
    }

    protected override void OnRunComplete()
    {
        if (Status != "Stopped.")
            Status = $"Done — {CurrentGeneration} generations.";
        SaveSnapshotCommand.Refresh();
    }

    // ── Generation callback ───────────────────────────────────────────────

    private void OnGeneration(MazeGenStats s)
    {
        _history.Add(s);
        OnUiThread(() =>
        {
            CurrentGeneration = s.Generation;
            BestFitnessStr    = $"{s.BestFitness:F1}";
            MeanFitnessStr    = $"{s.MeanFitness:F1}";
            GoalStatusStr     = s.GoalReached ? $"✔ {s.StepsToGoal} steps" : "Not yet";
            AgentPath         = s.BestPath;
            _bestGenome       = s.BestGenome;
            Notify(nameof(GenomeDisplay));
            UpdateProgress(s.Generation);

            Status = $"[{_paradigm}]  Gen {s.Generation} / {MaxGenerations}  ·  Best: {s.BestFitness:F1}" +
                     (s.GoalReached ? $"  ·  ✔ Goal in {s.StepsToGoal} steps" : "");

            _bestSeries.Points.Add(new DataPoint(s.Generation, s.BestFitness));
            _meanSeries.Points.Add(new DataPoint(s.Generation, s.MeanFitness));
            FitnessPlot.InvalidatePlot(true);
            RefreshStatBadges();
        });
    }

    // ── Preset / blank maze ───────────────────────────────────────────────

    private void LoadPreset(string name)
    {
        _editableMaze = EditableMaze.FromMazeProblem(MazeDatasets.Get(name));
        AgentPath     = null;
        _bestGenome   = Array.Empty<int>();
        Notify(nameof(GenomeDisplay));
        AutoName();
        Notify(nameof(Maze));
        Status = $"Loaded: {name}  ({_editableMaze.Width}×{_editableMaze.Height})";
    }

    private void NewBlankMaze()
    {
        _editableMaze = new EditableMaze(15, 15);
        AgentPath     = null;
        _bestGenome   = Array.Empty<int>();
        Notify(nameof(GenomeDisplay));
        AutoName();
        Notify(nameof(Maze));
        Status = "Blank 15×15 maze. Set start, goal, add walls, then run.";
    }

    // ── Snapshot save/load ────────────────────────────────────────────────

    private void SaveSnapshot()
    {
        var dlg = new SaveFileDialog
        {
            Title      = "Save Maze Snapshot",
            Filter     = "Maze Snapshot (*.mazesnap)|*.mazesnap|JSON (*.json)|*.json",
            FileName   = $"{SnapshotName}_{DateTime.Now:yyyyMMdd_HHmmss}",
            DefaultExt = ".mazesnap"
        };
        if (dlg.ShowDialog() != true) return;

        var snap = new MazeExperimentSnapshot
        {
            Name             = SnapshotName,
            SavedAt          = DateTime.Now,
            MazeLabel        = _selectedPreset,
            Paradigm         = SelectedParadigm,
            Selection        = SelectedSelection,
            Mutation         = SelectedMutation,
            Crossover        = SelectedCrossover,
            EsStrategy       = EsStrategy,
            PopulationSize   = PopulationSize,
            MaxGenerations   = MaxGenerations,
            PathLength       = PathLength,
            MutationRate     = MutationRate,
            CrossoverRate    = CrossoverRate,
            TournamentSize   = TournamentSize,
            Lambda           = Lambda,
            RandomSeed       = RandomSeed,
            MazeSerialised   = _editableMaze.Serialise(),
            BestFitness      = _history.Count > 0 ? _history[^1].BestFitness : 0,
            MeanFitnessFinal = _history.Count > 0 ? _history[^1].MeanFitness : 0,
            GenerationsRun   = CurrentGeneration,
            GoalReached      = _history.Count > 0 && _history[^1].GoalReached,
            StepsToGoal      = _history.Count > 0 ? _history[^1].StepsToGoal : -1,
            History          = _history.Select(h => new MazeGenerationRecord
            {
                Generation  = h.Generation,
                BestFitness = h.BestFitness,
                MeanFitness = h.MeanFitness
            }).ToList()
        };

        try
        {
            var json = JsonSerializer.Serialize(snap, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dlg.FileName, json);
            Status = $"Saved: {Path.GetFileName(dlg.FileName)}";
        }
        catch (Exception ex) { Status = $"Save failed: {ex.Message}"; }
    }

    private void LoadSnapshot()
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Load Maze Snapshot",
            Filter = "Maze Snapshot (*.mazesnap)|*.mazesnap|JSON (*.json)|*.json"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var json = File.ReadAllText(dlg.FileName);
            var snap = JsonSerializer.Deserialize<MazeExperimentSnapshot>(json);
            if (snap is null) { Status = "Failed to load."; return; }

            _editableMaze = EditableMaze.Deserialise(snap.MazeSerialised);
            _selectedPreset = "Custom (loaded)";

            SelectedParadigm  = snap.Paradigm;
            SelectedSelection = snap.Selection;
            SelectedMutation  = snap.Mutation;
            SelectedCrossover = snap.Crossover;
            EsStrategy        = snap.EsStrategy;
            PopulationSize    = snap.PopulationSize;
            MaxGenerations    = snap.MaxGenerations;
            PathLength        = snap.PathLength;
            MutationRate      = snap.MutationRate;
            CrossoverRate     = snap.CrossoverRate;
            TournamentSize   = snap.TournamentSize;
            Lambda            = snap.Lambda;
            RandomSeed        = snap.RandomSeed;
            SnapshotName      = snap.Name;

            _history.Clear();
            foreach (var r in snap.History)
                _history.Add(new MazeGenStats(r.Generation, r.BestFitness, r.MeanFitness, false, -1, Array.Empty<(int, int)>(), Array.Empty<int>()));

            ClearPlots();
            foreach (var r in snap.History)
            {
                _bestSeries.Points.Add(new DataPoint(r.Generation, r.BestFitness));
                _meanSeries.Points.Add(new DataPoint(r.Generation, r.MeanFitness));
            }
            FitnessPlot.InvalidatePlot(true);

            BestFitnessStr    = snap.BestFitness > 0 ? $"{snap.BestFitness:F1}" : "–";
            MeanFitnessStr   = snap.MeanFitnessFinal > 0 ? $"{snap.MeanFitnessFinal:F1}" : "–";
            GoalStatusStr    = snap.GoalReached ? $"✔ {snap.StepsToGoal} steps" : "Not yet";
            CurrentGeneration = snap.GenerationsRun;
            ProgressPct      = 100;
            AgentPath        = null;
            _bestGenome      = Array.Empty<int>();
            Notify(nameof(GenomeDisplay));

            Notify(nameof(Maze));
            SaveSnapshotCommand.Refresh();
            RefreshStatBadges();
            Status = $"Loaded: {snap.Name}  ({snap.SavedAt:dd MMM HH:mm})";
        }
        catch (Exception ex) { Status = $"Load failed: {ex.Message}"; }
    }

    private void ClearPlots()
    {
        _bestSeries.Points.Clear();
        _meanSeries.Points.Clear();
    }

    private void AutoName()
    {
        SnapshotName = $"Maze_{_paradigm}_{_selectedPreset.Replace(" ", "")}_{_mutation}_{DateTime.Now:yyyyMMdd}";
    }

    // ── Paradigm defaults ─────────────────────────────────────────────────

    private void ApplyParadigmDefaults()
    {
        switch (_paradigm)
        {
            case MazeParadigm.GeneticAlgorithm:
                SelectedMutation  = MazeMutationMethod.Inversion;
                SelectedCrossover = MazeCrossoverMethod.Uniform;
                MutationRate      = 0.05;
                CrossoverRate     = 0.85;
                break;
            case MazeParadigm.EvolutionStrategy:
                SelectedMutation  = MazeMutationMethod.BlockReset;
                SelectedCrossover = MazeCrossoverMethod.None;
                MutationRate      = 0.1;
                break;
            case MazeParadigm.EvolutionaryProgramming:
                SelectedMutation  = MazeMutationMethod.SegmentShuffle;
                SelectedCrossover = MazeCrossoverMethod.None;
                MutationRate      = 0.06;
                break;
        }
        Notify(nameof(CrossoverVisible));
        Notify(nameof(EsOptionsVisible));
        AutoName();
    }

    // ── Reset ─────────────────────────────────────────────────────────────

    private void DoReset()
    {
        ResetForNewRun();
        RefreshStatBadges();
        Status = "Reset. Click RUN EVOLUTION to start.";
    }

    // ── Plot builder ──────────────────────────────────────────────────────

    private static PlotModel BuildFitnessPlot()
    {
        var m = new PlotModel { Background = OxyColor.FromRgb(9, 9, 15) };
        m.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Generation",
            AxislineColor = OxyColor.FromRgb(51,65,85), TextColor = OxyColor.FromRgb(100,116,139),
            TitleColor    = OxyColor.FromRgb(100,116,139), TicklineColor = OxyColor.FromRgb(51,65,85) });
        m.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Fitness",
            AxislineColor = OxyColor.FromRgb(51,65,85), TextColor = OxyColor.FromRgb(100,116,139),
            TitleColor    = OxyColor.FromRgb(100,116,139), TicklineColor = OxyColor.FromRgb(51,65,85) });
        m.Series.Add(new LineSeries { Title = "Best", Color = OxyColor.FromRgb(124,58,237), StrokeThickness = 2 });
        m.Series.Add(new LineSeries { Title = "Mean", Color = OxyColor.FromRgb(100,116,139), StrokeThickness = 1, LineStyle = LineStyle.Dash });
        return m;
    }
}
