using System.Collections.ObjectModel;
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
}