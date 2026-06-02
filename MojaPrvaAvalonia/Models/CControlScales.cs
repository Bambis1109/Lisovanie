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
            case 40: return InitStep40(step);

            // ==========================================
            // MAIN SEKVENCIA (Kroky 100+)
            // ==========================================
            case 100: return MainStep100(step);
            case 101: return MainStep101(step);
            case 110: return MainStep110(step);
            case 120: return MainStep120(step);
            case 130: return MainStep130(step);
            case 140: return MainStep140(step);

            // ČAKANIE NA MATERIÁL
            case 145: return MainStep145(step);
            case 148: return MainStep148(step);

            // VETVA 1 (Váha 1)
            case 150: return MainStep150(step);
            case 160: return MainStep160(step);
            case 170: return MainStep170(step);
            case 180: return MainStep180(step);

            // VETVA 2 (Váha 2)
            case 250: return MainStep250(step);
            case 260: return MainStep260(step);
            case 270: return MainStep270(step);
            case 280: return MainStep280(step);

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
    } // Init

    private int InitStep10(int step)
    {
        Message = "Štart inicializácie váh...";
        // 1. Odoslanie povelov (Fire-and-Forget)
        Scale1.Operation.Master.SendCommand(EMasterCommand.Init);
        Scale2.Operation.Master.SendCommand(EMasterCommand.Init);
        return 20;
    }

    private int InitStep20(int step)
    {
        Message = "Čakám na dokončenie inicializácie (STM32)...";
        StatusCycle = EnStatusCycle.WaitForStep;
        // Čakáme max 15 sekúnd na k
        Scale1.WaitForInitAttained(15000);
        Scale2.WaitForInitAttained(15000);
        return 30;
    }

    private int InitStep30(int step)
    {
        Message = "Čakám na dokončenie inicializácie (STM32)...";
        StatusCycle = EnStatusCycle.WaitForStep;
        return 40;
    }

    private int InitStep40(int step)
    {
        Message = "Inicializácia úspešná";
        Log.Logger.ForContext("Name", Name).Information("Obe váhy boli úspešne inicializované.");
        return 99; // Skočí do finálneho kroku, kde CPlc nastaví stav EnStatusPlc.Ready
    }

    // ==========================================
    // METÓDY PRE MAIN PROGRAM
    // ==========================================

    private int MainStep100(int step)
    {
        Message = "Start Vaha 1";
        if (((CDataScale)Scale1.Data).StatusMainProc != EProcStatus.Ready) //kontrola ci je ready
        {
            Log.Logger.ForContext("Name", Name)
                .Error($"Váha 1 nie je Ready  status:[{((CDataScale)Scale1.Data).StatusMainProc}]");
            return 0;
        }

        Scale1.Operation.Master.SendCommand(EMasterCommand.Produkcia); // start vahy 1

        if (!Scale1.WaitForProcStatus(EProcStatus.Busy, 2000))
        {
            Log.Logger.ForContext("Name", Name)
                .Error($"Váha 1 nie je Busy  status:[{((CDataScale)Scale1.Data).StatusMainProc}]");
            return 0;
        }

        Log.Logger.ForContext("Name", Name)
            .Error($"Váha 1 je Busy  status:[{((CDataScale)Scale1.Data).StatusMainProc}]");
        return 101;
    }

    private int MainStep101(int step)
    {
        Message = "Start Vaha 2";
        if (((CDataScale)Scale2.Data).StatusMainProc != EProcStatus.Ready) //kontrola ci je ready
        {
            Log.Logger.ForContext("Name", Name)
                .Error($"Váha 2 nie je Ready  status:[{((CDataScale)Scale2.Data).StatusMainProc}]");
            return 0;
        }

        Scale2.Operation.Master.SendCommand(EMasterCommand.Produkcia); // start vahy 1

        if (!Scale2.WaitForProcStatus(EProcStatus.Busy, 2000))
        {
            Log.Logger.ForContext("Name", Name)
                .Error($"Váha 2 nie je Busy  status:[{((CDataScale)Scale2.Data).StatusMainProc}]");
            return 0;
        }

        Log.Logger.ForContext("Name", Name)
            .Error($"Váha 2 je Busy  status:[{((CDataScale)Scale2.Data).StatusMainProc}]");
        return 110;
    }

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
    }

    private int MainStep120(int step)
    {
        Message = "Cakam na pripravenie davky";
        var data1 = (CDataScale)Scale1.Data;
        var data2 = (CDataScale)Scale2.Data;

        // 1. KONTROLA MATERIÁLU (Ak chýba, ideme do čakacieho stavu)
        if (data1.StatusMainProc == EProcStatus.NoMaterial && data2.StatusMainProc == EProcStatus.NoMaterial)
        {
            Log.Logger.ForContext("Name", Name).Error($"Obe Vahy nemaju material");
            Log.Logger.ForContext("Name", Name).Error($"Váha 1  status:[{((CDataScale)Scale1.Data).StatusMainProc}]");
            Log.Logger.ForContext("Name", Name).Error($"Váha 2  status:[{((CDataScale)Scale2.Data).StatusMainProc}]");
            return 0;
        }

        // 2. KONTROLA HOTOVEJ DÁVKY
        bool isFull1 = data1.StatusMainMat == EMatStatus.Full;
        bool isFull2 = data2.StatusMainMat == EMatStatus.Full;

        // Ak aspon jedna váha ma  dávku, zostávame v kroku 130.

        if (isFull1 || isFull2)
        {
            return 140;
        }

        return step;
    }

    private int MainStep130(int step)
    {
        Message = "......";
        return 140; // Zóna ešte nie je voľná, čakáme (10ms sleep)
    }

    private int MainStep140(int step)
    {
        Message = "Prioritizácia dávok";
        var data1 = (CDataScale)Scale1.Data;
        var data2 = (CDataScale)Scale2.Data;

        bool isFull1 = data1.StatusMainMat == EMatStatus.Full;
        bool isFull2 = data2.StatusMainMat == EMatStatus.Full;
        if (isFull1 && isFull2)
        {
            if (_lastUsedScale == 2) return 150;
            if (_lastUsedScale == 1) return 250;
        }

        if (isFull1) return 150;
        if (isFull2) return 250;

        Log.Logger.ForContext("Name", Name).Error($"Nie je pripravena ziadna vaha");
        Log.Logger.ForContext("Name", Name).Error($"Váha 1  status:[{((CDataScale)Scale1.Data).StatusMainProc}]");
        Log.Logger.ForContext("Name", Name).Error($"Váha 2  status:[{((CDataScale)Scale2.Data).StatusMainProc}]");
        return 0;
    }

    private int MainStep145(int step)
    {
        if (RequestToEnd)
        {
            Log.Logger.ForContext("Name", Name).Information("Požiadavka na ukončenie počas čakania na materiál.");
            Scale1.Operation.Master.SendCommand(EMasterCommand.Stop);
            Scale2.Operation.Master.SendCommand(EMasterCommand.Stop);
            return 0;
        }

        if (RequestToContinue)
        {
            RequestToContinue = false;
            Message = "Odosielam povel Continue do váh...";
            Scale1.Operation.Master.SendCommand(EMasterCommand.Continue);
            Scale2.Operation.Master.SendCommand(EMasterCommand.Continue);
            return 148;
        }

        return step; // Zostáva v slučke, kým operátor nestlačí tlačidlo
    }

    private int MainStep148(int step)
    {
        Message = "Čakanie na potvrdenie od STM32...";
        var data1 = (CDataScale)Scale1.Data;
        var data2 = (CDataScale)Scale2.Data;

        // Čakáme, kým STM32 zhodí stav Nomaterial
        if (data1.StatusMainProc != EProcStatus.NoMaterial && data2.StatusMainProc != EProcStatus.NoMaterial)
        {
            StatusCycle = EnStatusCycle.Moving;
            return 130; // Návrat k čakaniu na dávku
        }

        return step;
    }

    // ---------------------------------------------------------
    // VETVA 1: VYSYPANIE VÁHA 1
    // ---------------------------------------------------------

    private int MainStep150(int step)
    {
        Message = "Váha 1: Povel na vysypanie (Next)";
        Scale1.Operation.Master.SendCommand(EMasterCommand.Next);
        _lastUsedScale = 1;
        return 160;
    }

    private int MainStep160(int step)
    {
        Message = "Váha 1: Čakanie na štart sypania (Busy + Occupied)";
        var data = (CDataScale)Scale1.Data;

        if (data.StatusMainProc == EProcStatus.Busy && data.StatusMainZone == EZoneStatus.Occupied)
            return 170;

        if (data.StatusMainProc == EProcStatus.Error)
        {
            Log.Logger.ForContext("Name", Name).Error("Váha 1 hlási chybu (Error) pri štarte sypania.");
            return 0;
        }

        return step; // Zostávame v slučke, čakáme na reakciu STM32
    }

    private int MainStep170(int step)
    {
        Message = "Váha 1: Čakanie na dokončenie sypania (Busy + Free)";
        var data = (CDataScale)Scale1.Data;

      

        // 2. ÚSPEŠNÉ DOKONČENIE
        if (data.StatusMainProc == EProcStatus.Busy && data.StatusMainZone == EZoneStatus.Free)
            return 180;

        // 3. CHYBA HARDVÉRU
        if (data.StatusMainProc == EProcStatus.Error)
        {
            Log.Logger.ForContext("Name", Name).Error("Váha 1 hlási chybu (Error) počas sypania.");
            return 0;
        }

        return step; // Sypanie prebieha, čakáme
    }

    private int MainStep180(int step)
    {
        Message = "Váha 1: Uvoľnenie zóny pre Lis";
        IL.ZonePress.Release(EnZoneOwner.Scale, EnZoneStatus.InputFull);
        return 110; // Návrat do idle slučky
    }

    // ---------------------------------------------------------
    // VETVA 2: VYSYPANIE VÁHA 2
    // ---------------------------------------------------------

    private int MainStep250(int step)
    {
        Message = "Váha 2: Povel na vysypanie (Next)";
        Scale2.Operation.Master.SendCommand(EMasterCommand.Next);
        _lastUsedScale = 2;
        return 260;
    }

    private int MainStep260(int step)
    {
        Message = "Váha 2: Čakanie na štart sypania (Busy + Occupied)";
        var data = (CDataScale)Scale2.Data;

        if (data.StatusMainProc == EProcStatus.Busy && data.StatusMainZone == EZoneStatus.Occupied)
            return 270;

        if (data.StatusMainProc == EProcStatus.Error)
        {
            Log.Logger.ForContext("Name", Name).Error("Váha 2 hlási chybu (Error) pri štarte sypania.");
            return 0;
        }

        return step;
    }

    private int MainStep270(int step)
    {
        Message = "Váha 2: Čakanie na dokončenie sypania (Ready + Free)";
        var data = (CDataScale)Scale2.Data;

     
        if (data.StatusMainProc == EProcStatus.Busy && data.StatusMainZone == EZoneStatus.Free)
            return 280;

        if (data.StatusMainProc == EProcStatus.Error)
        {
            Log.Logger.ForContext("Name", Name).Error("Váha 2 hlási chybu (Error) počas sypania.");
            return 0;
        }

        return step;
    }

    private int MainStep280(int step)
    {
        Message = "Váha 2: Uvoľnenie zóny pre Lis";
        IL.ZonePress.Release(EnZoneOwner.Scale, EnZoneStatus.InputFull);
        return 110; // Návrat do idle slučky
    }


    [RelayCommand]
    public void SaveParameters()
    {
        //ToDo  SaveParametersToFile("ParametersScale.json", ParametersScale);
    }

    [RelayCommand]
    public void LoadParameters()
    {
        //ToDo    LoadParametersFromFile("ParametersScale.json", ParametersScale);

        // NABINDOVANIE NA AI (Aktualizácia kinematiky)
    }
}