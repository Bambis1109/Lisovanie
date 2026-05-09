using System.Collections.ObjectModel;
using EposCmd.Net;

namespace MojaPrvaAvalonia.Models;

public class CMainProgram
{
    // Kolekcia teraz môže obsahovať akékoľvek CPlc (teda aj CManipulator)
    public ObservableCollection<CPlc> ZoznamPlc { get; } = new ObservableCollection<CPlc>();
    public CDeviceManagerCO DeviceManagerCO { get; set; }
    public CMainProgram()
    {
        // Vytvárame inštancie nášho nového potomka
        ZoznamPlc.Add(new CManipulator("Linka 1"));
        ZoznamPlc.Add(new CLis("Linka 2"));
    }

    
    public void Shutdown()
    {
        foreach (var plc in ZoznamPlc)
        {
            plc.StopProgramImmediately();
        }
    }
}