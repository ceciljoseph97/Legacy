// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/ViewModels/ExperimentViewModel.cs
using System.Windows;
using NeuroSim.Engine.Engine;
using NeuroSim.Engine.FitnessEvaluators;
using NeuroSim.Engine.Genomes;

namespace NeuroSim.UI.ViewModels;

/// <summary>
/// Full custom-experiment page: config + engine runner + visualization + analytics.
/// </summary>
public sealed class ExperimentViewModel : ViewModelBase
{
    // ── Sub-ViewModels ────────────────────────────────────────────────────
    public VisualizationViewModel Visualization { get; } = new();
    public AnalyticsViewModel     Analytics     { get; } = new();

    // ── Engine commands ───────────────────────────────────────────────────
    public RelayCommand RunCommand   { get; }
    public RelayCommand StopCommand  { get; }
    public RelayCommand ResetCommand { get; }

    private bool _isRunning;
    public bool IsRunning { get => _isRunning; set { Set(ref _isRunning, value); Notify(nameof(IsNotRunning)); } }
    public bool IsNotRunning => !_isRunning;

    private string _statusText = "Ready — configure and press Run";
    public string StatusText { get => _statusText; set => Set(ref _statusText, value); }

    private CancellationTokenSource? _cts;
    private GAEngineBase?            _engine;

    public ExperimentViewModel()
    {
        RunCommand   = new RelayCommand(StartRun,  () => !IsRunning);
        StopCommand  = new RelayCommand(() => _cts?.Cancel(), () => IsRunning);
        ResetCommand = new RelayCommand(Reset, () => !IsRunning);
    }

    private async void StartRun()
    {
        IsRunning = true;
        Visualization.Reset();
        Analytics.Reset();
        StatusText = "Initialising engine…";

        var config = BuildConfig();
        _cts    = new CancellationTokenSource();
        _engine = GAEngineFactory.Create(config);
        _engine.OnGenerationComplete += OnGeneration;
        _engine.OnComplete           += OnComplete;
        _engine.Initialize();
        StatusText = $"Running — 0 / {config.MaxGenerations}";

        try { await _engine.RunAsync(_cts.Token); }
        catch (OperationCanceledException) { StatusText = "Stopped."; }
        finally { IsRunning = false; _cts.Dispose(); _cts = null; }
    }

