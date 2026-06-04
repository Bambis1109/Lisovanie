using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using EposCmd.Net;
using IXXAT;
using Serilog;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MojaPrvaAvalonia.Models;

public partial class CMainProgram : ObservableObject
{
  [ObservableProperty] private EnIxxatState _ixxatState = EnIxxatState.Disconnected;

    partial void OnIxxatStateChanged(EnIxxatState oldValue, EnIxxatState newValue)
    {
        // Notifikujeme každé PLC, aby prehodnotilo CanExecute pre ConnectCommand
        foreach (var plc in ZoznamPlc)
        {
            plc.ConnectCommand.NotifyCanExecuteChanged();
        }
    }

    public ObservableCollection<CPlc> ZoznamPlc { get; } = new ObservableCollection<CPlc>();
    public CDeviceManagerCO DeviceManagerCO;
    public CDeviceManagerCO DeviceManagerScale;


    public CMainProgram()
    {
        ZoznamPlc.Add(new CControlManipulator("Manipulator"));
        ZoznamPlc.Add(new CControlLis("Lis"));
        ZoznamPlc.Add(new CControlScales("Vahy"));
    }

    public async Task<bool> Connect()
    {
        Log.Information("MainProgram: Connecting to CAN...");
        IxxatState = EnIxxatState.Connecting;
        try
        {
            if (!CreateCanConector(1, 0, ref DeviceManagerCO))
            {
                IxxatState = EnIxxatState.Disconnected;
                return false;
            }

            CControlManipulator? manipulator = ZoznamPlc[0] as CControlManipulator;
            CControlLis? lis = ZoznamPlc[1] as CControlLis;

            lis.MotorStred = CreateDevices(DeviceManagerCO, 1, "Sred", 1000, 2048);
            lis.MotorSlave = CreateDevices(DeviceManagerCO, 2, "Slave", 12417.34737, 16384);
            lis.MotorMaster = CreateDevices(DeviceManagerCO, 3, "Master", 12417.34737, 16384);
            manipulator.MotorUp = CreateDevices(DeviceManagerCO, 11, "Up", 729.4278, 262144.0);
            manipulator.MotorDown = CreateDevices(DeviceManagerCO, 12, "Down", 729.4278, 262144.0);
            manipulator.MotorJaws = CreateDevices(DeviceManagerCO, 13, "Jaws", 165, 500);
            manipulator.MotorZ = CreateDevices(DeviceManagerCO, 14, "Z", 1755, 500);


            manipulator.Motors.Clear();
            manipulator.Motors.Add(manipulator.MotorUp);
            manipulator.Motors.Add(manipulator.MotorDown);
            manipulator.Motors.Add(manipulator.MotorJaws);
            manipulator.Motors.Add(manipulator.MotorZ);

            lis.Motors.Clear();
            lis.Motors.Add(lis.MotorStred);
            lis.Motors.Add(lis.MotorSlave);
            lis.Motors.Add(lis.MotorMaster);


            manipulator.MotorViewModels[0].AssignDevice(manipulator.MotorUp);
            manipulator.MotorViewModels[1].AssignDevice(manipulator.MotorDown);
            manipulator.MotorViewModels[2].AssignDevice(manipulator.MotorJaws);
            manipulator.MotorViewModels[3].AssignDevice(manipulator.MotorZ);

            lis.MotorViewModels[0].AssignDevice(lis.MotorStred);
            lis.MotorViewModels[1].AssignDevice(lis.MotorSlave);
            lis.MotorViewModels[2].AssignDevice(lis.MotorMaster);

//*************************Vahy*********************************
            if (!CreateCanConector(0, 0, ref DeviceManagerScale))
            {
                IxxatState = EnIxxatState.Disconnected;
                return false;
            }

            CControlScales? _scales = ZoznamPlc[2] as CControlScales;

            _scales.Scale1 = CreateScale(DeviceManagerScale, 6, "Scale1");
            _scales.Scale2 = CreateScale(DeviceManagerScale, 3, "Scale2");


            _scales.Scales.Clear();
            _scales.Scales.Add(_scales.Scale1);
            _scales.Scales.Add(_scales.Scale2);


            _scales.ScaleViewModels[0].AssignDevice(_scales.Scale1);
            _scales.ScaleViewModels[1].AssignDevice(_scales.Scale2);

            foreach (var vm in manipulator.MotorViewModels)
            {
                vm.StartRefresh();
            }

            foreach (var vm in lis.MotorViewModels)
            {
                vm.StartRefresh();
            }

            foreach (var vms in _scales.ScaleViewModels)
            {
                vms.StartRefresh();
            }

            IxxatState = EnIxxatState.Connected;
            return true;
        }
        catch (Exception ex)
        {
            Log.Fatal($"MainProgram Connect Error: {ex.Message}");
            IxxatState = EnIxxatState.Disconnected;
            return false;
        }
    }

    private bool CreateCanConector(int canline, int boardline, ref CDeviceManagerCO deviceManagerCO)
    {
        try

        {
            if (deviceManagerCO != null)
            {
                Log.Warning("CanOpen Master is already initialized!");
                return true;
            }

            Guid boardId = CANopenMasterAPI6.COP_1stBOARD;
            deviceManagerCO = new CDeviceManagerCO
            {
                Name = $"DM:Main",
                BaudIndex = CANopenMasterAPI6.COP_k_1000_KB,
                BoardType = XatBoards.XAT_USB2CANV2_VCI3,
                BoardId = boardId,
                CanLine = canline,
                BoardLine = boardline,
                Timeout = 500
            };

            deviceManagerCO.Init();
            var connectionInfo = deviceManagerCO.GetConnectionInfo;
            Log.Information($"Connect: {connectionInfo}");
            return true;
        }
        catch
            (Exception ex)
        {
            Log.Fatal($"Create connectors error: {ex.Message}");
            return false;
        }
    }

