using System.Collections.ObjectModel;

namespace MojaPrvaAvalonia.Models;

public class CMainProgram
{
    // Kolekcia teraz môže obsahovať akékoľvek CPlc (teda aj CManipulator)
    public ObservableCollection<CPlc> ZoznamPlc { get; } = new ObservableCollection<CPlc>();

    public CMainProgram()
    {
        // Vytvárame inštancie nášho nového potomka
        ZoznamPlc.Add(new CManipulator("Linka 1"));
        ZoznamPlc.Add(new CManipulator("Linka 2"));
    }
}