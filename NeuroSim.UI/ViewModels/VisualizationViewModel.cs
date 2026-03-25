// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/ViewModels/VisualizationViewModel.cs
using System.Collections.ObjectModel;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using NeuroSim.Engine.Engine;
using NeuroSim.Engine.Genomes;

namespace NeuroSim.UI.ViewModels;

public sealed class VisualizationViewModel : ViewModelBase
{
    // ── Plot model ────────────────────────────────────────────────────────
    public PlotModel FitnessPlotModel { get; } = BuildPlot();

    private LineSeries _bestSeries = null!;
    private LineSeries _meanSeries = null!;
    private LineSeries _worstSeries = null!;

    // ── Best genome display ───────────────────────────────────────────────
    private string _bestGenomeDisplay = "(not started)";
    public string BestGenomeDisplay { get => _bestGenomeDisplay; set => Set(ref _bestGenomeDisplay, value); }

    // ── Genome canvas support ─────────────────────────────────────────────
    private TreeGenome?  _bestTreeGenome;
    private GraphGenome? _bestGraphGenome;
    private bool _showTree, _showGraph;

    public TreeGenome?  BestTreeGenome  { get => _bestTreeGenome;  set => Set(ref _bestTreeGenome,  value); }
    public GraphGenome? BestGraphGenome { get => _bestGraphGenome; set => Set(ref _bestGraphGenome, value); }
    public bool ShowTreeCanvas  { get => _showTree;  set { Set(ref _showTree, value);  Notify(nameof(ShowGenomeCanvas)); } }
    public bool ShowGraphCanvas { get => _showGraph; set { Set(ref _showGraph, value); Notify(nameof(ShowGenomeCanvas)); } }
    public bool ShowGenomeCanvas => _showTree || _showGraph;
    public string GenomeCanvasLabel => _showTree ? "BEST TREE" : "BEST GRAPH";

    // ── Live stats ────────────────────────────────────────────────────────
    private string _currentGen = "–";
    public string CurrentGeneration { get => _currentGen; set => Set(ref _currentGen, value); }

    private string _bestFitStr = "–";
    public string BestFitnessStr { get => _bestFitStr; set => Set(ref _bestFitStr, value); }

    private string _meanFitStr = "–";
    public string MeanFitnessStr { get => _meanFitStr; set => Set(ref _meanFitStr, value); }

    private string _stdDevStr = "–";
    public string StdDevStr { get => _stdDevStr; set => Set(ref _stdDevStr, value); }

    // ── Population diversity (bar per individual, sorted by fitness) ──────
    public PlotModel DiversityPlotModel { get; } = BuildDiversityPlot();
    private BarSeries _diversityBars = null!;

    public VisualizationViewModel()
    {
        WireSeriesRefs();
        WireDiversityRefs();
    }

    public void Reset()
    {
        _bestSeries.Points.Clear();
        _meanSeries.Points.Clear();
        _worstSeries.Points.Clear();
        FitnessPlotModel.InvalidatePlot(false);

        _diversityBars.Items.Clear();
        DiversityPlotModel.InvalidatePlot(false);

        BestGenomeDisplay = "(not started)";
        CurrentGeneration = "–";
        BestFitnessStr = "–";
        MeanFitnessStr = "–";
        StdDevStr = "–";
    }

    public void UpdateFromStats(GenerationStats stats)
    {
        CurrentGeneration = stats.Generation.ToString();
        BestFitnessStr = stats.BestFitness.ToString("F6");
        MeanFitnessStr = stats.MeanFitness.ToString("F6");
        StdDevStr = stats.StdDev.ToString("F4");

        // Truncate long genomes for readability
        BestGenomeDisplay = stats.BestGenomeStr.Length > 120
            ? stats.BestGenomeStr[..120] + "…"
            : stats.BestGenomeStr;

        _bestSeries.Points.Add(new DataPoint(stats.Generation, stats.BestFitness));
        _meanSeries.Points.Add(new DataPoint(stats.Generation, stats.MeanFitness));
        _worstSeries.Points.Add(new DataPoint(stats.Generation, stats.WorstFitness));
        FitnessPlotModel.InvalidatePlot(true);
    }

