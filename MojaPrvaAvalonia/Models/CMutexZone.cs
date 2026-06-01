using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;

namespace MojaPrvaAvalonia.Models;

public partial class CMutexZone : ObservableObject
{
    // Privátny zámok pre zabezpečenie Thread Safety (ochrana pred Race Conditions)
    private readonly object _syncRoot = new object();

    public string Name { get; }

    // Zóna po zapnutí aplikácie patrí Manipulatoru programu, kým sa nepotvrdí bezpečný stav
    [ObservableProperty]
    private EnZoneOwner _owner = EnZoneOwner.Manipulator;

    // Fyzický stav je po štarte neznámy
    [ObservableProperty]
    private EnZoneStatus _status = EnZoneStatus.Unknown;

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
            // Re-entrancy: Ak už zónu vlastním, nepotrebujem ju znovu zamykať
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
                
                Owner = EnZoneOwner.Free; 
                return true;
            }
            else
            {
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
    /// Volá ho MainProgram (alebo nadradená logika lisu) počas Initu, aby zónu uvoľnil do bežnej prevádzky.
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