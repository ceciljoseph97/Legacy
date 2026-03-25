// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/Views/TspPlaygroundView.xaml.cs
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NeuroSim.UI.Helpers;
using NeuroSim.UI.ViewModels;

namespace NeuroSim.UI.Views;

public partial class TspPlaygroundView : UserControl
{
    public TspPlaygroundView()
    {
        InitializeComponent();
        MainTabCtrl.SelectionChanged += OnTabChanged;
        Loaded += (_, _) => WireViewModel();
    }

    private void ExportMapBtn_OnClick(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as TspPlaygroundViewModel;
        var name = vm != null ? $"{vm.SnapshotName}_{DateTime.Now:yyyyMMdd_HHmm}" : "TSP_Map";
        if (ExportHelper.ExportVisual(TspCanvas, name))
            vm!.Status = "Map exported to PNG.";
    }

    private TspPlaygroundViewModel? _wiredVm;
    private void WireViewModel()
    {
        if (DataContext is TspPlaygroundViewModel vm && vm != _wiredVm)
        {
            _wiredVm = vm;
            vm.RequestCityDialog += (x, y) => TspCanvas.OpenCityWizard(x, y);
        }
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property.Name == "DataContext")
            WireViewModel();
    }

    private void OnTabChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MainTabCtrl.SelectedItem is not TabItem tab) return;
        MapPane.Visibility      = tab.Tag?.ToString() == "map"      ? Visibility.Visible : Visibility.Collapsed;
        AnalysisPane.Visibility = tab.Tag?.ToString() == "analysis"  ? Visibility.Visible : Visibility.Collapsed;
        RefPane.Visibility      = tab.Tag?.ToString() == "ref"       ? Visibility.Visible : Visibility.Collapsed;
    }

}

// ─────────────────────────────────────────────────────────────────────────────
//  Interactive TSP Canvas
// ─────────────────────────────────────────────────────────────────────────────

public sealed class InteractiveTspCanvas : FrameworkElement
{
    // ── Geometry constants ────────────────────────────────────────────────
    private const double CityR   = 7;
    private const double HitR    = 12;
    private const double Pad     = 36;

