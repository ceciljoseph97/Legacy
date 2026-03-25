// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/ViewModels/ProblemSelectorViewModel.cs
namespace NeuroSim.UI.ViewModels;

public sealed class ProblemSelectorViewModel : ViewModelBase
{
    private readonly NavigationService _nav;

    public RelayCommand OpenTspCommand   { get; }
    public RelayCommand OpenCspCommand   { get; }
    public RelayCommand OpenGraphCommand { get; }

    public ProblemSelectorViewModel(NavigationService nav)
    {
        _nav = nav;
        OpenTspCommand   = new RelayCommand(() => _nav.Navigate(new TspPlaygroundViewModel(_nav)));
        OpenCspCommand   = new RelayCommand(() => _nav.Navigate(new CspPlaygroundViewModel(_nav)));
        OpenGraphCommand = new RelayCommand(() => _nav.Navigate(new GraphPlaygroundViewModel(_nav)));
    }
}
