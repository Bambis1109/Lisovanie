using Avalonia.Controls;
using Avalonia.Interactivity;
using MojaPrvaAvalonia.ViewModels;
using Serilog;

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
        Log.Information("SetupButton_Click spustené v UcScale.");
        
        if (_frmScale != null)
        {
            Log.Information("frmScale už existuje, aktivujem ho.");
            _frmScale.Activate();
            return;
        }

        if (DataContext is UcScaleViewModel vm)
        {
            if (vm.Device != null)
            {
                Log.Information($"Vytváram frmScale pre zariadenie: {vm.Device.Name}");
                try
                {
                    _frmScale = new frmScale(vm.Device);
                    _frmScale.Closed += (s, args) => _frmScale = null;
                    
                    var parentWindow = TopLevel.GetTopLevel(this) as Window;
                    if (parentWindow != null)
                    {
                        Log.Information("Otváram frmScale s parent oknom.");
                        _frmScale.Show(parentWindow);
                    }
                    else
                    {
                        Log.Information("Otváram frmScale bez parent okna.");
                        _frmScale.Show();
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"Chyba pri otváraní frmScale: {ex.Message}\n{ex.StackTrace}");
                }
            }
            else
            {
                Log.Warning("Zariadenie (Device) vo ViewModele je null! Najskôr sa pripoj (Connect).");
            }
        }
        else
        {
            Log.Warning($"DataContext nie je UcScaleViewModel, je to: {DataContext?.GetType().Name}");
        }
    }
}