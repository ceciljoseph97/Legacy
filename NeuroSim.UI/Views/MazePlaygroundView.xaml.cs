// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/Views/MazePlaygroundView.xaml.cs
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NeuroSim.Problems.Maze;
using NeuroSim.UI.Helpers;
using NeuroSim.UI.ViewModels;

namespace NeuroSim.UI.Views;

// ── Code-behind: wires ViewModel → MazeCanvas ────────────────────────────────

public partial class MazePlaygroundView : System.Windows.Controls.UserControl
{
    private void ExportMazeBtn_OnClick(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as MazePlaygroundViewModel;
        var name = vm != null ? $"{vm.SnapshotName}_{DateTime.Now:yyyyMMdd_HHmm}" : "Maze";
        if (ExportHelper.ExportVisual(MazeCanvasCtrl, name))
            vm!.Status = "Maze exported to PNG.";
    }

    public MazePlaygroundView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private MazePlaygroundViewModel? _vm;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm != null) _vm.PropertyChanged -= VmOnPropertyChanged;

        _vm = DataContext as MazePlaygroundViewModel;
        if (_vm == null) return;

        MazeCanvasCtrl.Maze      = _vm.Maze;
        MazeCanvasCtrl.AgentPath = _vm.AgentPath;

        _vm.PropertyChanged += VmOnPropertyChanged;
    }

    private void VmOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MazePlaygroundViewModel.Maze):
                MazeCanvasCtrl.Maze = _vm?.Maze;
                break;
            case nameof(MazePlaygroundViewModel.AgentPath):
                MazeCanvasCtrl.AgentPath = _vm?.AgentPath;
                break;
        }
    }
}

// ── MazeCanvas ────────────────────────────────────────────────────────────────
// Renders maze grid + agent path. Handles mouse click → cell (x,y) for editing.

public sealed class MazeCanvas : System.Windows.Controls.Control
{
    private static readonly Brush WallBrush   = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B));
    private static readonly Brush OpenBrush   = new SolidColorBrush(Color.FromRgb(0x09, 0x09, 0x0F));
    private static readonly Brush StartBrush  = new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E));
    private static readonly Brush GoalBrush   = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
    private static readonly Brush GridLinePen = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E));
    private static readonly Pen   PathPen     = new(new SolidColorBrush(Color.FromRgb(0xA7, 0x8B, 0xFA)), 2)
                                                { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };

    static MazeCanvas()
    {
        WallBrush.Freeze(); OpenBrush.Freeze(); StartBrush.Freeze();
        GoalBrush.Freeze(); GridLinePen.Freeze(); PathPen.Freeze();
    }

    public MazeCanvas()
    {
        Focusable = true;
    }

    private MazeProblem? _maze;
    public MazeProblem? Maze
    {
        get => _maze;
        set { _maze = value; InvalidateVisual(); }
    }

    private (int X, int Y)[]? _agentPath;
    public (int X, int Y)[]? AgentPath
    {
        get => _agentPath;
        set { _agentPath = value; InvalidateVisual(); }
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (_maze == null) return;

        var pos = e.GetPosition(this);
        double w = ActualWidth, h = ActualHeight;
        double cellW = w / _maze.Width;
        double cellH = h / _maze.Height;
        double cell  = Math.Min(cellW, cellH);
        double offX  = (w - cell * _maze.Width)  / 2;
        double offY  = (h - cell * _maze.Height) / 2;

        int ix = (int)Math.Floor((pos.X - offX) / cell);
        int iy = (int)Math.Floor((pos.Y - offY) / cell);

        if (ix >= 0 && ix < _maze.Width && iy >= 0 && iy < _maze.Height)
        {
            var vm = DataContext as MazePlaygroundViewModel;
            vm?.OnCellClicked(ix, iy);
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        dc.DrawRectangle(OpenBrush, null, new Rect(0, 0, w, h));

        if (_maze == null) { DrawPlaceholder(dc, w, h); return; }

        double cellW = w / _maze.Width;
        double cellH = h / _maze.Height;
        double cell  = Math.Min(cellW, cellH);
        double offX  = (w - cell * _maze.Width)  / 2;
        double offY  = (h - cell * _maze.Height) / 2;

        for (int y = 0; y < _maze.Height; y++)
            for (int x = 0; x < _maze.Width; x++)
            {
                var rect = new Rect(offX + x * cell, offY + y * cell, cell, cell);
                dc.DrawRectangle(_maze.Walls[x, y] ? WallBrush : OpenBrush, null, rect);
            }

        var gridPen = new Pen(GridLinePen, 0.3);
        for (int y = 0; y <= _maze.Height; y++)
            dc.DrawLine(gridPen,
                new Point(offX, offY + y * cell),
                new Point(offX + _maze.Width * cell, offY + y * cell));
        for (int x = 0; x <= _maze.Width; x++)
            dc.DrawLine(gridPen,
                new Point(offX + x * cell, offY),
                new Point(offX + x * cell, offY + _maze.Height * cell));

        if (_agentPath is { Length: > 1 })
        {
            for (int i = 0; i < _agentPath.Length - 1; i++)
            {
                var (ax, ay) = _agentPath[i];
                var (bx, by) = _agentPath[i + 1];
                dc.DrawLine(PathPen,
                    new Point(offX + (ax + 0.5) * cell, offY + (ay + 0.5) * cell),
                    new Point(offX + (bx + 0.5) * cell, offY + (by + 0.5) * cell));
            }
            var (hx, hy) = _agentPath[^1];
            double hr = Math.Max(2, cell * 0.35);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)), null,
                new Point(offX + (hx + 0.5) * cell, offY + (hy + 0.5) * cell), hr, hr);
        }

        double r = Math.Max(3, cell * 0.4);
        dc.DrawEllipse(StartBrush, null,
            new Point(offX + (_maze.Start.X + 0.5) * cell, offY + (_maze.Start.Y + 0.5) * cell), r, r);
        dc.DrawEllipse(GoalBrush, null,
            new Point(offX + (_maze.Goal.X + 0.5) * cell, offY + (_maze.Goal.Y + 0.5) * cell), r, r);

        var label = new FormattedText("S", System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, new Typeface("Segoe UI"), cell * 0.5, Brushes.White, 1);
        dc.DrawText(label, new Point(
            offX + (_maze.Start.X + 0.5) * cell - label.Width / 2,
            offY + (_maze.Start.Y + 0.5) * cell - label.Height / 2));

        var gLabel = new FormattedText("G", System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, new Typeface("Segoe UI"), cell * 0.5, Brushes.White, 1);
        dc.DrawText(gLabel, new Point(
            offX + (_maze.Goal.X + 0.5) * cell - gLabel.Width / 2,
            offY + (_maze.Goal.Y + 0.5) * cell - gLabel.Height / 2));
    }

    private static void DrawPlaceholder(DrawingContext dc, double w, double h)
    {
        var text = new FormattedText(
            "Select a preset or New Blank, then click cells to edit (Start / Goal / Wall)",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), 13,
            new SolidColorBrush(Color.FromRgb(0x47, 0x55, 0x69)), 1);
        dc.DrawText(text, new Point((w - text.Width) / 2, (h - text.Height) / 2));
    }
}
