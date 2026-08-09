using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using EposCmd.Net;
using EposCmd.Net.DeviceScaleSet;
using Lisovanie.Net;
using Lisovanie.ViewModels;
using Serilog;

namespace Lisovanie.Models;

public partial class CControlScales : CPlcScale
{
    public CDeviceScale? Scale1 { get; set; }
    public CDeviceScale? Scale2 { get; set; }
    public CDeviceScale? Scale3 { get; set; }
    private int _lastUsedScale = 3;

    public CParametersScale ParametersScale { get; set; } = new();

    public CControlScales(string name) : base(name)
    {
        // Parametre načíta CRecipeManager.Apply() po výbere receptu pri štarte.
        ScaleViewModels.Add(new UcDeviceScaleViewModel(this, null, "SC1"));
        ScaleViewModels.Add(new UcDeviceScaleViewModel(this, null, "SC2"));
        ScaleViewModels.Add(new UcDeviceScaleViewModel(this, null, "SC3"));
    }

    // ==========================================
    // HELPERY PRE AKTÍVNE VÁHY (dátovo riadená logika)
    // ==========================================

    private CDeviceScale? GetScale(int index) => index switch
    {
        1 => Scale1,
        2 => Scale2,
        3 => Scale3,
        _ => null
    };

    private bool IsScaleEnabled(int index) => index switch
    {
        1 => ParametersScale.EnabledVaha1,
        2 => ParametersScale.EnabledVaha2,
        3 => ParametersScale.EnabledVaha3,
        _ => false
    };

    // Indexy váh (1..3), ktoré sú povolené v parametroch a majú priradené zariadenie
    private IEnumerable<int> ActiveIndices =>
        Enumerable.Range(1, 3).Where(i => IsScaleEnabled(i) && GetScale(i) != null);

    /// <summary>Aktívne váhy - povolené v parametroch a s vytvoreným zariadením na zbernici.</summary>
    public IEnumerable<CDeviceScale> ActiveScales => ActiveIndices.Select(i => GetScale(i)!);

    // Krok vetvy vysypania pre danú váhu
    private static int BranchStep(int index) => index switch
    {
        1 => 150,
        2 => 250,
        3 => 350,
        _ => 0
    };

    // ==========================================
    // PARAMETRE DÁVKY (SDO 0x6006) SPOLOČNÉ PRE VŠETKY VÁHY
    // ==========================================

    /// <summary>
    /// Spoločná sada parametrov riadenia dávky. V Single móde majú všetky váhy ten istý
    /// materiál, takže dostávajú identické nastavenie.
    /// </summary>
    public DeviceParameters DavkaParameters { get; } = new();

    private bool _davkaLoaded;

    /// <summary>Cesta k súboru so spoločnými parametrami dávky.</summary>
    public static string DavkaParametersPath =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Parameters", "ParametersScales.json");

    /// <summary>
    /// Zabezpečí, že spoločné parametre dávky sú načítané. Ak súbor ešte neexistuje,
    /// vyčíta hodnoty z prvej aktívnej váhy a súbor založí.
    /// Volá sa lenivo (nie v konštruktore) - zariadenia vznikajú až v CMainProgram.Connect().
    /// </summary>
    public bool EnsureDavkaParameters()
    {
        if (_davkaLoaded) return true;

        if (File.Exists(DavkaParametersPath))
        {
            if (!CDavkaParametersIo.Load(DavkaParametersPath, DavkaParameters))
            {
                Log.Logger.ForContext("Name", Name)
                    .Error($"Súbor s parametrami dávky sa nepodarilo načítať: {DavkaParametersPath}");
                return false;
            }

            _davkaLoaded = true;
            return true;
        }

        var source = ActiveScales.FirstOrDefault();
        if (source == null)
        {
            Log.Logger.ForContext("Name", Name)
                .Error($"Súbor {DavkaParametersPath} neexistuje a nie je dostupná žiadna aktívna váha, z ktorej by sa dal založiť.");
            return false;
        }

        if (!ReadDavkaParametersFromScale(source)) return false;

        CDavkaParametersIo.Save(DavkaParametersPath, DavkaParameters);
        _davkaLoaded = true;

        Log.Logger.ForContext("Name", Name)
            .Information($"Parametre dávky založené z váhy {source.Name} (ID: {source.NodeId}).");
        return true;
    }

