// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/Views/CityWizardDialog.xaml.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace NeuroSim.UI.Views;

public partial class CityWizardDialog : Window, INotifyPropertyChanged
{
    // ── Bindable properties ───────────────────────────────────────────────
    private string _cityName = "";
    public string CityName
    {
        get => _cityName;
        set { _cityName = value; Notify(); Validate(); }
    }

    private double _x;
    public double X
    {
        get => _x;
        set { _x = value; Notify(); Validate(); }
    }

    private double _y;
    public double Y
    {
        get => _y;
        set { _y = value; Notify(); Validate(); }
    }

    private string _validationMessage = "";
    public string ValidationMessage
    {
        get => _validationMessage;
        set { _validationMessage = value; Notify(); }
    }

    // ── Result ────────────────────────────────────────────────────────────
    public bool Confirmed { get; private set; }

    public CityWizardDialog(string suggestedName, double clickX, double clickY)
    {
        InitializeComponent();
        DataContext = this;
        _cityName = suggestedName;
        _x = Math.Round(Math.Clamp(clickX, 0, 1000), 1);
        _y = Math.Round(Math.Clamp(clickY, 0, 1000), 1);
        Loaded += (_, _) => { NameBox.Focus(); NameBox.SelectAll(); };
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(_cityName)) { ValidationMessage = "City name cannot be empty."; return; }
        if (_x < 0 || _x > 1000 || _y < 0 || _y > 1000) { ValidationMessage = "Coordinates must be 0 – 1000."; return; }
        ValidationMessage = "";
    }

    private void OnConfirm(object sender, RoutedEventArgs e)
    {
        Validate();
        if (!string.IsNullOrWhiteSpace(ValidationMessage)) return;
        Confirmed = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string? p = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}
