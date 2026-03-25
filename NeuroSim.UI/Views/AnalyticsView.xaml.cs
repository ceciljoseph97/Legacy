// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/Views/AnalyticsView.xaml.cs
using System.Collections.Specialized;
using System.Windows.Controls;
using NeuroSim.UI.ViewModels;

namespace NeuroSim.UI.Views;

public partial class AnalyticsView : UserControl
{
    public AnalyticsView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is AnalyticsViewModel vm)
                vm.Rows.CollectionChanged += AutoScroll;
        };
    }

    private void AutoScroll(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (StatsGrid.Items.Count > 0)
            StatsGrid.ScrollIntoView(StatsGrid.Items[^1]);
    }
}
