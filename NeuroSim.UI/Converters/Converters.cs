// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/Converters/Converters.cs
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace NeuroSim.UI.Converters;

[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type _, object __, CultureInfo ___) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type _, object __, CultureInfo ___) =>
        value is Visibility.Visible;
}

[ValueConversion(typeof(int), typeof(Visibility))]
public sealed class CountToVisibilityConverter : IValueConverter
{
    /// <summary>Visible when count is 0, Collapsed otherwise. For empty-state hints.</summary>
    public object Convert(object value, Type _, object __, CultureInfo ___) =>
        value is int n && n == 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object _, Type __, object ___, CultureInfo ____) =>
        throw new NotSupportedException();
}

[ValueConversion(typeof(object), typeof(string))]
public sealed class ObjectToStringConverter : IValueConverter
{
    public object Convert(object value, Type _, object __, CultureInfo ___) => value?.ToString() ?? "";
    public object ConvertBack(object value, Type _, object __, CultureInfo ___) => value as string ?? "";
}

/// <summary>Returns SelectedBrush when values[0] == values[1], else DefaultBrush. For selection highlight.</summary>
public sealed class EqualToBrushConverter : IMultiValueConverter
{
    public Brush? SelectedBrush { get; set; }
    public Brush? DefaultBrush { get; set; }
    public object Convert(object[] values, Type _, object __, CultureInfo ___)
    {
        if (values.Length >= 2 && ReferenceEquals(values[0], values[1]))
            return SelectedBrush ?? Brushes.Transparent;
        return DefaultBrush ?? Brushes.Transparent;
    }
    public object[] ConvertBack(object _, Type[] __, object ___, CultureInfo ____) =>
        throw new NotSupportedException();
}

[ValueConversion(typeof(object), typeof(Visibility))]
public sealed class NullToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }
    public object Convert(object value, Type _, object __, CultureInfo ___) =>
        (value is not null) != Invert ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object _, Type __, object ___, CultureInfo ____) =>
        throw new NotSupportedException();
}


[ValueConversion(typeof(bool), typeof(SolidColorBrush))]
public sealed class BoolToColorConverter : IValueConverter
{
    public SolidColorBrush TrueBrush { get; set; } = Brushes.Green;
    public SolidColorBrush FalseBrush { get; set; } = Brushes.Gray;

    public object Convert(object value, Type _, object __, CultureInfo ___) =>
        value is true ? TrueBrush : FalseBrush;

    public object ConvertBack(object value, Type _, object __, CultureInfo ___) =>
        throw new NotSupportedException();
}

/// <summary>Builds a Bezier path for a connection. Values: sourceX, sourceY, targetX, targetY.</summary>
public sealed class ConnectionToPathConverter : IMultiValueConverter
{
    private const double PortOffsetX = 70; // half node width for output; input at 0

    public object Convert(object[] values, Type _, object __, CultureInfo ___)
    {
        if (values.Length < 4 || values[0] is not double sx || values[1] is not double sy ||
            values[2] is not double tx || values[3] is not double ty)
            return Geometry.Empty;

        var start = new Point(sx + PortOffsetX * 2, sy + 16);
        var end = new Point(tx, ty + 16);
        var midX = (start.X + end.X) / 2;
        var path = new PathGeometry();
        var fig = new PathFigure { StartPoint = start };
        fig.Segments.Add(new BezierSegment(
            new Point(midX, start.Y),
            new Point(midX, end.Y),
            end, true));
        path.Figures.Add(fig);
        return path;
    }

    public object[] ConvertBack(object _, Type[] __, object ___, CultureInfo ____) =>
        throw new NotSupportedException();
}
