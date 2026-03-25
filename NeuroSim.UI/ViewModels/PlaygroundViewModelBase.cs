// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/ViewModels/PlaygroundViewModelBase.cs
using System.Windows;

namespace NeuroSim.UI.ViewModels;

/// <summary>
/// Abstract base for all playground ViewModels.
///
/// POC PATTERN — every new playground (Maze, BinPacking, JobShop, …) should:
///   1. Extend this class
///   2. Override RunEvolutionAsync  — hook the problem-specific evolver
///   3. Override ResetForNewRun     — clear plots / display state
///   4. Override TotalGenerations   — so ProgressPct auto-updates
///   5. Optionally override OnRunComplete for post-run work
///
/// What you get for free:
///   • IsRunning / IsNotRunning / IsEditable (bindable)
///   • Status string (bindable)
///   • ProgressPct 0–100 (bindable)
///   • SpeedDelayMs / SpeedLabel (bindable, slider-ready)
///   • StopCommand (cancels the run)
///   • OnUiThread(action) — eliminates Dispatcher.Invoke boilerplate
///   • StartEvolution() — the safe async run wrapper with try/catch/finally
///
/// SNAPSHOT PATTERN (for Maze, future playgrounds):
///   Add SaveSnapshotCommand, LoadSnapshotCommand. Define a snapshot DTO in
///   NeuroSim.UI.Models (e.g. MazeExperimentSnapshot). Serialise config +
///   problem state + per-generation history to JSON. Load restores all and
///   replays history into the charts.
///
/// Migration note for TspPlaygroundViewModel:
///   TspPlaygroundViewModel was written before this base existed.
///   It contains inline copies of all the logic above.
///   Migrate it by extending PlaygroundViewModelBase and deleting the
///   duplicated fields/commands — no behaviour changes needed.
/// </summary>
public abstract class PlaygroundViewModelBase : ViewModelBase
{
    protected readonly NavigationService _nav;
    protected CancellationTokenSource? _cts;

    // ── Running state ─────────────────────────────────────────────────────

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set { Set(ref _isRunning, value); Notify(nameof(IsNotRunning)); Notify(nameof(IsEditable)); }
    }
    public bool IsNotRunning => !_isRunning;
    public bool IsEditable   => !_isRunning;

    // ── Status / progress ─────────────────────────────────────────────────

    private string _status = "Ready.";
    public string Status { get => _status; set => Set(ref _status, value); }

    private double _progressPct;
    public double ProgressPct { get => _progressPct; set => Set(ref _progressPct, value); }

    // ── Speed control (shared by all playgrounds) ─────────────────────────

    private int _speedDelayMs = 50;
    public int SpeedDelayMs
    {
        get => _speedDelayMs;
        set { Set(ref _speedDelayMs, Math.Clamp(value, 0, 2000)); Notify(nameof(SpeedLabel)); }
    }
    public string SpeedLabel => _speedDelayMs == 0 ? "Full speed"
                              : _speedDelayMs < 100 ? $"{_speedDelayMs} ms / gen"
                              : $"{_speedDelayMs / 1000.0:F1} s / gen";

    // ── Commands ──────────────────────────────────────────────────────────

    /// <summary>Stop button — works for any playground, no override needed.</summary>
    public RelayCommand StopCommand { get; }

    // ── Constructor ───────────────────────────────────────────────────────

    protected PlaygroundViewModelBase(NavigationService nav)
    {
        _nav = nav;
        StopCommand = new RelayCommand(() => _cts?.Cancel(), () => IsRunning);
    }

    // ── Abstract contract — implement these in each playground ────────────

    /// <summary>Core evolution loop. Awaited inside a safe try/catch/finally.</summary>
    protected abstract Task RunEvolutionAsync(CancellationToken ct);

    /// <summary>Clear charts, route display, stats — called before each run.</summary>
    protected abstract void ResetForNewRun();

    /// <summary>Used to auto-compute ProgressPct. Return MaxGenerations.</summary>
    protected abstract int TotalGenerations { get; }

    // ── Template method — call from the concrete RunCommand ───────────────

    /// <summary>
    /// Safe async run shell. Call this from your concrete class's RunCommand:
    ///   RunCommand = new RelayCommand(() => StartEvolution(), () => !IsRunning);
    /// </summary>
    protected async void StartEvolution()
    {
        IsRunning  = true;
        ProgressPct = 0;
        ResetForNewRun();
        _cts = new CancellationTokenSource();

        try   { await RunEvolutionAsync(_cts.Token); }
        catch (OperationCanceledException) { Status = "Stopped."; }
        finally
        {
            IsRunning = false;
            _cts?.Dispose();
            _cts = null;
            ProgressPct = 100;
            OnRunComplete();
        }
    }

    /// <summary>Called in the finally block after every run. Override for post-run work.</summary>
    protected virtual void OnRunComplete() { }

    // ── Helpers available to all subclasses ───────────────────────────────

    /// <summary>
    /// Marshal an action to the UI thread.
    /// Replaces the repetitive Application.Current.Dispatcher.Invoke(...) pattern.
    /// </summary>
    protected void OnUiThread(Action action) =>
        Application.Current.Dispatcher.Invoke(action);

    /// <summary>Updates ProgressPct based on current generation.</summary>
    protected void UpdateProgress(int gen) =>
        ProgressPct = TotalGenerations > 0 ? gen * 100.0 / TotalGenerations : 0;
}
