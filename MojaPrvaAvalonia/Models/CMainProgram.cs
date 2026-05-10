using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using EposCmd.Net;
using IXXAT;
using Serilog;

namespace MojaPrvaAvalonia.Models;

public class CMainProgram
{
    public ObservableCollection<CPlc> ZoznamPlc { get; } = new ObservableCollection<CPlc>();
    public CDeviceManagerCO DeviceManagerCO { get; set; }
    public string ConnectionInfo { get; set; }

    public CMainProgram()
    {
        ZoznamPlc.Add(new CManipulator("Linka 1"));
        ZoznamPlc.Add(new CLis("Linka 2"));
    }

    public async Task<bool> Connect()
    {
        Log.Information("MainProgram: Connecting to CAN...");
        try
        {
            if (!CreateCanConector(0, 0)) return false;

            // Nájdeme náš manipulátor
            CManipulator manipulator = null;
            foreach (var plc in ZoznamPlc)
            {
                if (plc is CManipulator m)
                {
                    manipulator = m;
                    break;
                }
            }

            if (manipulator != null)
            {
                manipulator.MotorUp = CreateDevices(DeviceManagerCO, 11, "Up", 729.4278, 262144.0);
                manipulator.MotorDown = CreateDevices(DeviceManagerCO, 12, "Down", 729.4278, 262144.0);
                manipulator.MotorJaws = CreateDevices(DeviceManagerCO, 13, "Jaws", 165, 500);
                manipulator.MotorZ = CreateDevices(DeviceManagerCO, 14, "Z", 1755, 500);

                manipulator.Motors.Clear();
                manipulator.Motors.Add(manipulator.MotorUp);
                manipulator.Motors.Add(manipulator.MotorDown);
                manipulator.Motors.Add(manipulator.MotorJaws);
                manipulator.Motors.Add(manipulator.MotorZ);

                if (manipulator.MotorViewModels.Count == 4)
                {
                    manipulator.MotorViewModels[0].AssignDevice(manipulator.MotorUp);
                    manipulator.MotorViewModels[1].AssignDevice(manipulator.MotorDown);
                    manipulator.MotorViewModels[2].AssignDevice(manipulator.MotorJaws);
                    manipulator.MotorViewModels[3].AssignDevice(manipulator.MotorZ);
                    
                    foreach (var vm in manipulator.MotorViewModels)
                    {
                        vm.StartRefresh();
                    }
                }
                else
                {
                     Log.Error("MainProgram: Expected 4 MotorViewModels but found different count.");
                }

            
                
                Log.Information("MainProgram: CManipulator motors initialized.");
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Fatal($"MainProgram Connect Error: {ex.Message}");
            return false;
        }
    }

    public void SaveParameters(CManipulator manipulator, string fileName)
    {
        try
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.Combine(directory, $"{fileName}.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(manipulator.Parameters, options);
            File.WriteAllText(path, json);
            Log.Information($"Parameters saved to: {path}");
        }
        catch (Exception ex)
        {
            Log.Error($"Error saving parameters: {ex.Message}");
        }
    }

    public void LoadParameters(CManipulator manipulator, string fileName)
    {
        try
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.Combine(directory, $"{fileName}.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<CParameters>(json);
                if (loaded != null)
                {
                    manipulator.Parameters.RawLH = loaded.RawLH;
                    manipulator.Parameters.RawLD = loaded.RawLD;
                    manipulator.Parameters.EposLH = loaded.EposLH;
                    manipulator.Parameters.EposLD = loaded.EposLD;
                    manipulator.Parameters.OffsetArm = loaded.OffsetArm;
                    manipulator.Parameters.OffsetSystem = loaded.OffsetSystem;
                    Log.Information($"Parameters loaded from: {path}");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error loading parameters: {ex.Message}");
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

    private CDeviceEpos4 CreateDevices(CDeviceManagerCO deviceManagerCO, byte nodeId, string name, double gear, double pulse)
    {
        var motor = new CDeviceEpos4(deviceManagerCO._keyHandle, nodeId, name, gear, pulse);
        DeviceManagerCO.AddDevice(motor);
        Log.Information($"Created device: {name} (ID: {nodeId})");
        return motor;
    }

    public void Shutdown()
    {
        foreach (var plc in ZoznamPlc)
        {
            plc.StopProgramImmediately();
        }

        if (DeviceManagerCO != null)
        {
            foreach (var plc in ZoznamPlc)
            {
                if (plc is CManipulator m)
                {
                    foreach (var motor in m.Motors)
                    {
                        motor.Operation?.StateMachine?.SetDisableState();
                    }
                }
            }
        }
    }
}