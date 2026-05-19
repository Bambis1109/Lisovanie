using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EposCmd.Net;
using EposCmd.Net.DeviceScaleSet;
using MojaPrvaAvalonia.ViewModels;
using Serilog;

namespace MojaPrvaAvalonia.Models;

public partial class CPlcScale : CPlc
{
    public List<CDeviceScale> Scales { get; } = new();
    // ToDo vytvorit public ObservableCollection<UcScaleViewModel> ScaleViewModels { get; } = new();

    public CPlcScale(string name) : base(name)
    {
    }

    public override async Task ConnectAsync()
    {
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

        // Vykonanie CAN/Sériovej komunikácie na pozadí - Reset
        await Task.Run(async () =>
        {
            ResetScales(); // Tvrdý reštart všetkých Scale povelom cez SDO
            await Task.Delay(500);
        });

        // 1. Čakanie na odpoved od Scales


        Connection = EnStatusConnection.Connected;
        Message = "Pripojené. Čaká na Init.";
    }

    public void ResetScales()
    {
        foreach (var scale in Scales)
        {
            scale.Operation.System.SendCommand(ESystemCommand.Restart);
        }
    }

  

   
    protected async Task<enmError> WaitForResetAllNodeAsync()
    {
        var tasks = Scales.Select(async item =>
        {
            enmError resultNode = enmError.Error;

            // Zvýšený počet pokusov na 30 (3 sekundy). EPOS4 tvrdý reštart trvá 1 až 2 sekundy.
            for (int i = 0; i < 30; i++)
            {
                await Task.Delay(100);
                try
                {
                   //ToDo Budeme sa na nieco pytat
                }
                catch (Exception)
                {
                    // Ignorujeme výnimky počas bootovania
                }
            }

            if (resultNode == enmError.Error)
            {
                Log.Logger.ForContext("Name", Name)
                    .Fatal($"Scale {item.NodeId} ({item.Name}) nenabootoval v časovom limite (3s)!");
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

  
    public override void FinishNOKHandle()
    {
        base.FinishNOKHandle();
        
    }

    public override void FinishOKHandle()
    {
        base.FinishOKHandle();
    }
}