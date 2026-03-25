// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/Views/PlaygroundAnalysisView.xaml.cs
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeuroSim.UI.Helpers;
using NeuroSim.UI.Models;

namespace NeuroSim.UI.Views;

/// <summary>
/// Reusable analysis panel for TSP, Maze, and future playgrounds.
/// Binds: StatBadges, Charts. Optional: ExtraContent.
/// </summary>
public partial class PlaygroundAnalysisView : UserControl
{
    public static readonly DependencyProperty StatBadgesProperty =
        DependencyProperty.Register(nameof(StatBadges), typeof(IEnumerable), typeof(PlaygroundAnalysisView),
            new PropertyMetadata(null, (d, e) => ((PlaygroundAnalysisView)d).StatBadgesCtrl.ItemsSource = e.NewValue as IEnumerable));

    public static readonly DependencyProperty ChartsProperty =
        DependencyProperty.Register(nameof(Charts), typeof(IEnumerable), typeof(PlaygroundAnalysisView),
            new PropertyMetadata(null, (d, e) => ((PlaygroundAnalysisView)d).ChartsCtrl.ItemsSource = e.NewValue as IEnumerable));

    public static readonly DependencyProperty ExtraContentProperty =
        DependencyProperty.Register(nameof(ExtraContent), typeof(object), typeof(PlaygroundAnalysisView),
            new PropertyMetadata(null, (d, e) => ((PlaygroundAnalysisView)d).ExtraPresenter.Content = e.NewValue));

    public IEnumerable? StatBadges
    {
        get => (IEnumerable?)GetValue(StatBadgesProperty);
        set => SetValue(StatBadgesProperty, value);
    }

    public IEnumerable? Charts
    {
        get => (IEnumerable?)GetValue(ChartsProperty);
        set => SetValue(ChartsProperty, value);
    }

    public object? ExtraContent
    {
        get => GetValue(ExtraContentProperty);
        set => SetValue(ExtraContentProperty, value);
    }

    public PlaygroundAnalysisView()
    {
        InitializeComponent();
    }

    private void OnExportChartClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not ChartSlot slot)
            return;
        ExportHelper.ExportPlot(slot.Plot, slot.Title);
    }
}
