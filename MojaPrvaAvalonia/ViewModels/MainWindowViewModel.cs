using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MojaPrvaAvalonia.Models;
using MojaPrvaAvalonia.Net;
using Serilog;

namespace MojaPrvaAvalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<string> VypisLogov => Program.UiSink.LogEvents;

    public CMainProgram MainProgram { get; }

    public CControlManipulator? Manipulator => MainProgram.ZoznamPlc.Count > 0 ? MainProgram.ZoznamPlc[0] as CControlManipulator : null;
    public CControlLis? Lis => MainProgram.ZoznamPlc.Count > 1 ? MainProgram.ZoznamPlc[1] as CControlLis : null;

    public CMutexZone ZonePress => IL.ZonePress;

    public IEnumerable<EnZoneOwner> ZoneOwners => Enum.GetValues<EnZoneOwner>();

    public IEnumerable<EnZoneStatus> ZoneStatuses => Enum.GetValues<EnZoneStatus>();

    public MainWindowViewModel(CMainProgram mainProgram)
    {
        MainProgram = mainProgram;
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