using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EposCmd.Net;
using EposCmd.Net.DeviceScaleSet;
using Lisovanie.ViewModels;
using Serilog;

namespace Lisovanie.Models;

public partial class CPlcScale : CPlc
{
    public List<CDeviceScale> Scales { get; } = new();
    public ObservableCollection<UcDeviceScaleViewModel> ScaleViewModels { get; } = new();

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
            ResetScales(); // (NMT Reset Node)
            await Task.Delay(200);
        });
        // 1. Čakanie na Boot-up 
        var resetResult = await WaitForResetAllNodeAsync();
        if (resetResult == enmError.Error)
        {
            Log.Logger.ForContext("Name", Name).Error("Pripojenie zlyhalo: Niektoré motory nenabootovali.");
            StatusPlc = EnStatusPlc.Error;
            Connection = EnStatusConnection.Disconnect;
            Message = "Chyba: Zariadenia neodpovedajú po resete.";
            return;
        }

        // 2. Odoslanie povelu na štart uzlov (NMT Start Remote Node)
        await Task.Run(async () =>
        {
            await StartNodesAsync(); // Odoslanie príkazu na prechod do Operational
            await Task.Delay(500); // Poskytnutie času zbernici a meničom na spracovanie
        });
        // 3. Čakanie na overenie stavu Operational
        var startResult = await WaitForStartNodeAsync();
        if (startResult == enmError.Error)
        {
            Log.Logger.ForContext("Name", Name)
                .Error("Pripojenie zlyhalo: Niektoré vahy neprešli do stavu OPERATIONAL.");
            StatusPlc = EnStatusPlc.Error;
            Connection = EnStatusConnection.Disconnect;
            Message = "Chyba: Zariadenia neštartujú.";
            return;
        }

        await SetHearbeatNodes(200, true);
        Connection = EnStatusConnection.Connected;
        Message = "Pripojené. Čaká na Init.";
    }

    public void ResetScales()
    {
        foreach (var item in Scales)
        {
            item.LowLayer?.Can?.SendNmtService(ECommandSpecifier.NcsResetNode);
        }
    }

    public async Task StartNodesAsync()
    {
        foreach (var item in Scales)
        {
            item.LowLayer?.Can?.SendNmtService(ECommandSpecifier.NcsStartRemoteNode);

            // 3. 'await' teraz môže legálne fungovať
            await Task.Delay(10);
        }
    }

    public async Task SetHearbeatNodes(ushort hearbeatTime,bool enable)
    {
        foreach (var item in Scales)
        {
            item.LowLayer?.Can?.SetHeartbeat(hearbeatTime,enable);
        }
    }

    protected async Task<enmError> WaitForStartNodeAsync()
    {
        var tasks = Scales.Select(async item =>
        {
            for (int i = 0; i < 100; i++) // Prechod do Operational je rýchly, stačí 100x50ms (5 sekúnd)
            {
                await Task.Delay(50);
                try
                {
                    // if (item.LowLayer?.Can?.GetNMTState() == ENmtStatus.NcsOPERATIONAL)
                    if (item.LowLayer.Can.GetNMTState() == ENmtStatus.NcsOPERATIONAL)
                    {
                        Log.Logger.ForContext("Name", Name)
                            .Information($"Node {item.NodeId} ({item.Name}) je OPERATIONAL.");
                        return enmError.NoError;
                    }
                }
                catch (Exception)
                {
                    /* Ignorujeme dočasné chyby API počas dopytovania */
                }
            }

            Log.Logger.ForContext("Name", Name)
                .Error($"Node {item.NodeId} ({item.Name}) neprešiel do stavu OPERATIONAL v časovom limite!");
            return enmError.Error;
        });

        var results = await Task.WhenAll(tasks);
        return results.Any(r => r == enmError.Error) ? enmError.Error : enmError.NoError;
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
                    if (item.LowLayer?.Can == null) continue;

                    // OPRAVA 1: Aktívne sa pýtame Ixxat API na reálny NMT stav uzla.
                    // Toto číta reálny stav z Heartbeatu, ktorý Ixxat drží vo svojej pamäti.
                    ENmtStatus status = item.LowLayer.Can.GetNMTState();

                    // Aktualizujeme lokálnu premennú, aby UI a zvyšok aplikácie mali správny stav
                    item.Data.NmtStatus = status;

                    // OPRAVA 2: Akonáhle EPOS4 dokončí bootovanie, prejde do Pre-Operational (alebo Bootup)
                    if (status == ENmtStatus.NcsPREOPERATIONAL ||
                        status == ENmtStatus.NcsOPERATIONAL)
                    {
                        // OPRAVA 3: BEZPEČNOSTNÁ POISTKA
                        // Uzol síce žije a posiela Heartbeat, ale jeho SDO server sa môže ešte inicializovať.
                        // Počkáme 500ms pred prvým SDO dotazom, inak by sme dostali SDO Abort (garbage dáta).
                        await Task.Delay(500);

                        Log.Logger.ForContext("Name", Name).Information(
                            $"Node {item.NodeId} ({item.Name}) úspešne nabootoval (Stav: {status}). FW:[0x0000]");

                        resultNode = enmError.NoError;
                        break; // Úspech, vyskakujeme z for-cyklu
                    }
                }
                catch (Exception)
                {
                    // Ignorujeme výnimky počas bootovania. 
                    // GetNMTState môže hodiť výnimku (Abort), ak uzol ešte vôbec nekomunikuje a Ixxat ho eviduje ako Disconnected.
                }
            }

            if (resultNode == enmError.Error)
            {
                Log.Logger.ForContext("Name", Name)
                    .Fatal(
                        $"Node {item.NodeId} ({item.Name}) nenabootoval v časovom limite (3s)! Posledný známy stav: {item.Data.NmtStatus}");
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

    public void SendScaleCommand(CDeviceScale scale, Action<CDeviceScale> commandAction, string commandName)
    {
        if (scale.LowLayer.Can.GetNMTState() != ENmtStatus.NcsOPERATIONAL)
        {
            Log.Logger.ForContext("Name", Name)
                .Warning($"Povel {commandName} ignorovaný. Váha {scale.NodeId} nie je OPERATIONAL.");
            return;
        }

        try
        {
            commandAction.Invoke(scale);
            Log.Logger.ForContext("Name", Name).Debug($"Povel odoslaný: {scale.Name} -> {commandName}");
        }
        catch (Exception ex)
        {
            Log.Logger.ForContext("Name", Name)
                .Error($"Chyba pri odosielaní povelu {commandName} na {scale.Name}: {ex.Message}");
        }
    }

    // Príklady konkrétnych metód pre UI (ViewModel ich bude volať)

    public void StartDoserProduction(CDeviceScale scale)
        => SendScaleCommand(scale, s => s.Operation.Doser.SendCommand(EDoserCommand.Prod), "Doser_Prod");

    public void ClearDoserCommand(CDeviceScale scale)
        => SendScaleCommand(scale, s => s.Operation.Doser.SendCommand(EDoserCommand.Clear), "Doser_Clear");

    public void UnloadBoom(CDeviceScale scale)
        => SendScaleCommand(scale, s => s.Operation.Boom.SendCommand(EBoomCommand.Vyloz1), "Boom_Vyloz1");

    public override void FinishNOKHandle()
    {
        base.FinishNOKHandle();
    }

    public override void FinishOKHandle()
    {
        base.FinishOKHandle();
    }
}