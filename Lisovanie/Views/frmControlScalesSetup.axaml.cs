using Avalonia.Controls;
using Avalonia.Interactivity;
using Lisovanie.Models;

namespace Lisovanie.Views;

public partial class frmControlScalesSetup : Window
{
    private frmScaleParametersSettings? _settingsWindow;

    public frmControlScalesSetup()
    {
        InitializeComponent();
    }

    public frmControlScalesSetup(CControlScales controlScales)
    {
        InitializeComponent();
        DataContext = controlScales;
    }

    private void BtnSettings_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_settingsWindow != null)
        {
            _settingsWindow.Activate();
            return;
        }

        if (DataContext is CControlScales scales)
        {
            _settingsWindow = new frmScaleParametersSettings(scales);
            _settingsWindow.Closed += (s, args) => _settingsWindow = null;
            _settingsWindow.Show(this);
        }
    }

    private void BtnClose_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}