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

        // Vykonanie CAN/Sériovej komunikácie na pozadí, aby sa neblokovalo UI vlákno
        await Task.Run(async () =>
        {
            ResetCommunication();
            await Task.Delay(50);
            ResetNodes();
            await Task.Delay(50);
        });

        var resetResult = await WaitForResetAllNodeAsync();
        if (resetResult == enmError.Error)
        {
            Log.Logger.ForContext("Name", Name).Error("Pripojenie zlyhalo: Niektoré motory neodpovedajú.");

            // Dispatcher sa nepoužíva, keďže CPlc property používajú štandardný INotifyPropertyChanged
            // avšak Avalonia dokáže niektoré property viazať z iného vlákna. Pre istotu nastavíme
            // UI premenné vonku z Task.Run
            StatusPlc = EnStatusPlc.Error;
            Connection = EnStatusConnection.Disconnect;
            Message = "Chyba: Zariadenia neodpovedajú.";
            return;
        }

        // Opäť na pozadí
        await Task.Run(() => StartNodes());

        Connection = EnStatusConnection.Connected;
        Message = "Pripojené. Čaká na Init.";
    }

    public void ResetCommunication()
    {
        foreach (var motor in Motors)
        {
            motor.LowLayer?.Can?.SendNmtService(ECommandSpecifier.NcsResetCommunication);
        }
    }

    public void ResetNodes()
    {
        foreach (var motor in Motors)
        {
            motor.LowLayer?.Can?.SendNmtService(ECommandSpecifier.NcsResetNode);
        }
    }

    public void StartNodes()
    {
        foreach (var motor in Motors)
        {
            motor.LowLayer?.Can?.SendNmtService(ECommandSpecifier.NcsStartRemoteNode);
        }
    }

    protected async Task<enmError> WaitForResetAllNodeAsync()
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

    public void EnableAllMotors()
    {
        foreach (var motor in Motors)
        {
            try
            {
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
                .Fatal($"ClearAllFaults DeEx Node:{deviceEpos4.NodeId}  {dex.ErrorMessage}");
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