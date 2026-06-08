using Avalonia.Controls;
using Avalonia.Interactivity;
using MojaPrvaAvalonia.Models;
using MojaPrvaAvalonia.Views;

namespace MojaPrvaAvalonia.Views.UserControls;

public partial class UcControlScales : UserControl
{
    private frmControlScalesSetup? _setupWindow;

    public UcControlScales()
    {
        InitializeComponent();
    }

    private void BtnSetup_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_setupWindow != null)
        {
            _setupWindow.Activate();
            return;
        }

        if (DataContext is CControlScales scales)
        {
            _setupWindow = new frmControlScalesSetup(scales);
            _setupWindow.Closed += (s, args) => _setupWindow = null;
            var parentWindow = VisualRoot as Window;
            if (parentWindow != null)
                _setupWindow.Show(parentWindow);
            else
                _setupWindow.Show();
        }
    }
}