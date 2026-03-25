// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/Views/GraphPlaygroundView.xaml.cs
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NeuroSim.UI.ViewModels;

namespace NeuroSim.UI.Views;

public partial class GraphPlaygroundView : UserControl
{
    public GraphPlaygroundView() => InitializeComponent();
}

// ── DagEditorCanvas ──────────────────────────────────────────────────────────
// Interactive DAG editor: click to add node, drag between nodes to add edge.

public sealed class DagEditorCanvas : FrameworkElement
{
    // ── Dependency Properties ─────────────────────────────────────────────
    public static readonly DependencyProperty NodesProperty =
        DependencyProperty.Register(nameof(Nodes), typeof(ObservableCollection<GraphNodeVm>),
            typeof(DagEditorCanvas), new FrameworkPropertyMetadata(null, FMO, OnCollectionChanged));

    public static readonly DependencyProperty EdgesProperty =
        DependencyProperty.Register(nameof(Edges), typeof(ObservableCollection<GraphEdgeVm>),
            typeof(DagEditorCanvas), new FrameworkPropertyMetadata(null, FMO, OnCollectionChanged));

    public static readonly DependencyProperty EvolvedEdgesProperty =
        DependencyProperty.Register(nameof(EvolvedEdges), typeof(ObservableCollection<GraphEdgeVm>),
            typeof(DagEditorCanvas), new FrameworkPropertyMetadata(null, FMO, OnCollectionChanged));

    public static readonly DependencyProperty IsEditableProperty =
        DependencyProperty.Register(nameof(IsEditable), typeof(bool), typeof(DagEditorCanvas),
            new PropertyMetadata(true));

    public static readonly DependencyProperty AddEdgeModeProperty =
        DependencyProperty.Register(nameof(AddEdgeMode), typeof(bool), typeof(DagEditorCanvas),
            new PropertyMetadata(false));

    private static FrameworkPropertyMetadataOptions FMO =>
        FrameworkPropertyMetadataOptions.AffectsRender;

