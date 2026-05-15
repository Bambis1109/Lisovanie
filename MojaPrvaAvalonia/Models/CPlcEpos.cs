using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using EposCmd.Net;
using MojaPrvaAvalonia.ViewModels;
using Serilog;

namespace MojaPrvaAvalonia.Models;

public partial class CPlcEpos : CPlc
{
    public List<CDeviceEpos4> Motors { get; } = new();
    public ObservableCollection<UcMotorViewModel> MotorViewModels { get; } = new();

    public CPlcEpos(string name) : base(name)
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

        // Vykonanie CAN/Sériovej komunikácie na pozadí
        await Task.Run(async () =>
        {
            ResetNodes(); // Tvrdý reštart všetkých EPOS4
            await Task.Delay(500);
        });

        // 1. Čakanie na Boot-up (Pre-Operational)
        var resetResult = await WaitForResetAllNodeAsync();
        if (resetResult == enmError.Error)
        {
            Log.Logger.ForContext("Name", Name).Error("Pripojenie zlyhalo: Niektoré motory nenabootovali.");
            StatusPlc = EnStatusPlc.Error;
            Connection = EnStatusConnection.Disconnect;
            Message = "Chyba: Zariadenia neodpovedajú po resete.";
            return;
        }

        // 2. Spustenie uzlov a overenie stavu Operational
        var startResult = await StartNodesAsync();
        if (startResult == enmError.Error)
        {
            Log.Logger.ForContext("Name", Name)
                .Error("Pripojenie zlyhalo: Niektoré motory neprešli do stavu OPERATIONAL.");
            StatusPlc = EnStatusPlc.Error;
            Connection = EnStatusConnection.Disconnect;
            Message = "Chyba: Zariadenia neštartujú.";
            return;
        }