    /// <summary>Vyčíta parametre riadenia dávky z danej váhy do spoločnej sady.</summary>
    private bool ReadDavkaParametersFromScale(CDeviceScale scale)
    {
        int errors = 0;

        foreach (var property in CDavkaParametersIo.DavkaProperties)
        {
            if (!CDavkaParametersIo.TryGetSdoAddress(property, out ushort index, out byte subIndex)) continue;

            try
            {
                uint value = scale.LowLayer.Can.GetRegister(index, subIndex);
                property.SetValue(DavkaParameters, (int)value);
            }
            catch (Exception ex)
            {
                errors++;
                Log.Logger.ForContext("Name", Name)
                    .Error($"Chyba pri čítaní {property.Name} z váhy {scale.Name}: {ex.Message}");
            }
        }

        return errors == 0;
    }

    /// <summary>
    /// Odošle spoločné parametre dávky do všetkých aktívnych váh.
    /// Vracia počet neúspešných zápisov (0 = všetko v poriadku).
    /// </summary>
    public int SendDavkaParametersToScales()
    {
        var scales = ActiveScales.ToList();
        if (scales.Count == 0)
        {
            Log.Logger.ForContext("Name", Name)
                .Warning("Žiadna aktívna váha - parametre dávky sa neodosielajú.");
            return 0;
        }

        int errors = 0;

        foreach (var scale in scales)
        {
            foreach (var property in CDavkaParametersIo.DavkaProperties)
            {
                if (!CDavkaParametersIo.TryGetSdoAddress(property, out ushort index, out byte subIndex)) continue;

                try
                {
                    uint value = (uint)(int)(property.GetValue(DavkaParameters) ?? 0);
                    scale.LowLayer.Can.SetRegister(index, subIndex, value);
                }
                catch (Exception ex)
                {
                    errors++;
                    Log.Logger.ForContext("Name", Name)
                        .Error($"Chyba pri zápise {property.Name} do váhy {scale.Name}: {ex.Message}");
                }
            }
        }

        if (errors == 0)
        {
            Log.Logger.ForContext("Name", Name)
                .Information($"Parametre dávky odoslané do váh [{string.Join(",", scales.Select(s => s.NodeId))}].");
        }

        return errors;
    }

    public override int RunStep(int step)
    {
        switch (step)
        {
            // ==========================================
            // INIT SEKVENCIA (Kroky 1 - 99)
            // ==========================================
            case 1: return InitStep1(step);
            case 10: return InitStep10(step);
            case 20: return InitStep20(step);
            case 25: return InitStep25(step);
            case 30: return InitStep30(step);

            // ==========================================
            // MAIN SEKVENCIA (Kroky 100+)
            // ==========================================
            case 100: return MainStep100(step);
            case 101: return MainStep101(step);
            case 102: return MainStep102(step);
            case 110: return MainStep110(step);
            case 120: return MainStep120(step);
            case 130: return MainStep130(step);
            case 140: return MainStep140(step);
            case 150: return MainStep150(step);
            case 160: return MainStep160(step);
            case 170: return MainStep170(step);
            case 250: return MainStep250(step);
            case 260: return MainStep260(step);
            case 270: return MainStep270(step);
            case 280: return MainStep280(step);
            case 300: return MainStep300(step);
            case 350: return MainStep350(step);
            case 360: return MainStep360(step);
            case 370: return MainStep370(step);

            default: return base.RunStep(step);
        }
    }


