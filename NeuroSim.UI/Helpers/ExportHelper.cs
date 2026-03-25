// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/Helpers/ExportHelper.cs
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Wpf;

namespace NeuroSim.UI.Helpers;

/// <summary>
/// Shared export for maps, mazes, and plots.
/// Use from any playground: ExportHelper.ExportVisual(mapElement, ...) or ExportHelper.ExportPlot(...).
/// </summary>
public static class ExportHelper
{
    /// <summary>Export a visual (map, maze canvas) to PNG.</summary>
    public static bool ExportVisual(FrameworkElement element, string? suggestedName = null)
    {
        var dlg = new SaveFileDialog
        {
            Title      = "Export to PNG",
            Filter     = "PNG (*.png)|*.png",
            FileName   = suggestedName ?? "export",
            DefaultExt = ".png"
        };
        if (dlg.ShowDialog() != true) return false;

        try
        {
            var (w, h) = ((int)element.ActualWidth, (int)element.ActualHeight);
            if (w < 1 || h < 1) { w = 800; h = 600; }

            element.Measure(new Size(w, h));
            element.Arrange(new Rect(0, 0, w, h));

            var bmp = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(element);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            using (var fs = File.Create(dlg.FileName))
                encoder.Save(fs);

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }

    /// <summary>Export an OxyPlot PlotModel to PNG.</summary>
    public static bool ExportPlot(PlotModel? plot, string title, int width = 800, int height = 400)
    {
        if (plot == null) return false;

        var dlg = new SaveFileDialog
        {
            Title      = $"Export {title}",
            Filter     = "PNG (*.png)|*.png",
            FileName   = $"{title}_{DateTime.Now:yyyyMMdd_HHmm}",
            DefaultExt = ".png"
        };
        if (dlg.ShowDialog() != true) return false;

        try
        {
            var exporter = new PngExporter { Width = width, Height = height };
            exporter.ExportToFile(plot, dlg.FileName);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }
}
