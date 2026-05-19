using Avalonia.Controls;
using Avalonia.Interactivity;
using MojaPrvaAvalonia.ViewModels;

namespace MojaPrvaAvalonia.Views.UserControls;

public partial class UcScale : UserControl
{
    private frmScale? _frmScale;

    public UcScale()
    {
        InitializeComponent();
    }

    private void SetupButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_frmScale != null)
        {
            _frmScale.Activate();
            return;
        }

        if (DataContext is UcScaleViewModel vm && vm.Device != null)
        {
            _frmScale = new frmScale(vm.Device);
            _frmScale.Closed += (s, args) => _frmScale = null;
            
            var parentWindow = VisualRoot as Window;
            if (parentWindow != null)
            {
                _frmScale.Show(parentWindow);
            }
            else
            {
                _frmScale.Show();
            }
        }
    }
}