    private void OnGeneration(object? _, GenerationStats s)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Visualization.UpdateFromStats(s);
            Analytics.AddRow(s);
            if (_engine != null)
                Visualization.UpdateDiversity(_engine.GetAllFitnesses());
            StatusText = $"Gen {s.Generation}  |  Best: {s.BestFitness:F6}  |  Mean: {s.MeanFitness:F6}";
        });
    }

    private void OnComplete(object? _, EventArgs __)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            StatusText = _engine is null ? "Complete." : $"Complete!  Best: {_engine.BestFitness:F6}";
            IsRunning  = false;
        });
    }

    private void Reset()
    {
        _cts?.Cancel();
        Visualization.Reset();
        Analytics.Reset();
        StatusText = "Ready";
    }

    // ── Genome type ───────────────────────────────────────────────────────
    public IReadOnlyList<GenomeType> GenomeTypes { get; } =
        Enum.GetValues<GenomeType>().ToList();

    private GenomeType _selectedGenomeType = GenomeType.Binary;
    public GenomeType SelectedGenomeType
    {
        get => _selectedGenomeType;
        set
        {
            Set(ref _selectedGenomeType, value);
            RefreshFitnessOptions();
            Notify(nameof(ShowBinaryParams));
            Notify(nameof(ShowRealParams));
            Notify(nameof(ShowTreeParams));
            Notify(nameof(ShowGraphParams));
        }
    }

    public bool ShowBinaryParams => SelectedGenomeType == GenomeType.Binary;
    public bool ShowRealParams => SelectedGenomeType == GenomeType.RealValued;
    public bool ShowTreeParams => SelectedGenomeType == GenomeType.Tree;
    public bool ShowGraphParams => SelectedGenomeType == GenomeType.Graph;

    // ── Population ────────────────────────────────────────────────────────
    private int _populationSize = 100;
    public int PopulationSize { get => _populationSize; set => Set(ref _populationSize, Math.Clamp(value, 2, 10000)); }

    private int _maxGenerations = 200;
    public int MaxGenerations { get => _maxGenerations; set => Set(ref _maxGenerations, Math.Clamp(value, 1, 100000)); }

    private double _eliteRatio = 0.05;
    public double EliteRatio { get => _eliteRatio; set => Set(ref _eliteRatio, Math.Clamp(value, 0, 0.5)); }

    // ── Operators ─────────────────────────────────────────────────────────
    public IReadOnlyList<SelectionType> SelectionTypes { get; } = Enum.GetValues<SelectionType>().ToList();
    private SelectionType _selectionType = SelectionType.Tournament;
    public SelectionType SelectedSelectionType { get => _selectionType; set => Set(ref _selectionType, value); }

    private int _tournamentSize = 3;
    public int TournamentSize { get => _tournamentSize; set => Set(ref _tournamentSize, Math.Clamp(value, 2, 20)); }

    public IReadOnlyList<BinaryCrossoverType> BinaryCrossoverTypes { get; } = Enum.GetValues<BinaryCrossoverType>().ToList();
    private BinaryCrossoverType _binaryCrossover = BinaryCrossoverType.SinglePoint;
    public BinaryCrossoverType SelectedBinaryCrossover { get => _binaryCrossover; set => Set(ref _binaryCrossover, value); }

    public IReadOnlyList<RealCrossoverType> RealCrossoverTypes { get; } = Enum.GetValues<RealCrossoverType>().ToList();
    private RealCrossoverType _realCrossover = RealCrossoverType.Blend;
    public RealCrossoverType SelectedRealCrossover { get => _realCrossover; set => Set(ref _realCrossover, value); }

    public IReadOnlyList<RealMutationType> RealMutationTypes { get; } = Enum.GetValues<RealMutationType>().ToList();
    private RealMutationType _realMutation = RealMutationType.Gaussian;
    public RealMutationType SelectedRealMutation { get => _realMutation; set => Set(ref _realMutation, value); }

    // ── Rates ─────────────────────────────────────────────────────────────
    private double _crossoverRate = 0.8;
    public double CrossoverRate { get => _crossoverRate; set => Set(ref _crossoverRate, Math.Clamp(value, 0, 1)); }

    private double _mutationRate = 0.01;
    public double MutationRate { get => _mutationRate; set => Set(ref _mutationRate, Math.Clamp(value, 0, 1)); }

    private double _gaussianSigma = 0.1;
    public double GaussianSigma { get => _gaussianSigma; set => Set(ref _gaussianSigma, Math.Clamp(value, 0.001, 10)); }

    // ── Genome-specific ────────────────────────────────────────────────────
    private int _genomeLength = 30;
    public int GenomeLength { get => _genomeLength; set => Set(ref _genomeLength, Math.Clamp(value, 1, 1000)); }

    private double _realMin = -5.0;
    public double RealMin { get => _realMin; set => Set(ref _realMin, value); }

    private double _realMax = 5.0;
    public double RealMax { get => _realMax; set => Set(ref _realMax, value); }

    private int _treeVars = 1;
    public int TreeVariables { get => _treeVars; set => Set(ref _treeVars, Math.Clamp(value, 1, 10)); }

    private int _treeDepth = 5;
    public int MaxTreeDepth { get => _treeDepth; set => Set(ref _treeDepth, Math.Clamp(value, 2, 10)); }

    private int _graphNodes = 8;
    public int GraphNodes { get => _graphNodes; set => Set(ref _graphNodes, Math.Clamp(value, 2, 50)); }

    private bool _graphDirected = false;
    public bool GraphDirected { get => _graphDirected; set => Set(ref _graphDirected, value); }

    // ── Fitness function ──────────────────────────────────────────────────
    private List<string> _fitnessOptions = BuiltinFitnessRegistry.BinaryOptions.ToList();
    public List<string> FitnessOptions
    {
        get => _fitnessOptions;
        private set => Set(ref _fitnessOptions, value);
    }

    private string _selectedFitness = "OneMax";
    public string SelectedFitness { get => _selectedFitness; set => Set(ref _selectedFitness, value); }

    // ── Termination ───────────────────────────────────────────────────────
    private bool _useTargetFitness = false;
    public bool UseTargetFitness { get => _useTargetFitness; set => Set(ref _useTargetFitness, value); }

    private double _targetFitness = 1.0;
    public double TargetFitness { get => _targetFitness; set => Set(ref _targetFitness, value); }

    private int _randomSeed = 42;
    public int RandomSeed { get => _randomSeed; set => Set(ref _randomSeed, value); }

    // ── Build config ──────────────────────────────────────────────────────
    public EvolutionConfig BuildConfig()
    {
        return new EvolutionConfig
        {
            GenomeType = SelectedGenomeType,
            PopulationSize = PopulationSize,
            MaxGenerations = MaxGenerations,
            EliteRatio = EliteRatio,
            SelectionType = SelectedSelectionType,
            TournamentSize = TournamentSize,
            BinaryCrossoverType = SelectedBinaryCrossover,
            RealCrossoverType = SelectedRealCrossover,
            RealMutationType = SelectedRealMutation,
            CrossoverRate = CrossoverRate,
            MutationRate = MutationRate,
            GaussianSigma = GaussianSigma,
            GenomeLength = GenomeLength,
            RealMinValue = RealMin,
            RealMaxValue = RealMax,
            NumTreeVariables = TreeVariables,
            MaxTreeDepth = MaxTreeDepth,
            GraphNodeCount = GraphNodes,
            GraphDirected = GraphDirected,
            BuiltinFitnessName = SelectedFitness,
            UseTargetFitness = UseTargetFitness,
            TargetFitness = TargetFitness,
            RandomSeed = RandomSeed,
            LogInterval = 1
        };
    }

    private void RefreshFitnessOptions()
    {
        var opts = SelectedGenomeType switch
        {
            GenomeType.Binary => BuiltinFitnessRegistry.BinaryOptions,
            GenomeType.RealValued => BuiltinFitnessRegistry.RealOptions,
            GenomeType.Tree => BuiltinFitnessRegistry.TreeOptions,
            GenomeType.Graph => BuiltinFitnessRegistry.GraphOptions,
            _ => BuiltinFitnessRegistry.BinaryOptions
        };
        FitnessOptions = opts.ToList();
        SelectedFitness = FitnessOptions.FirstOrDefault() ?? "";
    }
}
