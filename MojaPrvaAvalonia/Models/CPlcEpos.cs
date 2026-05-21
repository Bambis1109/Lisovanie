using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EposCmd.Net;
using MojaPrvaAvalonia.ViewModels;
using Serilog;

namespace MojaPrvaAvalonia.Models;

public partial class CPlcEpos : CPlc
{
    public List<CDeviceEpos4> Motors { get; } = new();
    public ObservableCollection<UcDeviceEpos4ViewModel> MotorViewModels { get; } = new();

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

        // Vykonanie CAN/Sériovej komunikácie na pozadí - Reset
        await Task.Run(async () =>
        {
            ResetNodes(); // (NMT Reset Node)
            await Task.Delay(500);
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

    // Nová metóda: Iba odošle NMT príkaz na štart (CS = 0x01)
    public async Task StartNodesAsync()
    {
        foreach (var motor in Motors)
        {
            motor.LowLayer?.Can?.SendNmtService(ECommandSpecifier.NcsStartRemoteNode);
        
            // 3. 'await' teraz môže legálne fungovať
            await Task.Delay(10);
        }
    }

    // Upravená metóda: Iba asynchrónne čaká na potvrdenie stavu Operational
    protected async Task<enmError> WaitForStartNodeAsync()
    {
        var tasks = Motors.Select(async item =>
        {
            for (int i = 0; i < 100; i++) // Prechod do Operational je rýchly, stačí 100x50ms (5 sekúnd)
            {
                await Task.Delay(50);
                try
                {
                   // if (item.LowLayer?.Can?.GetNMTState() == ENmtStatus.NcsOPERATIONAL)
                   if(item.LowLayer.Can.GetNMTState() == ENmtStatus.NcsOPERATIONAL)
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

                // OPRAVA 1: Aktívne sa pýtame Ixxat API na reálny NMT stav uzla.
                // Toto číta reálny stav z Heartbeatu, ktorý Ixxat drží vo svojej pamäti.
                ENmtStatus status = item.LowLayer.Can.GetNMTState();

                // Aktualizujeme lokálnu premennú, aby UI a zvyšok aplikácie mali správny stav
                item.Data.NmtStatus = status;

                // OPRAVA 2: Akonáhle EPOS4 dokončí bootovanie, prejde do Pre-Operational (alebo Bootup)
                if (status == ENmtStatus.NcsBOOTUP || 
                    status == ENmtStatus.NcsPREOPERATIONAL || 
                    status == ENmtStatus.NcsOPERATIONAL)
                {
                    // OPRAVA 3: BEZPEČNOSTNÁ POISTKA
                    // Uzol síce žije a posiela Heartbeat, ale jeho SDO server sa môže ešte inicializovať.
                    // Počkáme 500ms pred prvým SDO dotazom, inak by sme dostali SDO Abort (garbage dáta).
                    await Task.Delay(500);

                    // Teraz môžeme bezpečne vyčítať FW verziu cez SDO
                    int fw = 0;
                    if (item.Operation?.MotionInfo != null)
                    {
                        try 
                        {
                            fw = item.Operation.MotionInfo.GetFwVersion();
                        }
                        catch (Exception sdoEx)
                        {
                            // Ak SDO zlyhá, nevadí, hlavne že uzol žije a komunikuje cez NMT
                            Log.Logger.ForContext("Name", Name).Warning($"Node {item.NodeId} žije, ale SDO čítanie FW zlyhalo: {sdoEx.Message}");
                            fw = 0xFFFF; // Dummy hodnota
                        }
                    }

                    Log.Logger.ForContext("Name", Name).Information(
                        $"Node {item.NodeId} ({item.Name}) úspešne nabootoval (Stav: {status}). FW:[0x{fw:X4}]");

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
                .Fatal($"Node {item.NodeId} ({item.Name}) nenabootoval v časovom limite (3s)! Posledný známy stav: {item.Data.NmtStatus}");
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
    /// <summary>
    /// Skontroluje, či sú všetky motory v stave OPERATIONAL.
    /// Pri prvej chybe okamžite preruší kontrolu a vyhodí výnimku.
    /// </summary>
    public void TestOperationModeAllMotor()
    {
        foreach (var motor in Motors)
        {
            // Ak motor nie je Operational, okamžite končíme (Fail-Fast)
            if (motor.LowLayer?.Can?.GetNMTState() != ENmtStatus.NcsOPERATIONAL)
            {
                Log.Logger.ForContext("Name", Name).Error($"TestOperationMode zlyhal: Node:{motor.NodeId} nie je OPERATIONAL.");
                throw new InvalidOperationException("Zbernica nie je pripravená. Jeden alebo viacero motorov nie je v stave OPERATIONAL.");
            }
        }
    }

    public void EnableAllMotors()
    {
        // 1. PRE-FLIGHT CHECK: Ak zlyhá, okamžite vyhodí výnimku a metóda končí.
        TestOperationModeAllMotor();

        // 2. AKCIA: Zapínanie motorov
        foreach (var motor in Motors)
        {
            try
            {
                motor.Operation?.StateMachine?.SetEnableState();
            }
            catch (Exception ex)
            {
                // Ak zlyhá samotný príkaz Enable (napr. motor je vo Fault stave),
                // zalogujeme to a okamžite prerušíme cyklus (Fail-Fast).
                Log.Logger.ForContext("Name", Name).Error($"Enable zlyhal na Node:{motor.NodeId}");
                throw new InvalidOperationException("EnableAllMotors zlyhalo. Akcia bola prerušená.");
            }
        }
    }
    
     public void DisableAllMotors()
    {
        bool hasError = false;

        foreach (var motor in Motors)
        {
            try
            {
                // Kontrolujeme stav priamo tu, aby sme neprerušili cyklus pre ostatné motory
                if (motor.LowLayer?.Can?.GetNMTState() != ENmtStatus.NcsOPERATIONAL)
                {
                    Log.Logger.ForContext("Name", Name).Error($"Disable zlyhal: Node:{motor.NodeId} nie je OPERATIONAL.");
                    hasError = true;
                    continue; // Preskočíme tento motor, ale POKRAČUJEME na ďalší!
                }

                motor.Operation?.StateMachine?.SetDisableState();
            }
            catch (Exception ex)
            {
                Log.Logger.ForContext("Name", Name).Error($"Disable zlyhal na Node:{motor.NodeId}");
                hasError = true; // Zaznamenáme chybu, ale POKRAČUJEME na ďalší motor!
            }
        }

        // Výnimku vyhodíme až keď sme sa pokúsili vypnúť VŠETKY motory
        if (hasError)
        {
            throw new InvalidOperationException("DisableAllMotors zlyhalo na jednom alebo viacerých uzloch.");
        }
    }

    public void StopAllMotors()
    {
        bool hasError = false;

        foreach (var motor in Motors)
        {
            try
            {
                if (motor.LowLayer?.Can?.GetNMTState() != ENmtStatus.NcsOPERATIONAL)
                {
                    Log.Logger.ForContext("Name", Name).Error($"Stop zlyhal: Node:{motor.NodeId} nie je OPERATIONAL.");
                    hasError = true;
                    continue; // Preskočíme tento motor, ale POKRAČUJEME na ďalší!
                }

                motor.Operation?.StateMachine?.SetQuickStopState();
            }
            catch (Exception ex)
            {
                Log.Logger.ForContext("Name", Name).Error($"Stop zlyhal na Node:{motor.NodeId}");
                hasError = true; // Zaznamenáme chybu, ale POKRAČUJEME na ďalší motor!
            }
        }

        // Výnimku vyhodíme až keď sme sa pokúsili zastaviť VŠETKY motory
        if (hasError)
        {
            throw new InvalidOperationException("StopAllMotors zlyhalo na jednom alebo viacerých uzloch.");
        }
    }
    
    public void ClearAllFaults()
    {
        TestOperationModeAllMotor();

        foreach (var motor in Motors)
        {
            try
            {
                motor.Operation?.StateMachine?.ClearFault();
            }
            catch (Exception ex)
            {
                Log.Logger.ForContext("Name", Name).Error($"ClearFault zlyhal na Node:{motor.NodeId}");
                throw new InvalidOperationException("ClearAllFaults zlyhalo. Akcia bola prerušená.");
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
            Log.Logger.ForContext("Name", Name)
                .Fatal($"Node:{deviceEpos4.NodeId}, *********** List errors *************");
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