    // ==========================================
    // METÓDY PRE INIT
    // ==========================================
    private int InitStep1(int step)
    {
        Message = "";
        return 10;
    } // Init -> 10

    private int InitStep10(int step)
    {
        Message = "Štart inicializácie váh...";
        // 1. Odoslanie povelov (Fire-and-Forget) - len aktívne váhy
        foreach (var i in ActiveIndices)
        {
            GetScale(i)!.Operation.Master.SendCommand(EMasterCommand.Init);
        }

        return 20;
    } //Štart inicializácie váh. -> 20

    private int InitStep20(int step)
    {
        Message = "Čakám na dokončenie inicializácie";
        // Čakáme max 15 sekúnd na každú aktívnu váhu
        foreach (var i in ActiveIndices)
        {
            GetScale(i)!.WaitForInitAttained(15000);
        }

        return 25;
    } //Čakám na dokončenie inicializácie -> 25

    private int InitStep25(int step)
    {
        Message = "Odosielam parametre dávky do váh";

        if (!EnsureDavkaParameters())
        {
            throw new Exception("Parametre dávky nie sú dostupné - inicializácia zastavená.");
        }

        int errors = SendDavkaParametersToScales();
        if (errors > 0)
        {
            throw new Exception($"Zápis parametrov dávky zlyhal ({errors} hodnôt) - inicializácia zastavená.");
        }

        return 30;
    } //Odoslanie spoločných parametrov dávky do váh -> 30

    private int InitStep30(int step)
    {
        Message = "Inicializácia úspešná";
        Log.Logger.ForContext("Name", Name)
            .Information($"Váhy [{string.Join(",", ActiveIndices)}] boli úspešne inicializované.");
        return 99; // Skočí do finálneho kroku, kde CPlc nastaví stav EnStatusPlc.Ready
    } // Ukoncenie inicializacie -99 koniec INIT

    // ==========================================
    // METÓDY PRE MAIN PROGRAM
    // ==========================================

    // Spoločný štart produkcie pre jednu váhu; vypnutá váha sa preskočí
    private int StartScaleStep(int index, int nextStep)
    {
        Message = $"Start Vaha {index}";
        var scale = GetScale(index);
        if (!IsScaleEnabled(index) || scale == null)
        {
            Log.Logger.ForContext("Name", Name).Information($"Váha {index} je vypnutá - preskakujem.");
            return nextStep;
        }

        if (!scale.IsReady()) //kontrola ci je ready
        {
            Log.Logger.ForContext("Name", Name)
                .Error($"Váha {index} nie je Ready  status:[{((CDataScale)scale.Data).StatusMainProc}]");
            return 0;
        }

        scale.Operation.Master.SendCommand(EMasterCommand.Produkcia);

        if (!scale.WaitForProcStatus(EProcStatus.Busy, 2000))
        {
            Log.Logger.ForContext("Name", Name)
                .Error($"Váha {index} nie je Busy  status:[{((CDataScale)scale.Data).StatusMainProc}]");
            return 0;
        }

        Log.Logger.ForContext("Name", Name)
            .Information($"Váha {index} je Busy  status:[{((CDataScale)scale.Data).StatusMainProc}]");
        return nextStep;
    }

    private int MainStep100(int step) => StartScaleStep(1, 101); //Start Vaha 1 ->101

    private int MainStep101(int step) => StartScaleStep(2, 102); //Start Vaha 2 ->102

    private int MainStep102(int step) => StartScaleStep(3, 110); //Start Vaha 3 ->110

    private int MainStep110(int step)
    {
        Message = "Cakanie na uvolnenie zony";
        if (RequestToEnd)
        {
            return 0;
        }

        if (IL.ZonePress.TryLock(EnZoneOwner.Scale, EnZoneStatus.InputEmpty))
        {
            return 120; // Zóna je naša, ideme vybrať, ktorá váha sype
        }

        return step; // Zóna ešte nie je voľná, čakáme (10ms sleep)
    } //Cakanie na uvolnenie zony ->120

