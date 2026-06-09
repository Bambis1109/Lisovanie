using CommunityToolkit.Mvvm.Input;
using EposCmd.Net;
using EposCmd.Net.DeviceScaleSet;
using MojaPrvaAvalonia.Net;
using MojaPrvaAvalonia.ViewModels;
using Serilog;

namespace MojaPrvaAvalonia.Models;

public partial class CControlScales : CPlcScale
{
    public CDeviceScale Scale1 { get; set; }
    public CDeviceScale Scale2 { get; set; }
    private int _lastUsedScale = 2;

    public CParametersScale ParametersScale { get; set; } = new();

    public CControlScales(string name) : base(name)
    {
        LoadParameters();
        ScaleViewModels.Add(new UcDeviceScaleViewModel(this, null, "SC1"));
        ScaleViewModels.Add(new UcDeviceScaleViewModel(this, null, "SC2"));
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
            case 30: return InitStep30(step);

            // ==========================================
            // MAIN SEKVENCIA (Kroky 100+)
            // ==========================================
            case 100: return MainStep100(step);
            case 101: return MainStep101(step);
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
        // 1. Odoslanie povelov (Fire-and-Forget)
        Scale1.Operation.Master.SendCommand(EMasterCommand.Init);
        Scale2.Operation.Master.SendCommand(EMasterCommand.Init);
        return 20;
    } //Štart inicializácie váh. -> 20

    private int InitStep20(int step)
    {
        Message = "Čakám na dokončenie inicializácie";
      // Čakáme max 15 sekúnd na k
        Scale1.WaitForInitAttained(15000);
        Scale2.WaitForInitAttained(15000);
        return 30;
    } //Čakám na dokončenie inicializácie -> 30

    private int InitStep30(int step)
    {
        Message = "Inicializácia úspešná";
        Log.Logger.ForContext("Name", Name).Information("Obe váhy boli úspešne inicializované.");
        return 99; // Skočí do finálneho kroku, kde CPlc nastaví stav EnStatusPlc.Ready
    } // Ukoncenie inicializacie -99 koniec INIT

    // ========================================== 
    // METÓDY PRE MAIN PROGRAM
    // ==========================================

    private int MainStep100(int step)
    {
        Message = "Start Vaha 1";
        if (!Scale1.IsReady()) //kontrola ci je ready
        {
            Log.Logger.ForContext("Name", Name)
                .Information($"Váha 1 nie je Ready  status:[{((CDataScale)Scale1.Data).StatusMainProc}]");
            return 0;
        }

        Scale1.Operation.Master.SendCommand(EMasterCommand.Produkcia); 

        if (!Scale1.WaitForProcStatus(EProcStatus.Busy, 2000))
        {
            Log.Logger.ForContext("Name", Name)
                .Information($"Váha 1 nie je Busy  status:[{((CDataScale)Scale1.Data).StatusMainProc}]");
            return 0;
        }

        Log.Logger.ForContext("Name", Name)
            .Information($"Váha 1 je Busy  status:[{((CDataScale)Scale1.Data).StatusMainProc}]");
        return 101;
    } //Start Vaha 1 ->101

    private int MainStep101(int step)
    {
        Message = "Start Vaha 2";
        if (!Scale2.IsReady()) //kontrola ci je ready
        {
            Log.Logger.ForContext("Name", Name)
                .Error($"Váha 2 nie je Ready  status:[{((CDataScale)Scale2.Data).StatusMainProc}]");
            return 0;
        }

        Scale2.Operation.Master.SendCommand(EMasterCommand.Produkcia); 

        if (!Scale2.WaitForProcStatus(EProcStatus.Busy, 2000))
        {
            Log.Logger.ForContext("Name", Name)
                .Error($"Váha 2 nie je Busy  status:[{((CDataScale)Scale2.Data).StatusMainProc}]");
            return 0;
        }

        Log.Logger.ForContext("Name", Name)
            .Information($"Váha 2 je Busy  status:[{((CDataScale)Scale2.Data).StatusMainProc}]");
        return 110;
    } //Start Vaha 2 ->110

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

        if (Scale1.IsNoMaterial() && Scale2.IsNoMaterial())//ak su obe vahy prazdne koniec
        {
            Log.Logger.ForContext("Name", Name).Error($"Obe Vahy nemaju material");
            Log.Logger.ForContext("Name", Name).Error($"Váha 1  status:[{((CDataScale)Scale1.Data).StatusMainProc}]");
            Log.Logger.ForContext("Name", Name).Error($"Váha 2  status:[{((CDataScale)Scale2.Data).StatusMainProc}]");
            return 0;
        }
      
        if (Scale1.IsFull() || Scale2.IsFull())
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

        if (Scale1.IsFull() && Scale2.IsFull())
        {
            if (_lastUsedScale == 2) return 150;
            if (_lastUsedScale == 1) return 250;
        }

        if (Scale1.IsFull()) return 150;
        if (Scale2.IsFull()) return 250;

        Log.Logger.ForContext("Name", Name)
            .Error($"Nie je pripravena Váha1 :[{Scale1.GetStatus()}]  Váha 2  status:[{Scale2.GetStatus()}]");
        return 0;
    } //Vyber vahy na vysypanie Vaha1->150 , Vaha2->250

    // ---------------------------------------------------------
    // VETVA 1: VYSYPANIE VÁHA 1
    // ---------------------------------------------------------

    private int MainStep150(int step)
    {
        Message = "Váha 1: Povel na vysypanie (Next)";
        Scale1.Operation.Master.SendCommand(EMasterCommand.Next);
        _lastUsedScale = 1;
        return 160;
    } //Váha 1: Povel na vysypanie (Next) -> 160

    private int MainStep160(int step)
    {
        Message = "Váha 1: Čakanie na štart sypania (Occupied)";
        if ( Scale1.IsOcupied())
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
        if (Scale1.IsFree())
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
        Scale2.Operation.Master.SendCommand(EMasterCommand.Next);
        _lastUsedScale = 2;
        return 260;
    } //Váha 2: Povel na vysypanie (Next) - >260

    private int MainStep260(int step)
    {
        Message = "Váha 2: Čakanie na štart sypania (Occupied)";
        if (Scale2.IsOcupied())
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
        if (Scale2.IsFree())
            return 280;

        if (Scale2.IsError())
        {
            Log.Logger.ForContext("Name", Name).Error("Váha 2 hlási chybu (Error) počas sypania.");
            return 0;
        }

        return step;
    } //Váha 2: Čakanie na dokončenie sypania (Busy + Free) ->280

    private int MainStep280(int step)
    {
        Message = "Uvoľnenie zóny pre Lis";
        IL.ZonePress.Release(EnZoneOwner.Scale, EnZoneStatus.InputFull);
        return 110; // Návrat do idle slučky
    } //Uvoľnenie zóny pre Lis - >110

    private int MainStep300(int step)
    {
        Message = "Ukoncenie cinnosti";
        Scale1.Operation.Master.SendCommand(EMasterCommand.Stop);
        Scale2.Operation.Master.SendCommand(EMasterCommand.Stop);

        return 0; // Návrat do idle slučky
    }

    [RelayCommand]
    public void SaveParameters()
    {
        SaveParametersToFile("ParametersScale.json", ParametersScale);
    }

    [RelayCommand]
    public void LoadParameters()
    {
        LoadParametersFromFile("ParametersScale.json", ParametersScale);
    }
}