    // ── Pens and brushes (allocated once) ─────────────────────────────────
    private static readonly Brush CityFill  = new SolidColorBrush(Color.FromRgb(0x7C, 0x3A, 0xED));
    private static readonly Brush CityHover = new SolidColorBrush(Color.FromRgb(0xA7, 0x8B, 0xFA));
    private static readonly Pen   CityPen   = new(new SolidColorBrush(Color.FromRgb(0xC4, 0xB5, 0xFD)), 1.5);
    private static readonly Pen   GridPen   = new(new SolidColorBrush(Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF)), 0.5);

    // Route pens generated dynamically based on heat (short=green, long=red)
    private static readonly Pen DefaultRoutePen = new(new SolidColorBrush(Color.FromRgb(0xA7, 0x8B, 0xFA)), 1.5);
    private static readonly Pen GoodEdgePen     = new(new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E)), 1.8);
    private static readonly Pen BadEdgePen      = new(new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)), 1.8);

    static InteractiveTspCanvas()
    {
        CityFill.Freeze(); CityHover.Freeze(); CityPen.Freeze();
        GridPen.Freeze();  DefaultRoutePen.Freeze();
        GoodEdgePen.Freeze(); BadEdgePen.Freeze();
    }

    // ── Drag state ────────────────────────────────────────────────────────
    private int    _dragIdx    = -1;
    private int    _hoverIdx   = -1;
    private Point  _dragOffset;

    // ── Context menu ──────────────────────────────────────────────────────
    private Point  _rightClickPos;

    public static readonly DependencyProperty BackgroundProperty =
        DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(InteractiveTspCanvas),
            new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush Background
    {
        get => (Brush)GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public InteractiveTspCanvas()
    {
        SnapsToDevicePixels = true;
        Focusable = true;
        BuildContextMenu();
    }

    // ── Context menu setup ────────────────────────────────────────────────

    private MenuItem _menuRemove = null!, _menuEdit = null!, _menuAddCity = null!;

    private void BuildContextMenu()
    {
        var cm = new ContextMenu();
        cm.Background    = new SolidColorBrush(Color.FromRgb(0x0F, 0x0F, 0x18));
        cm.BorderBrush   = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x3E));
        cm.Foreground    = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
        cm.FontSize      = 12;

        _menuAddCity     = MakeMenuItem("＋  Add City Here…",  OnAddCity);
        _menuEdit        = MakeMenuItem("✎   Edit Name…",       OnEditCity);
        _menuRemove      = MakeMenuItem("✕   Remove City",       OnRemoveCity);
        var menuClear    = MakeMenuItem("⊘   Clear All Cities",  OnClearAll);

        cm.Items.Add(_menuAddCity);
        cm.Items.Add(new Separator { Background = new SolidColorBrush(Color.FromRgb(0x1E,0x1E,0x2E)) });
        cm.Items.Add(_menuEdit);
        cm.Items.Add(_menuRemove);
        cm.Items.Add(new Separator { Background = new SolidColorBrush(Color.FromRgb(0x1E,0x1E,0x2E)) });
        cm.Items.Add(menuClear);
        ContextMenu = cm;

        ContextMenuOpening += OnContextMenuOpening;
    }

    private static MenuItem MakeMenuItem(string header, RoutedEventHandler handler)
    {
        var mi = new MenuItem { Header = header };
        mi.Click += handler;
        mi.Foreground = new SolidColorBrush(Color.FromRgb(0xE2,0xE8,0xF0));
        mi.Background = Brushes.Transparent;
        return mi;
    }

    private void OnContextMenuOpening(object? sender, ContextMenuEventArgs e)
    {
        int idx = HitTest(_rightClickPos);
        _menuEdit.IsEnabled   = idx >= 0;
        _menuRemove.IsEnabled = idx >= 0;
    }

    // ── Context menu handlers ──────────────────────────────────────────────

    private void OnAddCity(object sender, RoutedEventArgs e)
    {
        var vm = GetViewModel(); if (vm is null || !vm.IsEditable) return;
        var rawPos = CanvasToWorld(_rightClickPos);
        string suggest = $"C{vm.Cities.Count + 1}";

        var dlg = new CityWizardDialog(suggest, rawPos.X, rawPos.Y)
        {
            Owner = Window.GetWindow(this)
        };
        dlg.ShowDialog();
        if (dlg.Confirmed) vm.AddCity(dlg.X, dlg.Y, dlg.CityName);
        InvalidateVisual();
    }

    private void OnEditCity(object sender, RoutedEventArgs e)
    {
        var vm = GetViewModel(); if (vm is null) return;
        int idx = HitTest(_rightClickPos); if (idx < 0) return;
        var city = vm.Cities[idx];

        var dlg = new CityWizardDialog(city.Name, city.X, city.Y)
        {
            Title = "Edit City",
            Owner = Window.GetWindow(this)
        };
        dlg.ShowDialog();
        if (dlg.Confirmed)
        {
            vm.RenameCity(idx, dlg.CityName);
            vm.MoveCity(idx, dlg.X, dlg.Y);
        }
        InvalidateVisual();
    }

    private void OnRemoveCity(object sender, RoutedEventArgs e)
    {
        var vm = GetViewModel(); if (vm is null) return;
        int idx = HitTest(_rightClickPos);
        if (idx >= 0) vm.RemoveCity(idx);
        InvalidateVisual();
    }

    private void OnClearAll(object sender, RoutedEventArgs e)
    {
        GetViewModel()?.ClearCommand.Execute(null);
        InvalidateVisual();
    }

    // ── Mouse events ──────────────────────────────────────────────────────

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        _rightClickPos = e.GetPosition(this);
        base.OnMouseRightButtonDown(e);
    }

    // Left-click: double-click on empty space → add city; single click on city → drag
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            var vm2 = GetViewModel(); if (vm2 is not null && vm2.IsEditable)
            {
                var pos2 = e.GetPosition(this);
                if (HitTest(pos2) < 0)
                {
                    var world = CanvasToWorld(pos2);
                    OpenCityWizard(world.X, world.Y);
                    e.Handled = true;
                    return;
                }
            }
        }
        // --- single click drag logic ---
        var vm = GetViewModel(); if (vm is null || !vm.IsEditable) return;
        var pos = e.GetPosition(this);
        int idx = HitTest(pos);
        if (idx >= 0)
        {
            _dragIdx = idx;
            var city = vm.Cities[idx];
            var screenCity = WorldToCanvas(new Point(city.X, city.Y));
            _dragOffset = new Point(screenCity.X - pos.X, screenCity.Y - pos.Y);
            CaptureMouse();
            e.Handled = true;
        }
        base.OnMouseLeftButtonDown(e);
    }

    // Called from code-behind AddCity button and double-click
    public void OpenCityWizard(double worldX, double worldY)
    {
        var vm = GetViewModel(); if (vm is null) return;
        string suggest = $"C{vm.Cities.Count + 1}";
        var dlg = new CityWizardDialog(suggest, worldX, worldY)
        {
            Owner = Window.GetWindow(this)
        };
        dlg.ShowDialog();
        if (dlg.Confirmed) vm.AddCity(dlg.X, dlg.Y, dlg.CityName);
        InvalidateVisual();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var vm = GetViewModel(); if (vm is null) return;
        var pos = e.GetPosition(this);

        if (_dragIdx >= 0 && e.LeftButton == MouseButtonState.Pressed)
        {
            var world = CanvasToWorld(new Point(pos.X + _dragOffset.X, pos.Y + _dragOffset.Y));
            world = new Point(Math.Clamp(world.X, 0, 1000), Math.Clamp(world.Y, 0, 1000));
            vm.MoveCity(_dragIdx, world.X, world.Y);
            InvalidateVisual();
            return;
        }

        int prev = _hoverIdx;
        _hoverIdx = HitTest(pos);
        Cursor = _hoverIdx >= 0 ? Cursors.Hand : Cursors.Cross;
        if (_hoverIdx != prev) InvalidateVisual();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (_dragIdx >= 0) { ReleaseMouseCapture(); _dragIdx = -1; }
        base.OnMouseLeftButtonUp(e);
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        _hoverIdx = -1; InvalidateVisual();
        base.OnMouseLeave(e);
    }

    // ── Rendering ─────────────────────────────────────────────────────────

    protected override void OnRender(DrawingContext dc)
    {
        var vm = GetViewModel();
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x09, 0x09, 0x0F)), null, new Rect(RenderSize));

        DrawGrid(dc);

        if (vm is null) return;

        var cities = vm.Cities.ToList();
        if (cities.Count == 0)
        {
            DrawHint(dc, "Right-click to add cities  ·  Load a preset from the config panel");
            return;
        }

        // Draw route segments
        var segs = vm.RouteSegments.ToList();
        if (segs.Count > 0)
        {
            double? maxLen = null, minLen = null;
            if (vm.ColorEdgesByLength)
            {
                var lens = segs.Select(s => Math.Sqrt(Math.Pow(s.X2 - s.X1, 2) + Math.Pow(s.Y2 - s.Y1, 2))).ToList();
                maxLen = lens.Max(); minLen = lens.Min();
            }

            foreach (var seg in segs)
            {
                var a = WorldToCanvas(new Point(seg.X1, seg.Y1));
                var b = WorldToCanvas(new Point(seg.X2, seg.Y2));

                Pen pen = DefaultRoutePen;
                if (vm.ColorEdgesByLength && maxLen.HasValue && maxLen.Value > minLen!.Value)
                {
                    double len = Math.Sqrt(Math.Pow(seg.X2 - seg.X1, 2) + Math.Pow(seg.Y2 - seg.Y1, 2));
                    double t   = (len - minLen!.Value) / (maxLen.Value - minLen.Value);
                    pen = MakeHeatPen(t);
                }
                dc.DrawLine(pen, a, b);

                // Edge distance label
                if (vm.ShowEdgeDistances)
                {
                    double d    = Math.Sqrt(Math.Pow(seg.X2 - seg.X1, 2) + Math.Pow(seg.Y2 - seg.Y1, 2));
                    var mid = new Point((a.X + b.X) / 2, (a.Y + b.Y) / 2);
                    DrawSmallText(dc, $"{d:F0}", mid, Color.FromArgb(0xAA, 0x94, 0xA3, 0xB8));
                }
            }
        }

        // Draw cities
        for (int i = 0; i < cities.Count; i++)
        {
            var sc = WorldToCanvas(new Point(cities[i].X, cities[i].Y));
            bool hover = i == _hoverIdx;
            var fill   = hover ? CityHover : CityFill;
            dc.DrawEllipse(fill, CityPen, sc, CityR, CityR);

            // City label
            DrawSmallText(dc, cities[i].Name, new Point(sc.X + CityR + 3, sc.Y - 7),
                Color.FromRgb(0xF1, 0xF5, 0xF9));
        }

        // Best distance overlay
        if (vm.BestTour.Length == cities.Count && !string.IsNullOrEmpty(vm.BestDistStr) && vm.BestDistStr != "–")
        {
            DrawOverlay(dc, $"Best: {vm.BestDistStr}  ·  Gen: {vm.CurrentGeneration}");
        }
    }

    // ── Draw helpers ──────────────────────────────────────────────────────

    private void DrawGrid(DrawingContext dc)
    {
        double step = (ActualWidth - 2 * Pad) / 10;
        for (double x = Pad; x <= ActualWidth - Pad + 1; x += step)
            dc.DrawLine(GridPen, new Point(x, Pad), new Point(x, ActualHeight - Pad));
        for (double y = Pad; y <= ActualHeight - Pad + 1; y += step)
            dc.DrawLine(GridPen, new Point(Pad, y), new Point(ActualWidth - Pad, y));
    }

    private void DrawHint(DrawingContext dc, string text)
    {
        var tf = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
            13, new SolidColorBrush(Color.FromRgb(0x33, 0x3D, 0x52)), VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(tf, new Point((ActualWidth - tf.Width) / 2, (ActualHeight - tf.Height) / 2));
    }

    private void DrawSmallText(DrawingContext dc, string text, Point origin, Color color)
    {
        var tf = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
            9.5, new SolidColorBrush(color), VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(tf, origin);
    }

    private void DrawOverlay(DrawingContext dc, string text)
    {
        var tf = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
            11, new SolidColorBrush(Color.FromRgb(0xA7, 0x8B, 0xFA)), VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(tf, new Point(Pad, ActualHeight - Pad + 6));
    }

    private static Pen MakeHeatPen(double t)
    {
        // t=0 → green, t=1 → red
        byte r = (byte)(t * 239 + (1 - t) * 34);
        byte g = (byte)((1 - t) * 197 + t * 68);
        byte b = (byte)((1 - t) * 94 + t * 68);
        var p = new Pen(new SolidColorBrush(Color.FromRgb(r, g, b)), 1.8);
        p.Freeze();
        return p;
    }

    // ── Coordinate transforms ─────────────────────────────────────────────
    // World space: 0..1000 × 0..1000  Canvas space: Pad..Width-Pad, Pad..Height-Pad

    private Point WorldToCanvas(Point world)
    {
        double w = ActualWidth - 2 * Pad, h = ActualHeight - 2 * Pad;
        if (w <= 0 || h <= 0) return new Point(Pad, Pad);
        return new Point(Pad + world.X / 1000.0 * w, Pad + (1.0 - world.Y / 1000.0) * h);
    }

    private Point CanvasToWorld(Point canvas)
    {
        double w = ActualWidth - 2 * Pad, h = ActualHeight - 2 * Pad;
        if (w <= 0 || h <= 0) return new Point(500, 500);
        return new Point(
            Math.Clamp((canvas.X - Pad) / w * 1000, 0, 1000),
            Math.Clamp((1.0 - (canvas.Y - Pad) / h) * 1000, 0, 1000));
    }

    private int HitTest(Point screen)
    {
        var vm = GetViewModel(); if (vm is null) return -1;
        var cities = vm.Cities.ToList();
        for (int i = 0; i < cities.Count; i++)
        {
            var sc = WorldToCanvas(new Point(cities[i].X, cities[i].Y));
            double dx = sc.X - screen.X, dy = sc.Y - screen.Y;
            if (dx * dx + dy * dy <= HitR * HitR) return i;
        }
        return -1;
    }

    // ── DataContext helpers ────────────────────────────────────────────────

    private TspPlaygroundViewModel? GetViewModel() => DataContext as TspPlaygroundViewModel;

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property.Name == "DataContext" && e.NewValue is TspPlaygroundViewModel vm)
        {
            vm.Cities.CollectionChanged        += (_, _) => InvalidateVisual();
            vm.RouteSegments.CollectionChanged  += (_, _) => InvalidateVisual();
            vm.PropertyChanged += (_, pe) =>
            {
                if (pe.PropertyName is nameof(TspPlaygroundViewModel.BestDistStr)
                                    or nameof(TspPlaygroundViewModel.CurrentGeneration))
                    InvalidateVisual();
            };
        }
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo info) { base.OnRenderSizeChanged(info); InvalidateVisual(); }
}
