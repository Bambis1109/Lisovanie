using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using MojaPrvaAvalonia.Models; // Tvoj pôvodný using

namespace MojaPrvaAvalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    // Tu pridáme náš "most" pre Serilog
    public ObservableCollection<string> VypisLogov => Program.UiSink.LogEvents;

    public CMainProgram MainProgram { get; }

    // Konštruktor necháme presne tak, ako si ho mal ty!
    public MainWindowViewModel(CMainProgram mainProgram)
    {
        MainProgram = mainProgram;
    }

    [RelayCommand]
    public void ExitApplication()
    {
        // Pozametáme kód a ukončíme vlákna
        MainProgram.Shutdown();

        // Korektne ukončíme aplikáciu
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