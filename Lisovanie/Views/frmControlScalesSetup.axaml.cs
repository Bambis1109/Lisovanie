using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Lisovanie.Models;
using Lisovanie.ViewModels;
using Serilog;

namespace Lisovanie.Views;

public partial class frmControlScalesSetup : Window
{
    private frmScaleParametersSettings? _settingsWindow;
    private ParametersWindow? _parametersWindow;
    private bool _parametersOpening;

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

    private async void BtnParameters_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_parametersWindow != null)
        {
            _parametersWindow.Activate();
            return;
        }

        if (_parametersOpening) return;
        if (DataContext is not CControlScales scales) return;

        _parametersOpening = true;
        try
        {
            // Prvé otvorenie môže parametre vyčítavať z váhy cez SDO - mimo UI vlákna.
            await Task.Run(() => scales.EnsureDavkaParameters());

            _parametersWindow = new ParametersWindow
            {
                DataContext = new ParametersViewModel(scales.DavkaParameters, scales),
                Title = "Parametre dávky - všetky váhy"
            };
            _parametersWindow.Closed += (s, args) => _parametersWindow = null;
            _parametersWindow.Show(this);
        }
        catch (Exception ex)
        {
            // async void - neošetrená výnimka by zhodila celú aplikáciu
            Log.Error($"Chyba pri otváraní okna parametrov dávky: {ex.Message}");
            _parametersWindow = null;
        }
        finally
        {
            _parametersOpening = false;
        }
    }

    private void BtnClose_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}