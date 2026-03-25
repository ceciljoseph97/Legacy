// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/ViewModels/CspPlaygroundViewModel.cs
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using NeuroSim.Engine.Engine;
using NeuroSim.Problems.CSP;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace NeuroSim.UI.ViewModels;

public sealed class BinViewModel
{
    public int BinIndex { get; init; }
    public string Items { get; init; } = "";
    public double FillPct { get; init; }
    public string FillLabel { get; init; } = "";
    public SolidColorBrush BarBrush { get; init; } = Brushes.Violet;
}

public sealed class CspPlaygroundViewModel : ViewModelBase
{
    private readonly NavigationService _nav;

    // ── Dataset ───────────────────────────────────────────────────────────
    public IReadOnlyList<BinPackProblem> Datasets { get; } =
    [
        BinPackProblem.Sample,
        BinPackProblem.Random(15),
        BinPackProblem.Random(20, 99),
        BinPackProblem.Random(30, 7),
    ];

    private BinPackProblem _selected = BinPackProblem.Sample;
    public BinPackProblem SelectedDataset
    {
        get => _selected;
        set { Set(ref _selected, value); RefreshInfo(); }
    }

    // ── Config ────────────────────────────────────────────────────────────
    private int _popSize = 80;
    public int PopulationSize { get => _popSize; set => Set(ref _popSize, Math.Clamp(value, 10, 2000)); }

    private int _maxGen = 300;
    public int MaxGenerations { get => _maxGen; set => Set(ref _maxGen, Math.Clamp(value, 10, 5000)); }

    private double _mutRate = 0.2;
    public double MutationRate { get => _mutRate; set => Set(ref _mutRate, Math.Clamp(value, 0, 1)); }

    private int _seed = 42;
    public int RandomSeed { get => _seed; set => Set(ref _seed, value); }

    // ── State ─────────────────────────────────────────────────────────────
    private bool _isRunning;
    public bool IsRunning { get => _isRunning; set { Set(ref _isRunning, value); Notify(nameof(IsNotRunning)); } }
    public bool IsNotRunning => !_isRunning;

    private string _status = "Ready";
    public string Status { get => _status; set => Set(ref _status, value); }

    private string _binsUsedStr = "–";
    public string BinsUsedStr { get => _binsUsedStr; set => Set(ref _binsUsedStr, value); }

    private int _currentGen;
    public int CurrentGeneration { get => _currentGen; set => Set(ref _currentGen, value); }

    private string _problemInfo = "";
    public string ProblemInfo { get => _problemInfo; set => Set(ref _problemInfo, value); }

    // ── Bin visualization ─────────────────────────────────────────────────
    public ObservableCollection<BinViewModel> BinViews { get; } = new();

    // ── Fitness chart ─────────────────────────────────────────────────────
    public PlotModel FitnessPlot { get; } = BuildPlot();
    private LineSeries _bestSeries = null!;

    // ── Commands ──────────────────────────────────────────────────────────
    public RelayCommand RunCommand   { get; }
    public RelayCommand StopCommand  { get; }
    public RelayCommand ResetCommand { get; }

    private CancellationTokenSource? _cts;
    private GAEngineBase? _engine;

    public CspPlaygroundViewModel(NavigationService nav)
    {
        _nav = nav;
        RunCommand   = new RelayCommand(StartRun, () => !IsRunning);
        StopCommand  = new RelayCommand(() => _cts?.Cancel(), () => IsRunning);
        ResetCommand = new RelayCommand(Reset, () => !IsRunning);
        _bestSeries  = (LineSeries)FitnessPlot.Series[0];
        RefreshInfo();
    }

    private async void StartRun()
    {
        IsRunning = true;
        _bestSeries.Points.Clear();
        FitnessPlot.InvalidatePlot(false);
        BinViews.Clear();
        BinsUsedStr = "–";
        CurrentGeneration = 0;

        var cfg = CspGASetup.DefaultConfig(_selected.Items.Length);
        cfg.PopulationSize = PopulationSize;
        cfg.MaxGenerations = MaxGenerations;
        cfg.MutationRate   = MutationRate;
        cfg.RandomSeed     = RandomSeed;

        _cts    = new CancellationTokenSource();
        _engine = CspGASetup.CreateEngine(_selected, cfg);
        _engine.OnGenerationComplete += OnGeneration;
        _engine.OnComplete           += OnComplete;
        _engine.Initialize();
        Status = $"Running — 0 / {MaxGenerations}";

        try { await _engine.RunAsync(_cts.Token); }
        catch (OperationCanceledException) { Status = "Stopped."; }
        finally { IsRunning = false; _cts?.Dispose(); _cts = null; }
    }

