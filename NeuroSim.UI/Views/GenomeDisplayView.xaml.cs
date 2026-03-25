// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/Views/GenomeDisplayView.xaml.cs
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NeuroSim.UI.Models;

namespace NeuroSim.UI.Views;

/// <summary>
/// Reusable genome display: permutation, direction sequence, binary, real-valued, or graph.
/// Bind DataContext to GenomeDisplayInfo.
/// </summary>
public partial class GenomeDisplayView : UserControl
{
    public GenomeDisplayView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => UpdateGraphVisibility();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => UpdateGraphVisibility();

    private void UpdateGraphVisibility()
    {
        if (DataContext is GenomeDisplayInfo info
            && info.DisplayType == GenomeDisplayType.Graph
            && info.GraphEdgesForDisplay?.Any() == true)
        {
            GraphEdgesCtrl.Visibility = Visibility.Visible;
        }
        else
        {
            GraphEdgesCtrl.Visibility = Visibility.Collapsed;
        }
    }
}