    private CDeviceEpos4 CreateDevices(CDeviceManagerCO deviceManagerCO, byte nodeId, string name, double gear,
        double pulse)
    {
        var motor = new CDeviceEpos4(deviceManagerCO._keyHandle, nodeId, name, gear, pulse);
        DeviceManagerCO.AddDevice(motor);
        motor.ReceiveEmergency += OnMotorEmergency;
        motor.ReceiveStatus += OnMotorStatus;
        Log.Information($"Created device: {name} (ID: {nodeId})");
        return motor;
    }

    private CDeviceScale CreateScale(CDeviceManagerCO deviceManagerCO, byte nodeId, string name)
    {
        // 1. Vytvorenie inštancie váhy (nepotrebuje gear ani pulse)
        var scale = new CDeviceScale(deviceManagerCO._keyHandle, nodeId, name);

        // 2. Pridanie do manažéra (zabezpečí routing CAN správ)
        deviceManagerCO.AddDevice(scale);

        // 3. Odber udalostí (Eventy)
        scale.ReceiveEmergency += OnScaleEmergency;
        scale.ReceiveStatus += OnScaleStatus;

        // 4. Logovanie
        Log.Information($"Created scale device: {name} (ID: {nodeId})");

        return scale;
    }

    private void OnMotorEmergency(object? sender, EventArgs e)
    {
        if (sender is CDeviceEpos4 motor)
        {
            // Získame textový popis chyby priamo z tvojej knižnice
            string errorMsg = motor.GetLastEmergencyMsg();

            // Zalogujeme to ako Error (alebo Fatal) cez Serilog
            Log.Error($"[EMCY ALARM] Motor {motor.Name}: {errorMsg}");

            // Ak ide o chybu zbernice (CAN passive / Bus off)
            if (motor.Data.LastEmergency.err_value == 0x8120 ||
                motor.Data.LastEmergency.err_value == 0x81FD)
            {
                Log.Fatal($"Kritická chyba CAN zbernice na uzle {motor.NodeId}! Skontroluj káble.");

                // OPRAVA: Presun volania Shutdown do UI vlákna!
                Dispatcher.UIThread.Post(() => { Shutdown(); });
            }
        }
    }

    private void OnScaleEmergency(object sender, EventArgs e)
    {
        if (sender is CDeviceScale scale)
        {
            // Využijeme metódu GetLastEmergencyMsg, ktorú sme definovali v CDeviceScale
            // string errorMsg = scale.GetLastEmergencyMsg();
            Log.Error($"[SCALE EMERGENCY] ");

            // Tu môžeš pridať logiku pre UI, napr. zobrazenie chybovej hlášky
        }
    }

    private void OnMotorStatus(object? sender, EventArgs e)
    {
        if (sender is CDeviceEpos4 motor)
        {
            // Tu skontrolujeme aktuálny NMT stav uzla
            if (motor.LowLayer.Can.GetNMTState() == ENmtStatus.NcsDISCONNECTED)
            {
                Log.Fatal(
                    $"[NMT ALARM] (Heartbeat timeout)  {motor.Name} (ID: {motor.NodeId})! ");

                // Reagujeme rovnako ako pri zlyhaní zbernice
                Dispatcher.UIThread.Post(() => { Shutdown(); });
            }
        }
    }

    private void OnScaleStatus(object? sender, EventArgs e)
    {
        if (sender is CDeviceScale scale)
        {
            // Tu skontrolujeme aktuálny NMT stav uzla
            if (scale.LowLayer.Can.GetNMTState() == ENmtStatus.NcsDISCONNECTED)
            {
                Log.Fatal(
                    $"[NMT ALARM] (Heartbeat timeout) {scale.Name} (ID: {scale.NodeId})! ");

            }
        }
    }

    public void Shutdown()
    {
        Log.Information("Vykonávam núdzový Shutdown systému...");
        IxxatState = EnIxxatState.BusFault;

        // 1. Zastavenie PLC slučiek
        foreach (var plc in ZoznamPlc)
        {
            try
            {
                plc.StopProgramImmediately();
            }
            catch (Exception ex)
            {
                Log.Error($"Chyba pri zastavovaní PLC {plc.Name}: {ex.Message}");
            }
        }

        // 2. Vypnutie motorov (ak zbernica žije)
        if (DeviceManagerCO != null)
        {
            foreach (var plc in ZoznamPlc)
            {
                if (plc is CControlManipulator m)
                {
                    foreach (var motor in m.Motors)
                    {
                        try
                        {
                            // Ak je zbernica skratovaná alebo motor vo Fault, toto vyhodí výnimku.
                            // Musíme ju zachytiť, inak spadne celá aplikácia!
                            motor.Operation?.StateMachine?.SetDisableState();
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(
                                $"Ignorovaná chyba pri vypínaní motora {motor.Name} (Manipulator): {ex.Message}");
                        }
                    }
                }
                else if (plc is CControlLis l)
                {
                    foreach (var motor in l.Motors)
                    {
                        try
                        {
                            motor.Operation?.StateMachine?.SetDisableState();
                        }
                        catch (Exception ex)
                        {
                            Log.Warning($"Ignorovaná chyba pri vypínaní motora {motor.Name} (Lis): {ex.Message}");
                        }
                    }
                }
            }
        }

        Log.Information("Shutdown dokončený.");
    }
}