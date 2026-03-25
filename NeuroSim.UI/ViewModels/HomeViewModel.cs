// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/ViewModels/HomeViewModel.cs
namespace NeuroSim.UI.ViewModels;

public sealed class HomeViewModel : ViewModelBase
{
    private readonly NavigationService _nav;

    public RelayCommand GoToTspCommand     { get; }
    public RelayCommand GoToMazeCommand    { get; }
    public RelayCommand GoToBlueprintCommand { get; }

    public HomeViewModel(NavigationService nav)
    {
        _nav = nav;
        GoToTspCommand      = new RelayCommand(() => _nav.Navigate(new TspPlaygroundViewModel(_nav)));
        GoToMazeCommand     = new RelayCommand(() => _nav.Navigate(new MazePlaygroundViewModel(_nav)));
        GoToBlueprintCommand = new RelayCommand(() => _nav.Navigate(new BlueprintViewModel(nav)));
    }
}
