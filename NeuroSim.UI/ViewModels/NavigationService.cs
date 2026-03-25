// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/ViewModels/NavigationService.cs
namespace NeuroSim.UI.ViewModels;

/// <summary>Simple stack-based page navigator.</summary>
public sealed class NavigationService : ViewModelBase
{
    private readonly Stack<ViewModelBase> _stack = new();

    private ViewModelBase _current = null!;
    public ViewModelBase Current
    {
        get => _current;
        private set
        {
            Set(ref _current, value);
            Notify(nameof(CanGoBack));
            Notify(nameof(BreadcrumbTitle));
        }
    }

    public bool CanGoBack => _stack.Count > 1;

    public string BreadcrumbTitle => Current switch
    {
        HomeViewModel           => "Home",
        TspPlaygroundViewModel  => "Home  ›  Traveling Salesperson Problem",
        MazePlaygroundViewModel => "Home  ›  Maze Solver",
        BlueprintViewModel      => "Home  ›  Blueprint Editor",
        _ => ""
    };

    public void Navigate(ViewModelBase vm)
    {
        _stack.Push(vm);
        Current = vm;
    }

    public void GoBack()
    {
        if (_stack.Count > 1)
        {
            _stack.Pop();
            Current = _stack.Peek();
        }
    }

    public void Reset(ViewModelBase home)
    {
        _stack.Clear();
        _stack.Push(home);
        Current = home;
    }
}
