using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using EposCmd.Net;
using IXXAT;
using Serilog;
using Avalonia.Threading;

namespace MojaPrvaAvalonia.Models;

public class CMainProgram
{
    public ObservableCollection<CPlc> ZoznamPlc { get; } = new ObservableCollection<CPlc>();
    public CDeviceManagerCO DeviceManagerCO { get; set; }
    public string ConnectionInfo { get; set; }

    public CMainProgram()
    {
        ZoznamPlc.Add(new CManipulator("Manipulator"));
        ZoznamPlc.Add(new CLis("Lis"));
    }

    public async Task<bool> Connect()
    {
        Log.Information("MainProgram: Connecting to CAN...");
        try
        {
            if (!CreateCanConector(0, 0)) return false;

            CManipulator? manipulator = ZoznamPlc[0] as CManipulator;
            CLis? lis = ZoznamPlc[1] as CLis;

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
        

            foreach (var vm in manipulator.MotorViewModels)
            {
                vm.StartRefresh();
            }
            
            foreach (var vm in lis.MotorViewModels)
            {
                vm.StartRefresh();
            }
           DeviceManagerCO.Sync(ESync.NcsEnable);

            return true;
        }
        catch (Exception ex)
        {
            Log.Fatal($"MainProgram Connect Error: {ex.Message}");
            return false;
        }
    }

    private bool CreateCanConector(int canline, int boardline)
    {
        try
        {
            if (DeviceManagerCO != null)
            {
                Log.Warning("CanOpen Master is already initialized!");
                return true;
            }

            Guid boardId = CANopenMasterAPI6.COP_1stBOARD;
            DeviceManagerCO = new CDeviceManagerCO
            {
                Name = $"DM:Main",
                BaudIndex = CANopenMasterAPI6.COP_k_1000_KB,
                BoardType = XatBoards.XAT_USB2CANV2_VCI3,
                BoardId = boardId,
                CanLine = canline,
                BoardLine = boardline,
                Timeout = 500
            };

            DeviceManagerCO.Init();
            ConnectionInfo = DeviceManagerCO.GetConnectionInfo;
            Log.Information($"Connect: {ConnectionInfo}");
            return true;
        }
        catch (Exception ex)
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
        Log.Information($"Created device: {name} (ID: {nodeId})");
        return motor;
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
                Dispatcher.UIThread.Post(() =>
                {
                    Shutdown(); 
                });
            }
        }
    }
    public void Shutdown()
    {
        Log.Information("Vykonávam núdzový Shutdown systému...");

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
                if (plc is CManipulator m)
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
                            Log.Warning($"Ignorovaná chyba pri vypínaní motora {motor.Name} (Manipulator): {ex.Message}");
                        }
                    }
                }
                else if (plc is CLis l)
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