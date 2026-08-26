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

    // Index 0 = spoločný profil (Single), 1..3 = profily jednotlivých dávkovačov (Multi).
    // Každý má vlastné okno, aby sa dali profily porovnávať vedľa seba.
    private readonly ParametersWindow?[] _parametersWindows = new ParametersWindow?[4];
    private readonly bool[] _parametersOpening = new bool[4];

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

    private void BtnParameters_OnClick(object? sender, RoutedEventArgs e) => OpenDavkaWindow(0);

    private void BtnDavka1_OnClick(object? sender, RoutedEventArgs e) => OpenDavkaWindow(1);

    private void BtnDavka2_OnClick(object? sender, RoutedEventArgs e) => OpenDavkaWindow(2);

    private void BtnDavka3_OnClick(object? sender, RoutedEventArgs e) => OpenDavkaWindow(3);

    /// <summary>
    /// Otvorí okno profilu dávky. Index 0 = spoločný profil pre všetky váhy (Single),
    /// 1..3 = profil konkrétneho dávkovača (Multi-mix).
    /// </summary>
    private async void OpenDavkaWindow(int doserIndex)
    {
        if (_parametersWindows[doserIndex] != null)
        {
            _parametersWindows[doserIndex]!.Activate();
            return;
        }

        if (_parametersOpening[doserIndex]) return;
        if (DataContext is not CControlScales scales) return;

        _parametersOpening[doserIndex] = true;
        try
        {
            // Prvé otvorenie môže parametre vyčítavať z váhy cez SDO - mimo UI vlákna.
            await Task.Run(() => scales.EnsureDavkaParameters());

            var (parameters, title) = doserIndex == 0
                ? (scales.DavkaParameters, "Parametre dávky - všetky váhy")
                : (scales.GetDavkaProfile(doserIndex), $"Parametre dávky - dávkovač {doserIndex}");

            var window = new ParametersWindow
            {
                DataContext = doserIndex == 0
                    ? new ParametersViewModel(parameters, scales)
                    : new ParametersViewModel(parameters, scales, doserIndex),
                Title = title
            };
            window.Closed += (s, args) => _parametersWindows[doserIndex] = null;
            _parametersWindows[doserIndex] = window;
            window.Show(this);
        }
        catch (Exception ex)
        {
            // async void - neošetrená výnimka by zhodila celú aplikáciu
            Log.Error($"Chyba pri otváraní okna parametrov dávky: {ex.Message}");
            _parametersWindows[doserIndex] = null;
        }
        finally
        {
            _parametersOpening[doserIndex] = false;
        }
    }

    private void BtnClose_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}