    private int MainStep120(int step)
    {
        Message = "Cakam na pripravenie davky";

        if (RequestToEnd)
        {
            return 0;
        }

        var active = ActiveIndices.ToList();

        // Váha v stave NoMaterial je z rozdelovnika vyradena.
        // Ak su vsetky aktivne vahy bez materialu - koniec.
        if (active.All(i => GetScale(i)!.IsNoMaterial()))
        {
            Log.Logger.ForContext("Name", Name).Error("Žiadna aktívna váha nemá materiál.");
            foreach (var i in active)
            {
                Log.Logger.ForContext("Name", Name)
                    .Error($"Váha {i}  status:[{((CDataScale)GetScale(i)!.Data).StatusMainProc}]");
            }

            return 0;
        }

        if (active.Any(i => GetScale(i)!.IsFull()))
        {
            return 140;
        }

        return step;
    } //Cakam na pripravenie davky ak aspon jedna pripravena ->140

    private int MainStep130(int step)
    {
        Message = "......";
        return 140; // Zóna ešte nie je voľná, čakáme (10ms sleep)
    } // ->140

    private int MainStep140(int step)
    {
        Message = "Vyber vahy na vysypanie";

        // Round-robin 1->2->3: kandidati v cyklickom poradi od naposledy pouzitej vahy.
        // Vaha bez pripravenej davky (nie Full - napr. NoMaterial) sa preskoci.
        var active = ActiveIndices.ToList();
        var candidates = active
            .OrderBy(i => (i - _lastUsedScale - 1 + 3) % 3); // najskôr váha nasledujúca po _lastUsedScale

        foreach (var i in candidates)
        {
            if (GetScale(i)!.IsFull())
            {
                return BranchStep(i);
            }
        }

        foreach (var i in active)
        {
            Log.Logger.ForContext("Name", Name)
                .Error($"Nie je pripravena Váha {i}  status:[{GetScale(i)!.GetStatus()}]");
        }

        return 0;
    } //Vyber vahy na vysypanie Vaha1->150 , Vaha2->250 , Vaha3->350

    // ---------------------------------------------------------
    // VETVA 1: VYSYPANIE VÁHA 1
    // ---------------------------------------------------------

    private int MainStep150(int step)
    {
        Message = "Váha 1: Povel na vysypanie (Next)";
        Scale1!.Operation.Master.SendCommand(EMasterCommand.Next);
        _lastUsedScale = 1;
        return 160;
    } //Váha 1: Povel na vysypanie (Next) -> 160

    private int MainStep160(int step)
    {
        Message = "Váha 1: Čakanie na štart sypania (Occupied)";
        if (Scale1!.IsOcupied())
            return 170;

        if (Scale1.IsError())
        {
            Log.Logger.ForContext("Name", Name).Error("Váha 1 hlási chybu (Error) pri štarte sypania.");
            return 0;
        }

        return step; // Zostávame v slučke, čakáme na reakciu STM32
    } //Váha 1: Čakanie na štart sypania (Busy + Occupied) ->170

    private int MainStep170(int step)
    {
        Message = "Váha 1: Čakanie na dokončenie sypania (Free)";

        // 2. ÚSPEŠNÉ DOKONČENIE
        if (Scale1!.IsFree())
            return 280;
        // 3. CHYBA HARDVÉRU
        if (Scale1.IsError())
        {
            Log.Logger.ForContext("Name", Name).Error("Váha 1 hlási chybu (Error) počas sypania.");
            return 0;
        }

        return step; // Sypanie prebieha, čakáme
    } //Váha 1: Čakanie na dokončenie sypania (Busy + Free) ->280