    public void UpdateDiversity(IReadOnlyList<double> fitnesses)
    {
        _diversityBars.Items.Clear();
        foreach (double f in fitnesses.OrderBy(x => x))
            _diversityBars.Items.Add(new BarItem(f));
        DiversityPlotModel.InvalidatePlot(true);
    }

    // ── Plot builders ─────────────────────────────────────────────────────

    private static PlotModel BuildPlot()
    {
        var model = new PlotModel
        {
            Title = "Fitness Over Generations",
            Background = OxyColor.FromRgb(0x11, 0x11, 0x16),
            TextColor = OxyColor.FromRgb(0xF1, 0xF5, 0xF9),
            PlotAreaBorderColor = OxyColor.FromRgb(0x2D, 0x2D, 0x3E),
            TitleFontSize = 14,
            TitleColor = OxyColor.FromRgb(0xA7, 0x8B, 0xFA),
        };

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom, Title = "Generation",
            AxislineColor = OxyColor.FromRgb(0x2D,0x2D,0x3E),
            TicklineColor = OxyColor.FromRgb(0x2D,0x2D,0x3E),
            TextColor = OxyColor.FromRgb(0x94,0xA3,0xB8),
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromArgb(40,0x2D,0x2D,0x3E),
        });
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left, Title = "Fitness",
            AxislineColor = OxyColor.FromRgb(0x2D,0x2D,0x3E),
            TicklineColor = OxyColor.FromRgb(0x2D,0x2D,0x3E),
            TextColor = OxyColor.FromRgb(0x94,0xA3,0xB8),
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromArgb(40,0x2D,0x2D,0x3E),
        });

        model.Series.Add(new LineSeries { Title = "Best", Color = OxyColor.FromRgb(0x22, 0xC5, 0x5E), StrokeThickness = 2 });
        model.Series.Add(new LineSeries { Title = "Mean", Color = OxyColor.FromRgb(0xFB, 0xBF, 0x24), StrokeThickness = 1.5, LineStyle = LineStyle.Dash });
        model.Series.Add(new LineSeries { Title = "Worst", Color = OxyColor.FromRgb(0xEF, 0x44, 0x44), StrokeThickness = 1, LineStyle = LineStyle.Dot });

        model.IsLegendVisible = true;

        return model;
    }

    private static PlotModel BuildDiversityPlot()
    {
        var model = new PlotModel
        {
            Title = "Population Fitness Distribution",
            Background = OxyColor.FromRgb(0x11, 0x11, 0x16),
            TextColor = OxyColor.FromRgb(0xF1, 0xF5, 0xF9),
            PlotAreaBorderColor = OxyColor.FromRgb(0x2D, 0x2D, 0x3E),
            TitleFontSize = 13,
            TitleColor = OxyColor.FromRgb(0xA7, 0x8B, 0xFA),
        };

        model.Axes.Add(new CategoryAxis { Position = AxisPosition.Left, Title = "Individual" });
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Fitness" });

        model.Series.Add(new BarSeries
        {
            FillColor = OxyColor.FromArgb(180, 0x7C, 0x3A, 0xED),
            StrokeColor = OxyColor.FromRgb(0xA8, 0x55, 0xF7),
            StrokeThickness = 0.5
        });

        return model;
    }

    private void WireSeriesRefs()
    {
        _bestSeries = (LineSeries)FitnessPlotModel.Series[0];
        _meanSeries = (LineSeries)FitnessPlotModel.Series[1];
        _worstSeries = (LineSeries)FitnessPlotModel.Series[2];
    }

    private void WireDiversityRefs()
    {
        _diversityBars = (BarSeries)DiversityPlotModel.Series[0];
    }
}
