using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;

namespace MojaPrvaAvalonia.Models;

public partial class CMutexZone : ObservableObject
{
    // Privátny zámok pre zabezpečenie Thread Safety (ochrana pred Race Conditions)
    private readonly object _syncRoot = new object();

    public string Name { get; }

    [ObservableProperty]
    private EnZoneOwner _owner = EnZoneOwner.Free;

    [ObservableProperty]
    private EnZoneStatus _status = EnZoneStatus.InputEmpty;

    public CMutexZone(string name)
    {
        Name = name;
    }

    public override string ToString() => $"{Name}:(Owner:{Owner})(Status:{Status})";

    /// <summary>
    /// Pokúsi sa získať zónu pre žiadateľa, ak je voľná a má požadovaný fyzický stav.
    /// </summary>
    public bool TryLock(EnZoneOwner requester, EnZoneStatus requiredStatus)
    {
        lock (_syncRoot)
        {
            // Re-entrancy: Ak už zónu vlastním, nepotrebujem ju znovu zamykať (krok naprázdno)
            if (Owner == requester)
            {
                return true; 
            }

            // Ak je voľná a stav materiálu zodpovedá požiadavke, zamkni ju
            if (Owner == EnZoneOwner.Free && Status == requiredStatus)
            {
                Owner = requester;
                Log.Logger.ForContext("Name", Name)
                          .Verbose($"Preberá riadenie: {requester} (Stav zóny: {Status})");
                return true;
            }

            // Zóna je obsadená, alebo nemá správny stav (napr. požaduje sa InputEmpty, ale je InputFull)
            return false;
        }
    }

    /// <summary>
    /// Uvoľní zónu a zapíše jej nový fyzický stav (napr. InputEmpty -> InputFull).
    /// </summary>
    public bool Release(EnZoneOwner requester, EnZoneStatus newStatus)
    {
        lock (_syncRoot)
        {
            // Bezpečnostná poistka: Môže uvoľniť len ten, kto zónu skutočne vlastní
            if (Owner == requester)
            {
                Status = newStatus;
                Log.Logger.ForContext("Name", Name)
                          .Verbose($"Uvoľňuje riadenie: {requester}. Nový stav zóny: {newStatus}");
                
                Owner = EnZoneOwner.Free; // Až na konci uvoľníme zámok
                return true;
            }
            else
            {
                // Ak niekto cudzí skúša odomknúť zónu
                if (Owner != EnZoneOwner.Free)
                {
                    Log.Logger.ForContext("Name", Name)
                              .Warning($"Pokus o neoprávnené uvoľnenie zóny! Žiadateľ: {requester}, Aktuálny vlastník: {Owner}");
                }
                return false; 
            }
        }
    }

    /// <summary>
    /// Násilný reset zóny do základného stavu.
    /// Volá sa z centrálneho Resetu/Initu (napr. po havárii stroja, keď obsluha potvrdí vyčistenie).
    /// </summary>
    public void ForceReset()
    {
        lock (_syncRoot)
        {
            Owner = EnZoneOwner.Free;
            Status = EnZoneStatus.InputEmpty;
            Log.Logger.ForContext("Name", Name)
                      .Warning("Násilný reset zóny (ForceReset) -> Free / InputEmpty");
        }
    }
}