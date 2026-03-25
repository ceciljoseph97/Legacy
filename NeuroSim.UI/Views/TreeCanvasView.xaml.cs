// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/Views/TreeCanvasView.xaml.cs
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using NeuroSim.Engine.Genomes;
using static NeuroSim.Engine.Genomes.NodeKind;

namespace NeuroSim.UI.Views;

/// <summary>Renders a TreeGenome as a recursive node diagram.</summary>
public partial class TreeCanvasView : UserControl
{
    public static readonly DependencyProperty GenomeProperty =
        DependencyProperty.Register(nameof(Genome), typeof(TreeGenome), typeof(TreeCanvasView),
            new PropertyMetadata(null, (d, _) => ((TreeCanvasView)d).Render()));

    public TreeGenome? Genome
    {
        get => (TreeGenome?)GetValue(GenomeProperty);
        set => SetValue(GenomeProperty, value);
    }

    public TreeCanvasView() => InitializeComponent();

    private const double NodeR = 18;
    private const double HGap  = 40;
    private const double VGap  = 52;

    private static readonly Brush NodeBg    = new SolidColorBrush(Color.FromRgb(0x1C, 0x1C, 0x2E));
    private static readonly Brush NodeBorder = new SolidColorBrush(Color.FromRgb(0x7C, 0x3A, 0xED));
    private static readonly Brush EdgeStroke = new SolidColorBrush(Color.FromRgb(0x3D, 0x3D, 0x52));
    private static readonly Brush TextBrush  = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9));

    static TreeCanvasView()
    {
        ((SolidColorBrush)NodeBg).Freeze(); ((SolidColorBrush)NodeBorder).Freeze();
        ((SolidColorBrush)EdgeStroke).Freeze(); ((SolidColorBrush)TextBrush).Freeze();
    }

    public void Render()
    {
        DrawCanvas.Children.Clear();
        if (Genome?.Root is null) { EmptyLabel.Visibility = Visibility.Visible; return; }
        EmptyLabel.Visibility = Visibility.Collapsed;

        var positions = new Dictionary<TreeNode, Point>();
        double[] x = { 0 };
        Layout(Genome.Root, 0, positions, x);

        // Scale to fit
        double maxX = positions.Values.Max(p => p.X);
        double maxY = positions.Values.Max(p => p.Y);
        double scaleX = DrawCanvas.ActualWidth  > 0 ? (DrawCanvas.ActualWidth  - 64) / Math.Max(maxX, 1) : 1;
        double scaleY = DrawCanvas.ActualHeight > 0 ? (DrawCanvas.ActualHeight - 64) / Math.Max(maxY, 1) : 1;
        double scale  = Math.Min(scaleX, scaleY);

        foreach (var (node, pos) in positions)
        {
            double cx = 32 + pos.X * scale;
            double cy = 32 + pos.Y * scale;

            // Draw edges to children first
            foreach (var child in node.Children)
            {
                if (!positions.TryGetValue(child, out var cp)) continue;
                double chx = 32 + cp.X * scale;
                double chy = 32 + cp.Y * scale;
                DrawCanvas.Children.Add(new Line
                {
                    X1 = cx, Y1 = cy, X2 = chx, Y2 = chy,
                    Stroke = EdgeStroke, StrokeThickness = 1.5
                });
            }

            // Node circle
            var ellipse = new Ellipse
            {
                Width = NodeR * 2, Height = NodeR * 2,
                Fill = NodeBg, Stroke = NodeBorder, StrokeThickness = 1.5
            };
            Canvas.SetLeft(ellipse, cx - NodeR);
            Canvas.SetTop(ellipse,  cy - NodeR);
            DrawCanvas.Children.Add(ellipse);

            // Label
            var label = node.Kind switch
            {
                Variable => $"x{node.VarIndex}",
                Constant => $"{node.ConstValue:G3}",
                _ => node.Label
            };

            var tb = new TextBlock
            {
                Text = label, FontSize = 9, Foreground = TextBrush,
                FontFamily = new FontFamily("Consolas"),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            tb.Measure(new Size(100, 100));
            Canvas.SetLeft(tb, cx - tb.DesiredSize.Width / 2);
            Canvas.SetTop(tb,  cy - tb.DesiredSize.Height / 2);
            DrawCanvas.Children.Add(tb);
        }
    }

    private void Layout(TreeNode node, int depth, Dictionary<TreeNode, Point> positions, double[] counter)
    {
        if (node.Children.Count == 0)
        {
            positions[node] = new Point(counter[0] * (NodeR * 2 + HGap), depth * VGap);
            counter[0]++;
            return;
        }
        foreach (var child in node.Children)
            Layout(child, depth + 1, positions, counter);

        // Parent centred over children
        double minX = node.Children.Min(c => positions[c].X);
        double maxX = node.Children.Max(c => positions[c].X);
        positions[node] = new Point((minX + maxX) / 2, depth * VGap);
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo info) { base.OnRenderSizeChanged(info); Render(); }
}
