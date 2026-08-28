using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    /// <summary>
    /// Multi-mix režim: každá vrstva je iná zmes, takže každý dávkovač má vlastný profil
    /// a poradie dávkovania určuje Lis cez IL.ZonePress.VrstvaRequest.
    /// Recept je počas behu nemenný, takže hodnota sa po štarte už nemení.
    /// </summary>
    public bool IsMultiMix => ParametersScale.Mode == EnModeVyroby.Multi;

    /// <summary>Vrstva, ktorú práve obsluhuje multi-mix vetva (1..3).</summary>
    private int _vrstvaAktualna;

    /// <summary>
    /// Váha, ktorú práve rozbieha štartovacia sekvencia (kroky 100 - 102).
    /// Váhy sa spúšťajú postupne, lebo súbežne dávkujúce dávkovače sa vzájomne rušia.
    /// </summary>
    private int _startIndex;

    /// <summary>Meranie času plnenia zásobníkov jednej váhy pri štarte.</summary>
    private readonly Stopwatch _swStartDavkovanie = new();

    /// <summary>
    /// Poistka proti zaseknutiu štartu na chybnom dávkovači - nie riadiaca hodnota.
    /// Profil dávky má vlastné timeouty (Rs_TimeoutBulkMs + Rs_TimeoutFineMs, spolu až ~35 s)
    /// a pri štarte sa plnia dva zásobníky, takže 2 minúty sú bezpečne nad najhorším prípadom.
    /// Bez tejto poistky by šlo o nekonečné čakanie na potvrdenie zo zariadenia.
    /// </summary>
    private const int StartDavkovanieTimeoutMs = 120_000;

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

    public CDeviceScale? GetScale(int index) => index switch
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
    /// Hodnoty pochádzajú z receptu (sekcia Vaha) - napĺňa ich CRecipeManager.
    /// </summary>
    public DeviceParameters DavkaParameters { get; } = new();

    /// <summary>
    /// Profily dávky jednotlivých dávkovačov pre multi-mix režim. Každá vrstva je iná zmes
    /// s vlastnou cieľovou hmotnosťou aj dynamikou dávkovania, preto sa nezdieľajú.
    /// V Single režime sa nepoužívajú.
    /// </summary>
    private readonly DeviceParameters[] _davkaVahy = { new(), new(), new() };

    /// <summary>Profil dávky konkrétneho dávkovača (1..3).</summary>
    public DeviceParameters GetDavkaProfile(int index) => _davkaVahy[index - 1];

    /// <summary>Profil, ktorý sa má odoslať do danej váhy - podľa režimu výroby.</summary>
    private DeviceParameters DavkaProfileFor(int index) =>
        IsMultiMix ? GetDavkaProfile(index) : DavkaParameters;

    /// <summary>
    /// Zabezpečí, že sú k dispozícii parametre dávky. Ak ich recept ešte nemá,
    /// vyčíta ich z váhy a rovno zapíše do receptu.
    /// V Single režime ide o jeden spoločný profil prevzatý z prvej aktívnej váhy,
    /// v Multi režime o tri profily, každý z tej váhy, do ktorej patrí.
    /// </summary>
    public bool EnsureDavkaParameters()
    {
        return IsMultiMix ? EnsureDavkaParametersMulti() : EnsureDavkaParametersSingle();
    }

    private bool EnsureDavkaParametersSingle()
    {
        if (DavkaParameters.Rs_TargetWeightMg > 0) return true;

        var source = ActiveScales.FirstOrDefault();
        if (source == null)
        {
            Log.Logger.ForContext("Name", Name)
                .Error("Recept nemá profil dávky a nie je dostupná žiadna aktívna váha, z ktorej by sa dal prevziať.");
            return false;
        }

        if (!ReadDavkaParametersFromScale(source, DavkaParameters)) return false;

        // Recept profil nemal - uložíme ho, aby bol pri ďalšom štarte k dispozícii.
        SaveDavkaParametersToRecipe();

        Log.Logger.ForContext("Name", Name)
            .Information($"Profil dávky prevzatý z váhy {source.Name} (ID: {source.NodeId}) a zapísaný do receptu.");
        return true;
    }

    private bool EnsureDavkaParametersMulti()
    {
        bool prevzate = false;

        foreach (var i in ActiveIndices)
        {
            var profil = GetDavkaProfile(i);
            if (profil.Rs_TargetWeightMg > 0) continue;

            var scale = GetScale(i)!;
            if (!ReadDavkaParametersFromScale(scale, profil)) return false;

            prevzate = true;
            Log.Logger.ForContext("Name", Name)
                .Information($"Profil dávky dávkovača {i} prevzatý z váhy {scale.Name} (ID: {scale.NodeId}).");
        }

        // Uložíme až po prevzatí všetkých chýbajúcich profilov - SaveAll zapisuje celý recept.
        if (prevzate) SaveDavkaParametersToRecipe();

        return true;
    }

    /// <summary>Zapíše aktuálne parametre dávky do receptu (sekcia Vaha).</summary>
    public bool SaveDavkaParametersToRecipe()
    {
        var manager = Program.MainProgram?.RecipeManager;
        if (manager == null)
        {
            Log.Logger.ForContext("Name", Name).Error("Profil dávky sa nemá kam uložiť - chýba správca receptov.");
            return false;
        }

        return manager.SaveAll();
    }

    /// <summary>Vyčíta parametre riadenia dávky z danej váhy do zadaného profilu.</summary>
    private bool ReadDavkaParametersFromScale(CDeviceScale scale, DeviceParameters target)
    {
        int errors = 0;

        foreach (var property in CDavkaParametersIo.DavkaProperties)
        {
            if (!CDavkaParametersIo.TryGetSdoAddress(property, out ushort index, out byte subIndex)) continue;

            try
            {
                uint value = scale.LowLayer.Can.GetRegister(index, subIndex);
                property.SetValue(target, (int)value);
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
    /// Odošle parametre dávky do všetkých aktívnych váh. V Single režime dostanú všetky
    /// ten istý spoločný profil, v Multi režime každá váha svoj vlastný.
    /// Vracia počet neúspešných zápisov (0 = všetko v poriadku).
    /// </summary>
    public int SendDavkaParametersToScales()
    {
        var indices = ActiveIndices.ToList();
        if (indices.Count == 0)
        {
            Log.Logger.ForContext("Name", Name)
                .Warning("Žiadna aktívna váha - parametre dávky sa neodosielajú.");
            return 0;
        }

        int errors = 0;

        foreach (var i in indices)
        {
            var scale = GetScale(i)!;
            var profil = DavkaProfileFor(i);

            foreach (var property in CDavkaParametersIo.DavkaProperties)
            {
                if (!CDavkaParametersIo.TryGetSdoAddress(property, out ushort index, out byte subIndex)) continue;

                try
                {
                    uint value = (uint)(int)(property.GetValue(profil) ?? 0);
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
            string popis = IsMultiMix ? "vlastné profily" : "spoločný profil";
            Log.Logger.ForContext("Name", Name)
                .Information($"Parametre dávky ({popis}) odoslané do váh [{string.Join(",", indices.Select(i => GetScale(i)!.NodeId))}].");
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
            case 105: return MainStep105(step);
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

            // ==========================================
            // MULTI-MIX VETVA (Kroky 400+)
            // ==========================================
            case 400: return MainStep400(step);
            case 410: return MainStep410(step);
            case 420: return MainStep420(step);
            case 430: return MainStep430(step);
            case 440: return MainStep440(step);
            case 450: return MainStep450(step);

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

    // ------------------------------------------------------------------
    // POSTUPNÝ ŠTART VÁH (kroky 100 - 102)
    //
    // Povel Produkcia rozbehne na váhe plnenie oboch zásobníkov (vážiaca miska aj
    // výložník). Ak ho dostanú všetky váhy naraz, dávkovače bežia súbežne a vzájomne sa
    // rušia. Preto sa váhy rozbiehajú po jednej - ďalšia dostane povel až keď predošlá
    // hlási cez TPDO4 naplnené oba zásobníky (IsFullyCharged).
    //
    // V ustálenom behu problém nenastáva: Lis si pýta dávky s odstupmi a váha po povele
    // Next dobíja len jednu, takže sa dávkovania nikdy neprekryjú.
    // ------------------------------------------------------------------

    /// <summary>Posunie štart na ďalšiu aktívnu váhu; 0 = všetky sú už rozbehnuté.</summary>
    private int NextStartIndex() => ActiveIndices.FirstOrDefault(i => i > _startIndex);

    private int MainStep100(int step)
    {
        Message = "Štart váh - postupné rozbiehanie";

        _startIndex = ActiveIndices.FirstOrDefault();
        if (_startIndex == 0)
        {
            Log.Logger.ForContext("Name", Name).Error("Žiadna aktívna váha - program zastavený.");
            return 0;
        }

        return 101;
    } //Reset indexu na prvu aktivnu vahu -> 101

    private int MainStep101(int step)
    {
        Message = $"Start Vaha {_startIndex}";

        var scale = GetScale(_startIndex)!;

        if (!scale.IsReady()) //kontrola ci je ready
        {
            Log.Logger.ForContext("Name", Name)
                .Error($"Váha {_startIndex} nie je Ready  status:[{((CDataScale)scale.Data).StatusMainProc}]");
            return 0;
        }

        scale.Operation.Master.SendCommand(EMasterCommand.Produkcia);

        if (!scale.WaitForProcStatus(EProcStatus.Busy, 2000))
        {
            Log.Logger.ForContext("Name", Name)
                .Error($"Váha {_startIndex} nie je Busy  status:[{((CDataScale)scale.Data).StatusMainProc}]");
            return 0;
        }

        Log.Logger.ForContext("Name", Name)
            .Information($"Váha {_startIndex} je Busy  status:[{((CDataScale)scale.Data).StatusMainProc}]");

        _swStartDavkovanie.Restart();
        return 102;
    } //Povel Produkcia vahe _startIndex -> 102

    private int MainStep102(int step)
    {
        Message = $"Váha {_startIndex}: dávkujem oba zásobníky";

        if (RequestToEnd)
        {
            return 0;
        }

        var scale = GetScale(_startIndex)!;

        if (scale.IsError() || scale.IsDoserError())
        {
            Log.Logger.ForContext("Name", Name)
                .Error($"Váha {_startIndex} hlási chybu pri plnení zásobníkov  status:[{scale.GetStatus()}]");
            return 0;
        }

        // Váha bez materiálu štart nezastavuje - reťazec je spoločný pre oba režimy.
        // V Single ju krok 140 preskočí, v Multi ju zachytí krok 410 s hlásením o vrstve.
        if (scale.IsNoMaterial())
        {
            Log.Logger.ForContext("Name", Name)
                .Warning($"Váha {_startIndex} nemá materiál - pokračujem ďalšou.");
            return AdvanceStart();
        }

        if (scale.IsFullyCharged())
        {
            Log.Logger.ForContext("Name", Name).Information(
                $"Váha {_startIndex} má nadávkované oba zásobníky ({_swStartDavkovanie.Elapsed.TotalSeconds:F1} s).");
            return AdvanceStart();
        }

        if (_swStartDavkovanie.ElapsedMilliseconds > StartDavkovanieTimeoutMs)
        {
            Log.Logger.ForContext("Name", Name).Warning(
                $"Váha {_startIndex} nenaplnila oba zásobníky do {StartDavkovanieTimeoutMs} ms " +
                $"(doser:[{(scale.IsDoserFull() ? "Full" : "-")}] výložník:[{(scale.IsVyloznikFull() ? "Full" : "-")}]) " +
                "- pokračujem ďalšou.");
            return AdvanceStart();
        }

        return step;
    } //Cakanie na naplnenie oboch zasobnikov -> 101 (dalsia vaha) alebo 105 (hotovo)

    /// <summary>Prepne štart na ďalšiu váhu, alebo pustí program do rozcestníka 105.</summary>
    private int AdvanceStart()
    {
        _swStartDavkovanie.Reset();
        _startIndex = NextStartIndex();
        return _startIndex == 0 ? 105 : 101;
    }

    /// <summary>
    /// Rozcestník podľa režimu výroby. Obe vetvy sú od tohto miesta úplne oddelené.
    /// </summary>
    private int MainStep105(int step)
    {
        if (IsMultiMix)
        {
            Message = "Multi-mix: dávkovanie po vrstvách";
            return 400;
        }

        return 110;
    } //Volba rezimu -> 110 (single) alebo 400 (multi-mix)

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

    // ==========================================
    // MULTI-MIX VETVA (Kroky 400+)
    //
    // Na rozdiel od Single vetvy sa tu váha nevyberá round-robinom - Lis určí cez
    // IL.ZonePress.VrstvaRequest, ktorý dávkovač má naplniť nasledujúcu vrstvu.
    // Vetva preto nemá vlastný sekvenčný stav a _lastUsedScale sa jej netýka.
    // ==========================================

    private int MainStep400(int step)
    {
        Message = "Multi-mix: čakanie na uvoľnenie zóny";
        if (RequestToEnd)
        {
            return 0;
        }

        if (!IL.ZonePress.TryLock(EnZoneOwner.Scale, EnZoneStatus.InputEmpty))
        {
            return step; // Zóna ešte nie je voľná, čakáme (10ms sleep)
        }

        _vrstvaAktualna = IL.ZonePress.VrstvaRequest;

        if (_vrstvaAktualna is < 1 or > 3)
        {
            Log.Logger.ForContext("Name", Name)
                .Error($"Multi-mix: Lis vyžiadal neplatný dávkovač ({_vrstvaAktualna}) - program zastavený.");
            return 0;
        }

        if (GetScale(_vrstvaAktualna) == null)
        {
            Log.Logger.ForContext("Name", Name)
                .Error($"Multi-mix: dávkovač {_vrstvaAktualna} nie je pripojený na zbernici - program zastavený.");
            return 0;
        }

        return 410;
    } //Cakanie na zonu -> 410

    private int MainStep410(int step)
    {
        Message = $"Multi-mix: čakám na dávku dávkovača {_vrstvaAktualna}";
        if (RequestToEnd)
        {
            return 0;
        }

        var scale = GetScale(_vrstvaAktualna)!;

        if (scale.IsNoMaterial())
        {
            Log.Logger.ForContext("Name", Name)
                .Error($"Multi-mix: dávkovač {_vrstvaAktualna} nemá materiál  status:[{scale.GetStatus()}]");
            return 0;
        }

        if (scale.IsError())
        {
            Log.Logger.ForContext("Name", Name)
                .Error($"Multi-mix: chyba dávkovača {_vrstvaAktualna}  status:[{scale.GetStatus()}]");
            return 0;
        }

        if (scale.IsFull())
        {
            return 420; // Dávka je pripravená vo vykladacej miske
        }

        return step;
    } //Cakanie na pripravenu davku -> 420

    private int MainStep420(int step)
    {
        Message = $"Multi-mix: vysypanie dávkovača {_vrstvaAktualna}";
        GetScale(_vrstvaAktualna)!.Operation.Master.SendCommand(EMasterCommand.Next);
        return 430;
    } //Povel na vysypanie -> 430

    private int MainStep430(int step)
    {
        Message = $"Multi-mix: čakám na začiatok vysypania ({_vrstvaAktualna})";
        var scale = GetScale(_vrstvaAktualna)!;

        if (scale.IsOcupied())
        {
            return 440;
        }

        if (scale.IsError())
        {
            Log.Logger.ForContext("Name", Name)
                .Error($"Multi-mix: chyba dávkovača {_vrstvaAktualna}  status:[{scale.GetStatus()}]");
            return 0;
        }

        return step;
    } //Cakanie na zaciatok vysypania -> 440

    private int MainStep440(int step)
    {
        Message = $"Multi-mix: čakám na koniec vysypania ({_vrstvaAktualna})";
        var scale = GetScale(_vrstvaAktualna)!;

        if (scale.IsFree())
        {
            return 450;
        }

        if (scale.IsError())
        {
            Log.Logger.ForContext("Name", Name)
                .Error($"Multi-mix: chyba dávkovača {_vrstvaAktualna}  status:[{scale.GetStatus()}]");
            return 0;
        }

        return step;
    } //Cakanie na koniec vysypania -> 450

    private int MainStep450(int step)
    {
        Message = $"Multi-mix: uvoľnenie zóny pre Lis (vrstva {_vrstvaAktualna})";

        // Hmotnosť tejto vrstvy [g] - Lis si ich postupne spočíta.
        var scale = GetScale(_vrstvaAktualna)!;
        double hmotnost = ((CDataScale)scale.Data).WeightFinal / 10000000.0;

        Log.Logger.ForContext("Name", Name)
            .Information($"Multi-mix: vrstva {_vrstvaAktualna} nasypaná, hmotnosť {hmotnost:F3} g.");

        IL.ZonePress.Release(EnZoneOwner.Scale, EnZoneStatus.InputFull, hmotnost);
        return 400; // Návrat na čakanie na ďalšiu vrstvu
    } //Uvolnenie zony pre Lis -> 400

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
