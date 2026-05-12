using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using EposCmd.Net;
using MojaPrvaAvalonia.ViewModels;
using Serilog;

namespace MojaPrvaAvalonia.Models;

public partial class CLis : CPlc
{
    public CDeviceEpos4 MotorStred { get; set; }
    public CDeviceEpos4 MotorSlave { get; set; }
    public CDeviceEpos4 MotorMaster { get; set; }
    public CParametersLis ParametersLis { get; set; } = new();
    public List<CDeviceEpos4> Motors { get; } = new();
    public ObservableCollection<UcMotorViewModel> MotorViewModels { get; } = new ObservableCollection<UcMotorViewModel>();
 
    public CLis(string name) : base(name)
    {
        MotorViewModels.Add(new UcMotorViewModel(null, "Stred"));
        MotorViewModels.Add(new UcMotorViewModel(null, "Slave"));
        MotorViewModels.Add(new UcMotorViewModel(null, "Master"));
    }
    public override async Task ConnectAsync()
    {
        await base.ConnectAsync();
        Log.Logger.ForContext("Name", Name).Debug("[CMD] Stlačené tlačidlo: Connect");

        if (StatusPlc == EnStatusPlc.Ready || StatusPlc == EnStatusPlc.Error)
        {
            if (StatusPlc == EnStatusPlc.Ready)
            {
                Log.Logger.ForContext("Name", Name).Warning("Vyžiadaný Reconnect. Stroj stráca stav Ready.");
            }

            StatusPlc = EnStatusPlc.NotInit;
        }

        Connection = EnStatusConnection.WaitToConnect;
        Message = "Pripájam zariadenia...";

        ResetCommunication();
        await Task.Delay(50);
        ResetNodes();
        await Task.Delay(50);

        var resetResult = await WaitForResetAllNodeAsync();
        if (resetResult == enmError.Error)
        {
            Log.Logger.ForContext("Name", Name).Error("Pripojenie zlyhalo: Niektoré motory neodpovedajú.");
            StatusPlc = EnStatusPlc.Error;
            Connection = EnStatusConnection.Disconnect;
            Message = "Chyba: Zariadenia neodpovedajú.";
            return;
        }

        StartNodes();
        Connection = EnStatusConnection.Connected;
        Message = "Pripojené. Čaká na Init.";
    }
    public void ResetCommunication()
    {
        foreach (var motor in Motors)
        {
            motor.LowLayer.Can.SendNmtService(ECommandSpecifier.NcsResetCommunication);
        }
    }

    public void ResetNodes()
    {
        foreach (var motor in Motors)
        {
            motor.LowLayer.Can.SendNmtService(ECommandSpecifier.NcsResetNode);
        }
    }

    public void StartNodes()
    {
        foreach (var motor in Motors)
        {
            motor.LowLayer.Can.SendNmtService(ECommandSpecifier.NcsStartRemoteNode);
        }
    }

    private async Task<enmError> WaitForResetAllNodeAsync()
    {
        var tasks = Motors.Select(async item =>
        {
            enmError resultNode = enmError.Error;

            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(100);
                try
                {
                    if (item.Operation?.MotionInfo == null) continue;

                    var fw = item.Operation.MotionInfo.GetFwVersion();
                    Log.Logger.ForContext("Name", Name).Information(
                        $"Node {item.NodeId} The device Node:{item.NodeId} ({item.Name}) FW:[{fw}] has been reset");
                    resultNode = enmError.NoError;
                    break;
                }
                catch (Exception)
                {
                }
            }

            if (resultNode == enmError.Error)
            {
                Log.Logger.ForContext("Name", Name)
                    .Fatal($"Node {item.NodeId} The device Node:{item.NodeId} ({item.Name}) has not been reset");
            }

            return resultNode;
        });

        var results = await Task.WhenAll(tasks);

        if (results.Length == 0)
        {
            Log.Logger.ForContext("Name", Name).Error("Reset zlyhal: Žiadne zariadenia na zbernici.");
            return enmError.Error;
        }

        return results.Any(r => r == enmError.Error) ? enmError.Error : enmError.NoError;
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
            case 110: return MainStep110(step);
            case 120: return MainStep120(step);
            case 130: return MainStep130(step);
            case 140: return MainStep140(step);
            case 150: return MainStep150(step);

            default: return base.RunStep(step);
        }
    }

    // ==========================================
    // METÓDY PRE INIT
    // ==========================================
    private int InitStep1(int step)
    {
        Message = "Lis: Štart inicializácie";
        StatusCycle = EnStatusCycle.Moving;
        return 10;
    }

    private int InitStep10(int step)
    {
        Message = "Lis: Kontrola hydrauliky";
        return 20;
    }

    private int InitStep20(int step)
    {
        Message = "Lis: Kontrola bezpečnostných bariér";
        return 30;
    }

    private int InitStep30(int step)
    {
        Message = "Lis: Nastavenie lisovacej sily";
        return 40;
    }

    private int InitStep40(int step)
    {
        Message = "Lis: Pripravený";
        return 99; 
    }

    // ==========================================
    // METÓDY PRE MAIN PROGRAM
    // ==========================================
    private int MainStep100(int step)
    {
        Message = "Lis: Čakám na diel";
        StatusCycle = EnStatusCycle.Moving;

        if (RequestToEnd)
        {
            Log.Logger.ForContext("Name", Name).Information("Lis: Parkujem.");
            return 0;
        }

        return 110;
    }

    private int MainStep110(int step)
    {
        Message = "Lis: Lisovanie v procese";
        return 120;
    }

    private int MainStep120(int step)
    {
        Message = "Lis: Chladenie";
        StatusCycle = EnStatusCycle.Inspecting;
        return 130;
    }

    private int MainStep130(int step)
    {
        Message = "Lis: Vyťahovanie piesta";
        return 140;
    }

    private int MainStep140(int step)
    {
        Message = "Lis: Vyhadzovanie hotového dielu";
        return 150;
    }

    private int MainStep150(int step)
    {
        Message = "Lis: Cyklus dokončený";
        return 100; 
    }
}