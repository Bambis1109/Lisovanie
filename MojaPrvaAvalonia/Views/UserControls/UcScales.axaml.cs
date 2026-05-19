using Avalonia.Controls;
using Avalonia.Interactivity;
using MojaPrvaAvalonia.Models;

namespace MojaPrvaAvalonia.Views.UserControls;

public partial class UcScales : UserControl
{
    private frmScalesSetup? _setupWindow;

    public UcScales()
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

        if (DataContext is CScales scales)
        {
            _setupWindow = new frmScalesSetup(scales);
            
            _setupWindow.Closed += (s, args) => _setupWindow = null;
            
            var parentWindow = VisualRoot as Window;
            if (parentWindow != null)
            {
                _setupWindow.Show(parentWindow);
            }
            else
            {
                _setupWindow.Show();
            }
        }
    }
}