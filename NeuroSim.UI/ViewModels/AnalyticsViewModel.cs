// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/ViewModels/AnalyticsViewModel.cs
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using NeuroSim.Engine.Engine;
using Microsoft.Win32;

namespace NeuroSim.UI.ViewModels;

public sealed class GenerationRow
{
    public int Gen { get; init; }
    public string Best { get; init; } = "";
    public string Mean { get; init; } = "";
    public string StdDev { get; init; } = "";
    public string Worst { get; init; } = "";
    public int Evals { get; init; }
    public string Ms { get; init; } = "";
}

public sealed class AnalyticsViewModel : ViewModelBase
{
    public ObservableCollection<GenerationRow> Rows { get; } = new();

    private string _summaryText = "No experiment run yet.";
    public string SummaryText { get => _summaryText; set => Set(ref _summaryText, value); }

    private EvolutionLog? _log;

    public RelayCommand ExportCsvCommand { get; }

    public AnalyticsViewModel()
    {
        ExportCsvCommand = new RelayCommand(ExportCsv, () => _log?.History.Count > 0);
    }

    public void Reset()
    {
        Rows.Clear();
        _log = null;
        SummaryText = "No experiment run yet.";
    }

    public void AddRow(GenerationStats s)
    {
        Rows.Add(new GenerationRow
        {
            Gen = s.Generation,
            Best = s.BestFitness.ToString("F6"),
            Mean = s.MeanFitness.ToString("F6"),
            StdDev = s.StdDev.ToString("F4"),
            Worst = s.WorstFitness.ToString("F6"),
            Evals = s.Evaluations,
            Ms = s.ElapsedMs.ToString("F1")
        });

        // scroll is handled by the view's CollectionChanged handler
    }

    public void FinishExperiment(EvolutionLog log)
    {
        _log = log;
        if (log.History.Count == 0) return;

        var best = log.History.MaxBy(s => s.BestFitness)!;
        var last = log.History[^1];
        SummaryText =
            $"Completed {last.Generation} generations | " +
            $"Best fitness: {best.BestFitness:F6} (gen {best.Generation}) | " +
            $"Final mean: {last.MeanFitness:F6} ± {last.StdDev:F4} | " +
            $"Total evaluations: {last.Evaluations:N0}";

        ExportCsvCommand.Refresh();
    }

    private void ExportCsv()
    {
        if (_log is null) return;
        var dlg = new SaveFileDialog
        {
            Title = "Export evolution log",
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"evolution_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };
        if (dlg.ShowDialog() != true) return;
        File.WriteAllText(dlg.FileName, _log.ToCSV());
        MessageBox.Show($"Saved to:\n{dlg.FileName}", "Export complete",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