    private static void OnCollectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs _)
        => ((DagEditorCanvas)d).BindAll();

    public ObservableCollection<GraphNodeVm>?  Nodes        { get => (ObservableCollection<GraphNodeVm>?)GetValue(NodesProperty);       set => SetValue(NodesProperty, value); }
    public ObservableCollection<GraphEdgeVm>?  Edges        { get => (ObservableCollection<GraphEdgeVm>?)GetValue(EdgesProperty);       set => SetValue(EdgesProperty, value); }
    public ObservableCollection<GraphEdgeVm>?  EvolvedEdges { get => (ObservableCollection<GraphEdgeVm>?)GetValue(EvolvedEdgesProperty); set => SetValue(EvolvedEdgesProperty, value); }
    public bool IsEditable   { get => (bool)GetValue(IsEditableProperty);   set => SetValue(IsEditableProperty, value); }
    public bool AddEdgeMode  { get => (bool)GetValue(AddEdgeModeProperty);  set => SetValue(AddEdgeModeProperty, value); }

    // ── Interaction state ─────────────────────────────────────────────────
    private int  _dragNodeId  = -1;
    private int  _edgeSrcId   = -1;   // for edge drawing
    private bool _isDragging;
    private Point _edgeEndPt;          // current mouse position for in-progress edge

    // ── Brushes / pens ────────────────────────────────────────────────────
    private static readonly Brush BgBrush       = Frozen(Color.FromRgb(0x0A, 0x0A, 0x0F));
    private static readonly Brush NodeFill      = Frozen(Color.FromRgb(0x1C, 0x1C, 0x2E));
    private static readonly Brush NodeStroke    = Frozen(Color.FromRgb(0xA7, 0x8B, 0xFA));
    private static readonly Brush UserEdgePen   = Frozen(Color.FromRgb(0xA7, 0x8B, 0xFA));
    private static readonly Brush EvoEdgePen    = Frozen(Color.FromRgb(0x38, 0xBD, 0xF8));
    private static readonly Brush TempEdgePen   = Frozen(Color.FromRgb(0xF5, 0x9E, 0x0B));
    private static readonly Brush LabelBrush    = Frozen(Color.FromRgb(0xF1, 0xF5, 0xF9));
    private static readonly Brush GridBrush     = Frozen(Color.FromArgb(15, 0xFF, 0xFF, 0xFF));

    private static SolidColorBrush Frozen(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }

    private const double NodeR = 18;

    public DagEditorCanvas()
    {
        ClipToBounds = true;
        DataContextChanged += (_, _) => InvalidateVisual();
    }

    private void BindAll()
    {
        if (Nodes        is not null) { Nodes.CollectionChanged        += (_, _) => InvalidateVisual(); foreach (var n in Nodes) n.PropertyChanged += (_, _) => InvalidateVisual(); }
        if (Edges        is not null) Edges.CollectionChanged        += (_, _) => InvalidateVisual();
        if (EvolvedEdges is not null) EvolvedEdges.CollectionChanged  += (_, _) => InvalidateVisual();
        InvalidateVisual();
    }

    // ── Hit-test ──────────────────────────────────────────────────────────
    private int HitNode(Point p)
    {
        if (Nodes is null) return -1;
        foreach (var n in Nodes)
            if ((new Point(n.X, n.Y) - p).Length <= NodeR + 4) return n.Id;
        return -1;
    }

    private GraphPlaygroundViewModel? GetVM() => DataContext as GraphPlaygroundViewModel;

    // ── Mouse ─────────────────────────────────────────────────────────────
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (!IsEditable) return;
        var pos = e.GetPosition(this);
        int hit = HitNode(pos);

        if (AddEdgeMode && hit >= 0)
        {
            _edgeSrcId  = hit;
            _edgeEndPt  = pos;
            _isDragging = true;
            CaptureMouse();
        }
        else if (!AddEdgeMode && hit >= 0)
        {
            _dragNodeId = hit;
            _isDragging = false;
            CaptureMouse();
        }
        else if (!AddEdgeMode && hit < 0)
        {
            GetVM()?.AddNode(pos.X, pos.Y);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var pos = e.GetPosition(this);

        if (AddEdgeMode && _edgeSrcId >= 0)
        {
            _edgeEndPt = pos;
            InvalidateVisual();
        }
        else if (!AddEdgeMode && _dragNodeId >= 0)
        {
            _isDragging = true;
            GetVM()?.MoveNode(_dragNodeId, pos.X, pos.Y);
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        var pos = e.GetPosition(this);

        if (AddEdgeMode && _edgeSrcId >= 0)
        {
            int hit = HitNode(pos);
            if (hit >= 0 && hit != _edgeSrcId)
                GetVM()?.AddEdge(_edgeSrcId, hit);
            _edgeSrcId  = -1;
            _isDragging = false;
            InvalidateVisual();
        }

        _dragNodeId = -1;
        _isDragging = false;
        ReleaseMouseCapture();
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);
        if (!IsEditable) return;
        int hit = HitNode(e.GetPosition(this));
        if (hit >= 0) GetVM()?.RemoveNode(hit);
    }

    // ── Render ────────────────────────────────────────────────────────────
    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        if (w < 1 || h < 1) return;

        dc.DrawRectangle(BgBrush, null, new Rect(0, 0, w, h));

        // Grid
        var gp = new Pen(GridBrush, 0.5); gp.Freeze();
        for (int i = 1; i < 10; i++)
        {
            dc.DrawLine(gp, new Point(w * i / 10, 0), new Point(w * i / 10, h));
            dc.DrawLine(gp, new Point(0, h * i / 10), new Point(w, h * i / 10));
        }

        if (Nodes is null or { Count: 0 })
        {
            DrawHint(dc, w, h);
            return;
        }

        var nodeLookup = Nodes.ToDictionary(n => n.Id, n => new Point(n.X, n.Y));

        // User edges
        if (Edges is not null)
            foreach (var e in Edges)
                DrawArrow(dc, nodeLookup, e.From, e.To, UserEdgePen, 1.5);

        // Evolved edges
        if (EvolvedEdges is not null)
            foreach (var e in EvolvedEdges)
                DrawArrow(dc, nodeLookup, e.From, e.To, EvoEdgePen, 1.8, dashed: true);

        // In-progress edge
        if (_edgeSrcId >= 0 && nodeLookup.TryGetValue(_edgeSrcId, out var srcPt))
        {
            var pen = new Pen(TempEdgePen, 1.4) { DashStyle = DashStyles.Dash }; pen.Freeze();
            dc.DrawLine(pen, srcPt, _edgeEndPt);
        }

        // Nodes on top
        foreach (var node in Nodes)
        {
            var p = new Point(node.X, node.Y);
            bool active = node.Id == _dragNodeId || node.Id == _edgeSrcId;
            var stroke = new Pen(active ? new SolidColorBrush(Colors.White) : NodeStroke, active ? 2 : 1.5);
            stroke.Freeze();

            dc.DrawEllipse(NodeFill, stroke, p, NodeR, NodeR);

            var ft = new FormattedText(node.Label,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
                11, LabelBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(ft, new Point(p.X - ft.Width / 2, p.Y - ft.Height / 2));
        }
    }

    private void DrawArrow(DrawingContext dc, Dictionary<int, Point> nodes,
        int fromId, int toId, Brush brush, double thickness, bool dashed = false)
    {
        if (!nodes.TryGetValue(fromId, out var a) || !nodes.TryGetValue(toId, out var b)) return;

        // Shorten to node radius
        var dir = b - a;
        double len = dir.Length;
        if (len < 1) return;
        dir /= len;

        var start = new Point(a.X + dir.X * NodeR, a.Y + dir.Y * NodeR);
        var end   = new Point(b.X - dir.X * NodeR, b.Y - dir.Y * NodeR);

        var pen = new Pen(brush, thickness);
        if (dashed) pen.DashStyle = DashStyles.DashDot;
        pen.Freeze();
        dc.DrawLine(pen, start, end);

        // Arrowhead
        double aLen = 9, aHalf = 4;
        var perp = new Vector(-dir.Y, dir.X);
        var tip = end;
        var left  = new Point(end.X - dir.X * aLen + perp.X * aHalf, end.Y - dir.Y * aLen + perp.Y * aHalf);
        var right = new Point(end.X - dir.X * aLen - perp.X * aHalf, end.Y - dir.Y * aLen - perp.Y * aHalf);
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            ctx.BeginFigure(tip, true, true);
            ctx.LineTo(left, true, false);
            ctx.LineTo(right, true, false);
        }
        geo.Freeze();
        dc.DrawGeometry(brush as SolidColorBrush ?? Brushes.Gray, null, geo);
    }

    private void DrawHint(DrawingContext dc, double w, double h)
    {
        var ft = new FormattedText("Click to add nodes · Toggle mode to draw edges",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, new Typeface("Segoe UI"), 13,
            new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x3E)),
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(ft, new Point(w / 2 - ft.Width / 2, h / 2));
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo info) { base.OnRenderSizeChanged(info); InvalidateVisual(); }
}
