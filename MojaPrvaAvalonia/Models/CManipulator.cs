using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using EposCmd.Net;
using MojaPrvaAvalonia.ViewModels;
using Serilog;

namespace MojaPrvaAvalonia.Models;

public partial class CManipulator : CPlc
{
    public CDeviceEpos4 MotorDown { get; set; }
    public CDeviceEpos4 MotorUp { get; set; }
    public CDeviceEpos4 MotorJaws { get; set; }
    public CDeviceEpos4 MotorZ { get; set; }
    public CParameters Parameters { get; set; } = new();
    public CoaxialDelta2D deltaRobot { get; set; } = new(115.0, 165.0, 262144.0);
    public List<CDeviceEpos4> Motors { get; } = new();

    public ObservableCollection<UcMotorViewModel> MotorViewModels { get; } =
        new ObservableCollection<UcMotorViewModel>();

    public CManipulator(string name) : base(name)
    {
        LoadParameters();
        MotorViewModels.Add(new UcMotorViewModel(null, "Up"));
        MotorViewModels.Add(new UcMotorViewModel(null, "Down"));
        MotorViewModels.Add(new UcMotorViewModel(null, "Jaws"));
        MotorViewModels.Add(new UcMotorViewModel(null, "Z"));
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

        deltaRobot.SetMotors(this.MotorDown, this.MotorUp);
        deltaRobot.StartMonitoring();

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
            case 160: return MainStep160(step);
            case 170: return MainStep170(step);

            default: return base.RunStep(step);
        }
    }

    // ==========================================
    // METÓDY PRE INIT
    // ==========================================
    private int InitStep1(int step)
    {
        Message = "Init 1: Štart inicializácie";
        StatusCycle = EnStatusCycle.Moving;
        return 10;
    }

    private int InitStep10(int step)
    {
        Message = "Vypocet polohy ramena";
        deltaRobot.CalculateAndSetCalibrationOffsets(Parameters.RawLH, Parameters.RawLD);
        Parameters.OffsetArm = (int)deltaRobot.OffsetArm;
        Parameters.OffsetSystem = (int)deltaRobot.OffsetSystem;
        double eposPositionLH;
        double eposPositionLD;
        deltaRobot.CalculateColdStartPositions(MotorUp.Data.PositionActualSensor2,
            MotorDown.Data.PositionActualSensor2, out eposPositionLH,
            out eposPositionLD);
        Parameters.EposLH = (int)eposPositionLH;
        Parameters.EposLD = (int)eposPositionLD;
        return 20;
    }

    private int InitStep20(int step)
    {
        Message = "Mazanie chyb a nastav enable";
        foreach (var motor in Motors)
        {
            if (motor.Operation?.StateMachine == null) continue;
            motor.Operation.StateMachine.ClearFault();
        }

        foreach (var motor in Motors)
        {
            if (motor.Operation?.StateMachine == null) continue;
            motor.Operation.StateMachine.SetEnableState();
        }

        foreach (var motor in Motors)
        {
            if (motor.Operation?.StateMachine == null) continue;
            motor.Operation.HomingMode?.ActivateHomingMode();
        }

        return 30;
    }

    private int InitStep30(int step)
    {
        Message = "Homing Z a Jaws";
        MotorJaws.Operation.HomingMode.SetHomingParameter(1000, 100, 20, 1500, 300, 0,
            EHomingMethod.HmCurrentThresholdNegativeSpeed);
        MotorZ.Operation.HomingMode.SetHomingParameter(10000, 1500, 500, 0, 300, 0,
            EHomingMethod.HmHomeSwitchPositiveSpeed);

        MotorJaws.Operation.HomingMode.FindHome();
        MotorZ.Operation.HomingMode.FindHome();

        MotorJaws.Operation.MotionInfo.WaitForHomingAttained(5000);
        MotorZ.Operation.MotionInfo.WaitForHomingAttained(5000);

        MotorJaws.Operation.ProfilePositionMode.ActivateProfilePositionMode();
        MotorZ.Operation.ProfilePositionMode.ActivateProfilePositionMode();

        MotorJaws.Operation.ProfilePositionMode.SetPositionProfile(4000, 20000, 20000);
        MotorZ.Operation.ProfilePositionMode.SetPositionProfile(6000, 60000, 60000);


        return 40;
    }

    private int InitStep40(int step)
    {
        Message = "Inicializacia ramien";

        MotorDown.Operation.HomingMode.SetHomingParameter(100, 20, 10, 0, 100, Parameters.EposLD,
            EHomingMethod.HmActualPosition);
        MotorUp.Operation.HomingMode.SetHomingParameter(100, 20, 10, 0, 100, Parameters.EposLH,
            EHomingMethod.HmActualPosition);

        MotorDown.Operation.HomingMode.FindHome();
        MotorUp.Operation.HomingMode.FindHome();

        MotorDown.Operation.MotionInfo.WaitForHomingAttained(1000);
        MotorUp.Operation.MotionInfo.WaitForHomingAttained(1000);

        MotorDown.Operation.ProfilePositionMode.ActivateProfilePositionMode();
        MotorDown.Operation.ProfilePositionMode.SetPositionProfile(30, 100, 100);

        MotorUp.Operation.ProfilePositionMode.ActivateProfilePositionMode();
        MotorUp.Operation.ProfilePositionMode.SetPositionProfile(30, 100, 100);

       deltaRobot.MoveToPolar(65,-90);
       deltaRobot.WaitForTargetReached(3000);

        Log.Logger.ForContext("Name", Name).Debug($"Manipulator inizializovany.");

        return 99;
    }

    // ==========================================
    // METÓDY PRE MAIN PROGRAM (Prepojené na reálne motory)
    // ==========================================
    private int MainStep100(int step)
    {
        Message = "Main 100: Kontrola parkovania";
        StatusCycle = EnStatusCycle.Moving;

        if (RequestToEnd)
        {
            Log.Logger.ForContext("Name", Name).Information("Zachytená požiadavka na parkovanie, ukončujem program.");
            return 0;
        }

        return 110;
    }

    private int MainStep110(int step)
    {
        Message = "Vysun";
        deltaRobot.MoveToPolar(100,10);
        deltaRobot.WaitForTargetReached(5000);
        return 120;
    }

    private int MainStep120(int step)
    {
        Message = "Z-axis dole";
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(-30, true, true);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);
        return 130;
    }

    private int MainStep130(int step)
    {
        Message = "Celuste otvor";
        MotorJaws.Operation.ProfilePositionMode.MoveToPositionGear(20, true, true);
        MotorJaws.Operation.MotionInfo.WaitForTargetReached(5000);
        return 140;
    }

    private int MainStep140(int step)
    {
        Message = "Zasun";
        deltaRobot.MoveToPolar(58,30);
        deltaRobot.WaitForTargetReached(5000);
        deltaRobot.MoveToPolar(200,-30);
        deltaRobot.WaitForTargetReached(5000);
        return 150;
    }

    private int MainStep150(int step)
    {
        Message = "Z hore";
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(0, true, true);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);
        return 160;
    }

    private int MainStep160(int step)
    {
        Message = "Celuste zatvor";
        MotorJaws.Operation.ProfilePositionMode.MoveToPositionGear(0, true, true);
        MotorJaws.Operation.MotionInfo.WaitForTargetReached(5000);
        return 170;
    }

    private int MainStep170(int step)
    {
        Message = "Vychodiskova poloha";
        deltaRobot.MoveToPolar(60,-90);
        deltaRobot.WaitForTargetReached(5000);
        return 100;
    }

    // Dodatočné metódy z CDelta
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    public async Task Kalibruj()
    {
        try
        {
            await Task.Run(() =>
            {
                if (MotorUp != null && MotorUp.Operation != null)
                {
                    Parameters.RawLH = MotorUp.Operation.HomingMode.GetSSiEncoderActualPositionA();
                }

                if (MotorDown != null && MotorDown.Operation != null)
                {
                    Parameters.RawLD = MotorDown.Operation.HomingMode.GetSSiEncoderActualPositionA();
                }

                Log.Information(
                    $"Manipulator: Kalibruj dokončené. RawLH: {Parameters.RawLH}, RawLD: {Parameters.RawLD}");
            });
        }
        catch (Exception ex)
        {
            Log.Error($"Manipulator Kalibruj Error: {ex.Message}");
        }
    }

    public void ShowLog()
    {
        if (MotorDown == null || MotorUp == null) return;

        var actualSSIDown = MotorDown.Data.PositionActualSensor2;
        var actualDown = MotorDown.Data.PositionActual;
        var actualDownGear = MotorDown.Data.PositionActualGear;
        var actualSSIUp = MotorUp.Data.PositionActualSensor2;
        var actualUp = MotorUp.Data.PositionActual;
        var actualUpGear = MotorUp.Data.PositionActualGear;
        Log.Logger.ForContext("Name", Name)
            .Debug(
                $"SSI  In:({actualSSIUp})({actualUp})({actualUpGear:F4}) Out:({actualSSIDown})({actualDown})({actualDownGear:F4}) )");
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    public async Task DisableMotorsAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                Log.Information("Manipulator: DisableMotors command...");
                foreach (var motor in Motors)
                {
                    motor.Operation?.StateMachine?.SetDisableState();
                }

                ShowLog();
            });
        }
        catch (Exception ex)
        {
            Log.Error($"DisableMotors Error: {ex.Message}");
        }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    public async Task EnableMotorsAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                Log.Information("Manipulator: EnableMotors command...");
                foreach (var motor in Motors)
                {
                    motor.Operation?.StateMachine?.SetEnableState();
                }

                ShowLog();
            });
        }
        catch (Exception ex)
        {
            Log.Error($"EnableMotors Error: {ex.Message}");
        }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    public async Task SetupInitAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                Log.Information("Manipulator: SetupInit initializing devices...");

                if (Motors == null || Motors.Count < 4 ||
                    MotorDown?.Operation == null || MotorUp?.Operation == null ||
                    MotorJaws?.Operation == null || MotorZ?.Operation == null)
                {
                    Log.Warning("Manipulator: Cannot init, motors are not fully connected or initialized.");
                    return;
                }

                deltaRobot.CalculateAndSetCalibrationOffsets(Parameters.RawLH, Parameters.RawLD);
                Parameters.OffsetArm = (int)deltaRobot.OffsetArm;
                Parameters.OffsetSystem = (int)deltaRobot.OffsetSystem;
                double eposPositionLH;
                double eposPositionLD;

                deltaRobot.CalculateColdStartPositions(MotorUp.Data.PositionActualSensor2,
                    MotorDown.Data.PositionActualSensor2, out eposPositionLH,
                    out eposPositionLD);

                Parameters.EposLH = (int)eposPositionLH;
                Parameters.EposLD = (int)eposPositionLD;

                foreach (var motor in Motors)
                {
                    if (motor.Operation?.StateMachine == null) continue;

                    motor.Operation.StateMachine.ClearFault();
                    motor.Operation.StateMachine.SetEnableState();
                    motor.Operation.HomingMode?.ActivateHomingMode();
                }

                MotorJaws.Operation.HomingMode.SetHomingParameter(1000, 100, 10, 1500, 300, 0,
                    EHomingMethod.HmCurrentThresholdNegativeSpeed);
                MotorZ.Operation.HomingMode.SetHomingParameter(10000, 1500, 100, 0, 300, 0,
                    EHomingMethod.HmHomeSwitchPositiveSpeed);

                MotorJaws.Operation.HomingMode.FindHome();
                MotorZ.Operation.HomingMode.FindHome();

                MotorJaws.Operation.MotionInfo.WaitForHomingAttained(5000);
                MotorZ.Operation.MotionInfo.WaitForHomingAttained(5000);

                MotorJaws.Operation.ProfilePositionMode.ActivateProfilePositionMode();
                MotorZ.Operation.ProfilePositionMode.ActivateProfilePositionMode();

                MotorJaws.Operation.ProfilePositionMode.SetPositionProfile(4000, 20000, 20000);
                MotorZ.Operation.ProfilePositionMode.SetPositionProfile(6000, 60000, 60000);

                MotorDown.Operation.HomingMode.SetHomingParameter(100, 20, 10, 0, 100, Parameters.EposLD,
                    EHomingMethod.HmActualPosition);
                MotorUp.Operation.HomingMode.SetHomingParameter(100, 20, 10, 0, 100, Parameters.EposLH,
                    EHomingMethod.HmActualPosition);

                MotorDown.Operation.HomingMode.FindHome();
                MotorUp.Operation.HomingMode.FindHome();

                MotorDown.Operation.MotionInfo.WaitForHomingAttained(1000);
                MotorUp.Operation.MotionInfo.WaitForHomingAttained(1000);

                MotorDown.Operation.ProfilePositionMode.ActivateProfilePositionMode();
                MotorDown.Operation.ProfilePositionMode.SetPositionProfile(40, 200, 200);

                MotorUp.Operation.ProfilePositionMode.ActivateProfilePositionMode();
                MotorUp.Operation.ProfilePositionMode.SetPositionProfile(40, 200, 200);

                MotorDown.Operation.ProfilePositionMode.MoveToPositionGear(135, true, true);
                MotorUp.Operation.ProfilePositionMode.MoveToPositionGear(-135, true, true);

                MotorDown.Operation.MotionInfo.WaitForTargetReached(5000);
                MotorUp.Operation.MotionInfo.WaitForTargetReached(5000);

                Log.Information("Manipulator: SetupInit finished successfully.");
            });
        }
        catch (Exception ex)
        {
            Log.Error($"SetupInit Error: {ex.Message}");
        }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    public void SaveParameters()
    {
        try
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory;
            var path = System.IO.Path.Combine(directory, "Parameters.json");
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            var json = System.Text.Json.JsonSerializer.Serialize(Parameters, options);
            System.IO.File.WriteAllText(path, json);
            Log.Information($"Manipulator: Parameters saved to: {path}");
        }
        catch (Exception ex)
        {
            Log.Error($"Manipulator: Error saving parameters: {ex.Message}");
        }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    public void LoadParameters()
    {
        try
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory;
            var path = System.IO.Path.Combine(directory, "Parameters.json");
            if (System.IO.File.Exists(path))
            {
                var json = System.IO.File.ReadAllText(path);
                var loaded = System.Text.Json.JsonSerializer.Deserialize<CParameters>(json);
                if (loaded != null)
                {
                    Parameters.RawLH = loaded.RawLH;
                    Parameters.RawLD = loaded.RawLD;
                    Parameters.EposLH = loaded.EposLH;
                    Parameters.EposLD = loaded.EposLD;
                    Parameters.OffsetArm = loaded.OffsetArm;
                    Parameters.OffsetSystem = loaded.OffsetSystem;
                    Log.Information($"Manipulator: Parameters loaded from: {path}");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Manipulator: Error loading parameters: {ex.Message}");
        }
    }

    // Movement methods
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    public async Task MoveRightAsync()
    {
        try
        {
            await Task.Run(() => { deltaRobot.MoveRight(10); });
        }
        catch (Exception ea)
        {
            Log.Error($"MoveRight Error: {ea.Message}");
        }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    public async Task MoveLeftAsync()
    {
        try
        {
            await Task.Run(() => { deltaRobot.MoveLeft(10); });
        }
        catch (Exception ea)
        {
            Log.Error($"MoveLeft Error: {ea.Message}");
        }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    public async Task MoveUpAsync()
    {
        try
        {
            await Task.Run(() => { deltaRobot.MoveUp(10); });
        }
        catch (Exception ea)
        {
            Log.Error($"MoveUp Error: {ea.Message}");
        }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    public async Task MoveDownAsync()
    {
        try
        {
            await Task.Run(() => { deltaRobot.MoveDown(10); });
        }
        catch (Exception ea)
        {
            Log.Error($"MoveDown Error: {ea.Message}");
        }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    public async Task JawsOpenAsync()
    {
        try
        {
            await Task.Run(() => { MotorJaws.Operation.ProfilePositionMode.MoveToPositionGear(2, false, true); });
        }
        catch (Exception ea)
        {
            Log.Error($"JawsOpen Error: {ea.Message}");
        }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    public async Task JawsCloseAsync()
    {
        try
        {
            await Task.Run(() => { MotorJaws.Operation.ProfilePositionMode.MoveToPositionGear(-2, false, true); });
        }
        catch (Exception ea)
        {
            Log.Error($"JawsClose Error: {ea.Message}");
        }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    public async Task MoveZUpAsync()
    {
        try
        {
            await Task.Run(() => { MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(10, false, true); });
        }
        catch (Exception ea)
        {
            Log.Error($"MoveZUp Error: {ea.Message}");
        }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    public async Task MoveZDownAsync()
    {
        try
        {
            await Task.Run(() => { MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(-10, false, true); });
        }
        catch (Exception ea)
        {
            Log.Error($"MoveZDown Error: {ea.Message}");
        }
    }
}