    private void OnGeneration(object? _, GenerationStats s)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentGeneration = s.Generation;
            _bestSeries.Points.Add(new DataPoint(s.Generation, -s.BestFitness));
            FitnessPlot.InvalidatePlot(true);
            Status = $"Gen {s.Generation}  |  Best score: {-s.BestFitness:F3}";

            if (s.Generation % 10 == 0)
                UpdateBinView(s.BestGenomeStr);
        });
    }

    private void OnComplete(object? _, EventArgs __)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Status = $"Complete!  Best: {-_engine!.BestFitness:F3}";
            UpdateBinView(_engine.BestGenomeStr);
        });
    }

    private void UpdateBinView(string genomeStr)
    {
        var parts = genomeStr.Split('→');
        if (parts.Length != _selected.Items.Length) return;
        if (!int.TryParse(parts[0], out _)) return;

        var order = parts.Select(int.Parse).ToArray();
        var assignment = _selected.Decode(order);
        BinsUsedStr = $"{assignment.BinCount} bins";

        BinViews.Clear();
        var palette = new SolidColorBrush[]
        {
            new(Color.FromRgb(0x7C,0x3A,0xED)), new(Color.FromRgb(0x22,0xC5,0x5E)),
            new(Color.FromRgb(0xF5,0x9E,0x0B)), new(Color.FromRgb(0x38,0xBD,0xF8)),
            new(Color.FromRgb(0xEF,0x44,0x44)), new(Color.FromRgb(0xA7,0x8B,0xFA)),
        };
        for (int i = 0; i < assignment.Bins.Count; i++)
        {
            var bin = assignment.Bins[i];
            int used = bin.Sum(x => x.Size);
            double pct = used / (double)_selected.BinCapacity;
            BinViews.Add(new BinViewModel
            {
                BinIndex  = i + 1,
                Items     = string.Join(", ", bin.Select(x => x.Label)),
                FillPct   = pct,
                FillLabel = $"{used}/{_selected.BinCapacity} ({pct:P0})",
                BarBrush  = palette[i % palette.Length]
            });
        }
    }

    private void RefreshInfo()
    {
        int total = _selected.Items.Sum(x => x.Size);
        int minBins = (int)Math.Ceiling(total / (double)_selected.BinCapacity);
        ProblemInfo = $"{_selected.Items.Length} items  ·  capacity {_selected.BinCapacity}  ·  lower bound {minBins} bins";
    }

    private void Reset()
    {
        BinViews.Clear();
        _bestSeries.Points.Clear();
        FitnessPlot.InvalidatePlot(false);
        BinsUsedStr = "–";
        Status = "Ready";
        CurrentGeneration = 0;
    }

    private static PlotModel BuildPlot()
    {
        var m = new PlotModel
        {
            Title = "Fitness (− Bin Count)",
            Background = OxyColor.FromRgb(0x11,0x11,0x16),
            TextColor  = OxyColor.FromRgb(0xF1,0xF5,0xF9),
            PlotAreaBorderColor = OxyColor.FromRgb(0x2D,0x2D,0x3E),
            TitleFontSize = 13, TitleColor = OxyColor.FromRgb(0xA7,0x8B,0xFA),
        };
        m.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Generation",
            TextColor = OxyColor.FromRgb(0x94,0xA3,0xB8), AxislineColor = OxyColor.FromRgb(0x2D,0x2D,0x3E),
            TicklineColor = OxyColor.FromRgb(0x2D,0x2D,0x3E) });
        m.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Score",
            TextColor = OxyColor.FromRgb(0x94,0xA3,0xB8), AxislineColor = OxyColor.FromRgb(0x2D,0x2D,0x3E),
            TicklineColor = OxyColor.FromRgb(0x2D,0x2D,0x3E) });
        m.Series.Add(new LineSeries { Color = OxyColor.FromRgb(0x22,0xC5,0x5E), StrokeThickness = 2 });
        return m;
    }
}
