// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/Views/GraphCanvasView.xaml.cs
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using NeuroSim.Engine.Genomes;

namespace NeuroSim.UI.Views;

/// <summary>Renders a GraphGenome using a circular-layout (nodes on a circle).</summary>
public partial class GraphCanvasView : UserControl
{
    public static readonly DependencyProperty GenomeProperty =
        DependencyProperty.Register(nameof(Genome), typeof(GraphGenome), typeof(GraphCanvasView),
            new PropertyMetadata(null, (d, _) => ((GraphCanvasView)d).Render()));

    public GraphGenome? Genome
    {
        get => (GraphGenome?)GetValue(GenomeProperty);
        set => SetValue(GenomeProperty, value);
    }

    public GraphCanvasView() => InitializeComponent();

    private static readonly Brush NodeFill  = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x30));
    private static readonly Brush NodeEdge  = new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8));
    private static readonly Brush EdgePen   = new SolidColorBrush(Color.FromRgb(0x7C, 0x3A, 0xED));
    private static readonly Brush TextBrush = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9));

    static GraphCanvasView()
    {
        ((SolidColorBrush)NodeFill).Freeze(); ((SolidColorBrush)NodeEdge).Freeze();
        ((SolidColorBrush)EdgePen).Freeze();  ((SolidColorBrush)TextBrush).Freeze();
    }

    public void Render()
    {
        DrawCanvas.Children.Clear();
        if (Genome is null) { EmptyLabel.Visibility = Visibility.Visible; return; }
        EmptyLabel.Visibility = Visibility.Collapsed;

        int n = Genome.NodeCount;
        if (n == 0) return;

        double w = DrawCanvas.ActualWidth;
        double h = DrawCanvas.ActualHeight;
        if (w < 1 || h < 1) return;

        double cx = w / 2, cy = h / 2;
        double r = Math.Min(cx, cy) * 0.78;
        const double nr = 16;

        // Node positions on circle
        var pos = Enumerable.Range(0, n)
            .Select(i => new Point(cx + r * Math.Cos(2 * Math.PI * i / n - Math.PI / 2),
                                   cy + r * Math.Sin(2 * Math.PI * i / n - Math.PI / 2)))
            .ToArray();

        var pen = new Pen(EdgePen, 1.4); pen.Freeze();

        // Draw edges
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
                if (Genome.HasEdge(i, j))
                    DrawCanvas.Children.Add(new Line
                    {
                        X1 = pos[i].X, Y1 = pos[i].Y,
                        X2 = pos[j].X, Y2 = pos[j].Y,
                        Stroke = EdgePen, StrokeThickness = 1.3, Opacity = 0.7
                    });

        // Draw nodes
        for (int i = 0; i < n; i++)
        {
            var ell = new Ellipse
            {
                Width = nr * 2, Height = nr * 2,
                Fill = NodeFill, Stroke = NodeEdge, StrokeThickness = 1.5
            };
            Canvas.SetLeft(ell, pos[i].X - nr);
            Canvas.SetTop(ell,  pos[i].Y - nr);
            DrawCanvas.Children.Add(ell);

            var tb = new TextBlock { Text = $"{i}", FontSize = 10, Foreground = TextBrush,
                FontWeight = FontWeights.SemiBold };
            tb.Measure(new Size(50, 50));
            Canvas.SetLeft(tb, pos[i].X - tb.DesiredSize.Width / 2);
            Canvas.SetTop(tb,  pos[i].Y - tb.DesiredSize.Height / 2);
            DrawCanvas.Children.Add(tb);
        }

        // Edge count label
        int edges = 0;
        for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) if (Genome.HasEdge(i, j)) edges++;
        var info = new TextBlock
        {
            Text = $"{n} nodes  ·  {edges} edges",
            Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)),
            FontSize = 10, FontFamily = new FontFamily("Segoe UI")
        };
        Canvas.SetLeft(info, 8); Canvas.SetTop(info, 8);
        DrawCanvas.Children.Add(info);
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo info) { base.OnRenderSizeChanged(info); Render(); }
}
