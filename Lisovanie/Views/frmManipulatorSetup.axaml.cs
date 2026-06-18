using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Lisovanie.Models;

namespace Lisovanie.Views;

public partial class frmManipulatorSetup : Window
{
    private FrmManipulatorSettingsPar? _settingsWindow;

    public frmManipulatorSetup()
    {
        InitializeComponent();
    }

    public frmManipulatorSetup(CControlManipulator controlManipulator)
    {
        InitializeComponent();
        DataContext = controlManipulator;
    }

    private void BtnClose_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void BtnSettings_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_settingsWindow != null)
        {
            _settingsWindow.Activate();
            return;
        }

        if (DataContext is CControlManipulator manipulator)
        {
            _settingsWindow = new FrmManipulatorSettingsPar(manipulator);
            _settingsWindow.Closed += (s, args) => _settingsWindow = null;
            _settingsWindow.Show(this);
        }
    }
}