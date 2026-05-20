// ==========================================
// Súbor: MojaPrvaAvalonia\Views\UserControls\UcDeviceScale.axaml.cs
// ==========================================

using Avalonia.Controls;
using Avalonia.Interactivity;
using MojaPrvaAvalonia.ViewModels;
using Serilog;

namespace MojaPrvaAvalonia.Views.UserControls;

public partial class UcDeviceScale : UserControl
{
    private frmDeviceScaleSettings? _frmScale;

    public UcDeviceScale()
    {
        InitializeComponent();
    }

    private void SetupButton_Click(object? sender, RoutedEventArgs e)
    {
        Log.Information("SetupButton_Click spustené v UcScale.");
        
        if (_frmScale != null)
        {
            _frmScale.Activate();
            return;
        }

        if (DataContext is UcDeviceScaleViewModel vm)
        {
            if (vm.Device != null)
            {
                try
                {
                    // ZMENA: Pridanie vm.ControlScales do konštruktora
                    _frmScale = new frmDeviceScaleSettings(vm.ControlScales, vm.Device);
                    _frmScale.Closed += (s, args) => _frmScale = null;
                    
                    var parentWindow = TopLevel.GetTopLevel(this) as Window;
                    if (parentWindow != null)
                    {
                        _frmScale.Show(parentWindow);
                    }
                    else
                    {
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
    }
}