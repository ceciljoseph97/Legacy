// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/App.xaml.cs
using System.Windows;

namespace NeuroSim.UI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, ex) =>
        {
            MessageBox.Show($"Unhandled error: {ex.Exception.Message}", "NeuroSim Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true;
        };
    }
}