    // ---------------------------------------------------------
    // VETVA 2: VYSYPANIE VÁHA 2
    // ---------------------------------------------------------
    private int MainStep250(int step)
    {
        Message = "Váha 2: Povel na vysypanie (Next)";
        Scale2!.Operation.Master.SendCommand(EMasterCommand.Next);
        _lastUsedScale = 2;
        return 260;
    } //Váha 2: Povel na vysypanie (Next) - >260

    private int MainStep260(int step)
    {
        Message = "Váha 2: Čakanie na štart sypania (Occupied)";
        if (Scale2!.IsOcupied())
            return 270;

        if (Scale2.IsError())
        {
            Log.Logger.ForContext("Name", Name).Error("Váha 2 hlási chybu (Error) pri štarte sypania.");
            return 0;
        }

        return step;
    } //Váha 2: Čakanie na štart sypania (Busy + Occupied) ->270

    private int MainStep270(int step)
    {
        Message = "Váha 2: Čakanie na dokončenie sypania (Free)";
        if (Scale2!.IsFree())
            return 280;

        if (Scale2.IsError())
        {
            Log.Logger.ForContext("Name", Name).Error("Váha 2 hlási chybu (Error) počas sypania.");
            return 0;
        }

        return step;
    } //Váha 2: Čakanie na dokončenie sypania (Busy + Free) ->280

    // ---------------------------------------------------------
    // VETVA 3: VYSYPANIE VÁHA 3
    // ---------------------------------------------------------
    private int MainStep350(int step)
    {
        Message = "Váha 3: Povel na vysypanie (Next)";
        Scale3!.Operation.Master.SendCommand(EMasterCommand.Next);
        _lastUsedScale = 3;
        return 360;
    } //Váha 3: Povel na vysypanie (Next) -> 360

    private int MainStep360(int step)
    {
        Message = "Váha 3: Čakanie na štart sypania (Occupied)";
        if (Scale3!.IsOcupied())
            return 370;

        if (Scale3.IsError())
        {
            Log.Logger.ForContext("Name", Name).Error("Váha 3 hlási chybu (Error) pri štarte sypania.");
            return 0;
        }

        return step;
    } //Váha 3: Čakanie na štart sypania (Busy + Occupied) ->370

    private int MainStep370(int step)
    {
        Message = "Váha 3: Čakanie na dokončenie sypania (Free)";
        if (Scale3!.IsFree())
            return 280;

        if (Scale3.IsError())
        {
            Log.Logger.ForContext("Name", Name).Error("Váha 3 hlási chybu (Error) počas sypania.");
            return 0;
        }

        return step;
    } //Váha 3: Čakanie na dokončenie sypania (Busy + Free) ->280

    private int MainStep280(int step)
    {
        Message = "Uvoľnenie zóny pre Lis";

        // Hmotnosť vysypanej dávky [g] z práve použitej váhy – putuje so zónou na Lis.
        var scale = GetScale(_lastUsedScale)!;
        double hmotnost = ((CDataScale)scale.Data).WeightFinal / 10000000.0;

        IL.ZonePress.Release(EnZoneOwner.Scale, EnZoneStatus.InputFull, hmotnost);
        return 110; // Návrat do idle slučky
    } //Uvoľnenie zóny pre Lis - >110

    private int MainStep300(int step)
    {
        Message = "Ukoncenie cinnosti";
        foreach (var i in ActiveIndices)
        {
            GetScale(i)!.Operation.Master.SendCommand(EMasterCommand.Stop);
        }

        return 0; // Návrat do idle slučky
    }

    // NodeID váh patria do vrstvy stroja, ich zapnutie do vrstvy výrobku - ukladá sa
    // preto vždy celá sada cez CRecipeManager.
    [RelayCommand]
    public void SaveParameters()
    {
        Program.MainProgram?.RecipeManager.SaveAll();
    }

    [RelayCommand]
    public void LoadParameters()
    {
        Program.MainProgram?.RecipeManager.Reload();
    }
}