        Connection = EnStatusConnection.Connected;
        Message = "Pripojené. Čaká na Init.";
    }

    public void ResetNodes()
    {
        foreach (var motor in Motors)
        {
            motor.LowLayer?.Can?.SendNmtService(ECommandSpecifier.NcsResetNode);
        }
    }

    // Vylepšená metóda: Nielen pošle príkaz, ale aj overí, či sa motory naozaj spustili
    public async Task<enmError> StartNodesAsync()
    {
        foreach (var motor in Motors)
        {
            motor.LowLayer?.Can?.SendNmtService(ECommandSpecifier.NcsStartRemoteNode);
        }

        var tasks = Motors.Select(async item =>
        {
            for (int i = 0; i < 100; i++) // Prechod do Operational je rýchly, stačí 10x50ms
            {
                await Task.Delay(50);
                try
                {
                    if (item.LowLayer?.Can?.GetNMTState() == ENmtStatus.NcsOPERATIONAL)
                    {
                        Log.Logger.ForContext("Name", Name)
                            .Information($"Node {item.NodeId} ({item.Name}) je OPERATIONAL.");
                        return enmError.NoError;
                    }
                }
                catch (Exception)
                {
                    /* Ignorujeme dočasné chyby API */
                }
            }

            Log.Logger.ForContext("Name", Name)
                .Error($"Node {item.NodeId} ({item.Name}) neprešiel do stavu OPERATIONAL!");
            return enmError.Error;
        });

        var results = await Task.WhenAll(tasks);
        return results.Any(r => r == enmError.Error) ? enmError.Error : enmError.NoError;
    }

    protected async Task<enmError> WaitForResetAllNodeAsync()
    {
        var tasks = Motors.Select(async item =>
        {
            enmError resultNode = enmError.Error;

            // Zvýšený počet pokusov na 30 (3 sekundy). EPOS4 tvrdý reštart trvá 1 až 2 sekundy.
            for (int i = 0; i < 30; i++)
            {
                await Task.Delay(100);
                try
                {
                    if (item.LowLayer?.Can == null) continue;

                    // Pýtame sa Ixxat API na aktuálny NMT stav uzla (nevyvoláva SDO komunikáciu)
                    ENmtStatus status = item.LowLayer.Can.GetNMTState();

                    // Akonáhle EPOS4 dokončí bootovanie, sám odošle Boot-up správu a prejde do Pre-Operational
                    if (status == ENmtStatus.NcsPREOPERATIONAL)
                    {
                        // Teraz môžeme bezpečne vyčítať FW verziu cez SDO
                        int fw = 0;
                        if (item.Operation?.MotionInfo != null)
                        {
                            fw = item.Operation.MotionInfo.GetFwVersion();
                        }

                        Log.Logger.ForContext("Name", Name).Information(
                            $"Node {item.NodeId} ({item.Name}) úspešne nabootoval. FW:[0x{fw:X4}]");

                        resultNode = enmError.NoError;
                        break;
                    }
                }
                catch (Exception)
                {
                    // Ignorujeme výnimky počas bootovania
                }
            }

            if (resultNode == enmError.Error)
            {
                Log.Logger.ForContext("Name", Name)
                    .Fatal($"Node {item.NodeId} ({item.Name}) nenabootoval v časovom limite (3s)!");
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

    public void EnableAllMotors()
    {
        foreach (var motor in Motors)
        {
            try
            {
                // Ochrana: Ak motor nie je Operational, ignoruje PDO. Nemá zmysel posielať príkazy.
                if (motor.LowLayer?.Can?.GetNMTState() != ENmtStatus.NcsOPERATIONAL)
                {
                    Log.Logger.ForContext("Name", Name)
                        .Warning($"Node:{motor.NodeId} nie je Operational. Preskakujem Enable.");
                    continue;
                }

                motor.Operation?.StateMachine?.SetEnableState();
            }
            catch (CDeviceException dex)
            {
                Log.Logger.ForContext("Name", Name)
                    .Fatal(dex, $"EnableAllMotors DeEx Node:{motor.NodeId}  {dex.ErrorMessage}");
            }
            catch (Exception ex)
            {
                Log.Logger.ForContext("Name", Name).Fatal(ex, $"EnableAllMotors Node:{motor.NodeId}");
            }
        }
    }

    public void DisableAllMotors()
    {
        foreach (var motor in Motors)
        {
            try
            {
                if (motor.LowLayer?.Can?.GetNMTState() != ENmtStatus.NcsOPERATIONAL) continue;
                motor.Operation?.StateMachine?.SetDisableState();
            }
            catch (CDeviceException dex)
            {
                Log.Logger.ForContext("Name", Name)
                    .Fatal(dex, $"DisableAllMotors DeEx Node:{motor.NodeId}  {dex.ErrorMessage}");
            }
            catch (Exception ex)
            {
                Log.Logger.ForContext("Name", Name).Fatal(ex, $"DisableAllMotors Node:{motor.NodeId}");
            }
        }
    }

    public void StopAllMotors()
    {
        foreach (var motor in Motors)
        {
            try
            {
                if (motor.LowLayer?.Can?.GetNMTState() != ENmtStatus.NcsOPERATIONAL) continue;
                motor.Operation?.StateMachine?.SetQuickStopState();
            }
            catch (CDeviceException dex)
            {
                Log.Logger.ForContext("Name", Name)
                    .Fatal(dex, $"StopAllMotors DeEx Node:{motor.NodeId}  {dex.ErrorMessage}");
            }
            catch (Exception ex)
            {
                Log.Logger.ForContext("Name", Name).Fatal(ex, $"StopAllMotors Node:{motor.NodeId}");
            }
        }
    }

    public void ClearAllFaults()
    {
        foreach (var motor in Motors)
        {
            try
            {
                if (motor.LowLayer?.Can?.GetNMTState() != ENmtStatus.NcsOPERATIONAL) continue;
                motor.Operation?.StateMachine?.ClearFault();
            }
            catch (CDeviceException dex)
            {
                Log.Logger.ForContext("Name", Name)
                    .Fatal($"ClearAllFaults DeEx Node:{motor.NodeId}  {dex.ErrorMessage}");
            }
            catch (Exception ex)
            {
                Log.Logger.ForContext("Name", Name).Fatal($"ClearAllFaults Node:{motor.NodeId}");
            }
        }
    }

    internal void LogErrors()
    {
        foreach (var item in Motors)
        {
            ShowErrorsEpos4(item);
        }
    }

    public void ShowErrorsEpos4(CDeviceEpos4 deviceEpos4)
    {
        try
        {
            // 1. Zisti počet chýb uložených v zariadení (Objekt 0x1003:00)
            byte errorCount = deviceEpos4.Operation.DeviceErrorHandling.GetNbOfDeviceError();
            if (errorCount == 0) return;

            // 2. Prejdi všetky chyby (indexy 1 až errorCount, max 5)
            for (byte i = 1; i <= errorCount && i <= 5; i++)
            {
                // Načítaj 16-bitový kód chyby
                ushort errorCode = deviceEpos4.Operation.DeviceErrorHandling.GetDeviceErrorCode(i);

                // Získaj textový popis z vašej switch procedúry
                string description = deviceEpos4.Operation.DeviceErrorHandling.GetErrorDescription(errorCode);

                // Výpis do logu: NodeId, poradie v histórii, hex kód a popis
                Log.Logger.ForContext("Name", Name)
                    .Fatal($"Node:{deviceEpos4.NodeId}, Error #{i}: [0x{errorCode:X4}] - {description}");
            }
        }
        catch (CDeviceException dex)
        {
            Log.Logger.ForContext("Name", Name)
                .Fatal($"ShowErrorsEpos4 DeEx Node:{deviceEpos4.NodeId}  {dex.ErrorMessage}");
        }
        catch (Exception ea)
        {
            Log.Logger.ForContext("Name", Name).Error($"Node:{deviceEpos4.NodeId}, ShowErrors failed => {ea.Message}");
        }
    }

    public override void FinishNOKHandle()
    {
        base.FinishNOKHandle();
        DisableAllMotors();
        LogErrors();
    }

    public override void FinishOKHandle()
    {
        base.FinishOKHandle();
    }
}