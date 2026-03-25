// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/ViewModels/MainViewModel.cs
using System.Windows;
using NeuroSim.Engine.Engine;
using NeuroSim.Engine.Genomes;
using NeuroSim.Engine.Population;

namespace NeuroSim.UI.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    public NavigationService Navigation { get; } = new();

    public RelayCommand BackCommand { get; }
    public RelayCommand HomeCommand { get; }

    private string _statusText = "Ready";
    public string StatusText { get => _statusText; set => Set(ref _statusText, value); }

    public MainViewModel()
    {
        var home = new HomeViewModel(Navigation);
        Navigation.Reset(home);
        BackCommand = new RelayCommand(() => Navigation.GoBack(), () => Navigation.CanGoBack);
        HomeCommand = new RelayCommand(() => { var h = new HomeViewModel(Navigation); Navigation.Reset(h); });

        // Sync status from active playgrounds
        Navigation.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(NavigationService.Current))
                BindCurrentPageStatus();
        };
    }

    private void BindCurrentPageStatus()
    {
        StatusText = Navigation.Current switch
        {
            HomeViewModel               => "Home",
            TspPlaygroundViewModel  tsp => tsp.Status,
            MazePlaygroundViewModel maz => maz.Status,
            BlueprintViewModel          => "Blueprint Editor",
            _                           => "Ready"
        };

        // Subscribe to Status changes for any PlaygroundViewModelBase
        if (Navigation.Current is PlaygroundViewModelBase pg)
            pg.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(PlaygroundViewModelBase.Status))
                    StatusText = pg.Status;
            };
    }
}
