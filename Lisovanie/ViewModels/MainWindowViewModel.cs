using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lisovanie.Models;
using Lisovanie.Net;
using Serilog;

namespace Lisovanie.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<string> VypisLogov => Program.UiSink.LogEvents;

    public CMainProgram MainProgram { get; }

    public CControlManipulator? Manipulator => MainProgram.ZoznamPlc.Count > 0 ? MainProgram.ZoznamPlc[0] as CControlManipulator : null;
    public CControlLis? Lis => MainProgram.ZoznamPlc.Count > 1 ? MainProgram.ZoznamPlc[1] as CControlLis : null;

    /// <summary>
    /// Názov aktívneho receptu do hlavičky. Recept sa dá zmeniť len reštartom aplikácie,
    /// takže hodnota sa počas behu okna nemení a stačí ju prečítať raz.
    /// </summary>
    public string ActiveRecipeName => MainProgram.RecipeManager.ActiveRecipeName;

    public CMutexZone ZonePress => IL.ZonePress;

    public IEnumerable<EnZoneOwner> ZoneOwners => Enum.GetValues<EnZoneOwner>();

    public IEnumerable<EnZoneStatus> ZoneStatuses => Enum.GetValues<EnZoneStatus>();

    /// <summary>Počty výliskov za dnešný deň z databázy. Počítadlá matíc vedľa nich
    /// ukazujú len aktuálny beh - nulujú sa pri každom vyprázdnení matice.</summary>
    [ObservableProperty] private int _dnesOk;
    [ObservableProperty] private int _dnesNok;
    [ObservableProperty] private int _dnesSpolu;

    /// <summary>Dnešný dátum a čas bez sekúnd a sekundy zvlášť - v XAML majú sekundy
    /// polovičnú veľkosť písma, preto sú to dve samostatné bindovateľné property.</summary>
    [ObservableProperty] private string _hodinyBezSekund = string.Empty;
    [ObservableProperty] private string _sekundy = string.Empty;

    private DispatcherTimer? _dnesTimer;
    private DispatcherTimer? _hodinyTimer;
    private bool _dnesRefreshBezi;

    public MainWindowViewModel(CMainProgram mainProgram)
    {
        MainProgram = mainProgram;
    }

    /// <summary>Spustí periodické čítanie dnešnej bilancie. Volá sa po otvorení okna.</summary>
    public void StartDnesRefresh()
    {
        if (_dnesTimer is { IsEnabled: true }) return;

        _dnesTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _dnesTimer.Tick += (_, _) => _ = RefreshDnesAsync();
        _dnesTimer.Start();

        // Prvé načítanie hneď, aby okno neukazovalo nuly prvých 5 sekúnd.
        _ = RefreshDnesAsync();

        // Hodiny bežia zvlášť, raz za sekundu - na rozdiel od bilancie nejde o I/O,
        // takže sa netreba obmedzovať na 5-sekundový interval databázového refreshu.
        _hodinyTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _hodinyTimer.Tick += (_, _) => AktualizujHodiny();
        _hodinyTimer.Start();
        AktualizujHodiny();
    }

    public void StopDnesRefresh()
    {
        _dnesTimer?.Stop();
        _dnesTimer = null;

        _hodinyTimer?.Stop();
        _hodinyTimer = null;
    }

    private void AktualizujHodiny()
    {
        var teraz = DateTime.Now;
        HodinyBezSekund = teraz.ToString("dd.MM.yyyy HH:mm:");
        Sekundy = teraz.ToString("ss");
    }

    /// <summary>
    /// Prečíta dnešné počty z DB. Dátum sa berie pri každom tiku nanovo, takže
    /// bilancia sa o polnoci vynuluje sama. Dotazy sa nesmú prekrývať - ide o I/O.
    /// </summary>
    private async Task RefreshDnesAsync()
    {
        if (_dnesRefreshBezi) return;
        _dnesRefreshBezi = true;
        try
        {
            var counts = await MainProgram.ProductionLogger.GetCountsForDayAsync(DateTime.Now);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                DnesOk = counts.Ok;
                DnesNok = counts.Nok;
                DnesSpolu = counts.Spolu;
            });
        }
        catch (Exception ex)
        {
            Log.Warning("MainWindow: dnešnú bilanciu sa nepodarilo načítať: {Message}", ex.Message);
        }
        finally
        {
            _dnesRefreshBezi = false;
        }
    }

    [RelayCommand]
    public async Task ConnectAsync()
    {
        Log.Information("MainWindow: Connecting to CAN...");
        await MainProgram.Connect();
    }

    [RelayCommand]
    public void ExitApplication()
    {
        MainProgram.Shutdown();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
        else
        {
            System.Environment.Exit(0);
        }
    }
}