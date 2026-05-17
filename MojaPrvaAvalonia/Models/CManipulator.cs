using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EposCmd.Net;
using MojaPrvaAvalonia.ViewModels;
using Serilog;

namespace MojaPrvaAvalonia.Models;

public partial class CManipulator : CPlcEpos
{
    public CDeviceEpos4 MotorDown { get; set; }
    public CDeviceEpos4 MotorUp { get; set; }
    public CDeviceEpos4 MotorJaws { get; set; }
    public CDeviceEpos4 MotorZ { get; set; }
    public CoaxialDelta2D deltaRobot { get; set; } = new(115.0, 165.0, 262144.0, 56.0, 270.0, 82.0);

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

        if (Connection == EnStatusConnection.Connected)
        {
            deltaRobot.SetMotors(this.MotorDown, this.MotorUp);
            deltaRobot.StartMonitoring();
        }
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

            // Vysun
            case 110: return MainStep110(step);
            case 120: return MainStep120(step);
            case 130: return MainStep130(step);
            case 140: return MainStep140(step);
            case 150: return MainStep150(step);

            case 160: return MainStep160(step); // Z-axis dole
            case 170: return MainStep170(step); // Celuste otvor

            // Zasun
            case 180: return MainStep180(step);
            case 190: return MainStep190(step);
            case 200: return MainStep200(step);
            case 210: return MainStep210(step);
            case 220: return MainStep220(step);

            case 230: return MainStep230(step); // Z hore
            case 240: return MainStep240(step); // Celuste zatvor
            case 250: return MainStep250(step); // Vychodiskova poloha

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

        // DÔLEŽITÁ OPRAVA: Načítame aktívne offsety z uložených parametrov do kinematiky (ako vo funkčnej verzii)
        deltaRobot.LoadOffsets(deltaRobot.ParametersDelta.OffsetSystem, deltaRobot.ParametersDelta.OffsetArm);

        double eposPositionLH;
        double eposPositionLD;

        deltaRobot.CalculateColdStartPositions(MotorUp.Data.PositionActualSensor2,
            MotorDown.Data.PositionActualSensor2, out eposPositionLH,
            out eposPositionLD);

        deltaRobot.ParametersDelta.EposLH = (int)eposPositionLH;
        deltaRobot.ParametersDelta.EposLD = (int)eposPositionLD;

        return 20;
    }

    private int InitStep20(int step)
    {
        Message = "Mazanie chyb a nastav enable";
        ClearAllFaults();
        EnableAllMotors();

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

        MotorJaws.Operation.ProfilePositionMode.SetPositionProfile(2000, 10000, 10000);
        MotorZ.Operation.ProfilePositionMode.SetPositionProfile(2000, 5000, 5000);


        return 40;
    }

    private int InitStep40(int step)
    {
        Message = "Inicializacia ramien";

        MotorDown.Operation.HomingMode.SetHomingParameter(100, 20, 10, 0, 100, deltaRobot.ParametersDelta.EposLD,
            EHomingMethod.HmActualPosition);
        MotorUp.Operation.HomingMode.SetHomingParameter(100, 20, 10, 0, 100, deltaRobot.ParametersDelta.EposLH,
            EHomingMethod.HmActualPosition);

        MotorDown.Operation.HomingMode.FindHome();
        MotorUp.Operation.HomingMode.FindHome();

        MotorDown.Operation.MotionInfo.WaitForHomingAttained(1000);
        MotorUp.Operation.MotionInfo.WaitForHomingAttained(1000);

        uint velocity = 20;
        uint acceleration = 200;
        uint deceleration = 200;

        MotorDown.Operation.ProfilePositionMode.ActivateProfilePositionMode();
        MotorDown.Operation.ProfilePositionMode.SetPositionProfile(velocity, acceleration, deceleration);

        MotorUp.Operation.ProfilePositionMode.ActivateProfilePositionMode();
        MotorUp.Operation.ProfilePositionMode.SetPositionProfile(velocity, acceleration, deceleration);

        deltaRobot.MoveToXY(0, 140);
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
        Message = "Vysun 1: Centruj";
        deltaRobot.MoveToXY(0, 140);
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(x1, true, true);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(x2, true, true);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);
        deltaRobot.WaitForTargetReached(5000);
        return 120;
    }

    private int MainStep120(int step)
    {
        Message = "Vysun 2: Vpred";
        deltaRobot.MoveToXY(0, 180);
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(x1, true, true);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(x2, true, true);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);
        deltaRobot.WaitForTargetReached(5000);
        return 130;
    }

    private int MainStep130(int step)
    {
        Message = "Vysun 3: Vlavo";
        deltaRobot.MoveToXY(-200, 180);
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(x1, true, true);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);

        return 140;
    }

    private int MainStep140(int step)
    {
        Message = "Vysun 4: Spat";
        deltaRobot.MoveToXY(-200, 140);

        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(x2, true, true);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);
        deltaRobot.WaitForTargetReached(5000);
        return 150;
    }

    private int MainStep150(int step)
    {
        Message = "Vysun 5: Finalizuj";
        deltaRobot.MoveToXY(0, 140);
        deltaRobot.WaitForTargetReached(5000);
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(x1, true, true);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);


        return 160;
    }

    private double x1 = -1;
    private double x2 = -30;

    private int MainStep160(int step)
    {
        Message = "Z-axis dole";


        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(x2, true, true);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);
        return 170;
    }

    private int MainStep170(int step)
    {
        Message = "Celuste otvor";
        MotorJaws.Operation.ProfilePositionMode.MoveToPositionGear(0, true, true);
        MotorJaws.Operation.MotionInfo.WaitForTargetReached(5000);
        return 180;
    }

    private int MainStep180(int step)
    {
        Message = "Zasun 1: Centruj";
        deltaRobot.MoveToXY(0, 140);
        deltaRobot.WaitForTargetReached(5000);
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(x1, true, true);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);


        return 190;
    }

    private int MainStep190(int step)
    {
        Message = "Zasun 2: Vpred";
        deltaRobot.MoveToXY(0, 180);
        deltaRobot.WaitForTargetReached(5000);
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(x2, true, true);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);

        return 200;
    }

    private int MainStep200(int step)
    {
        Message = "Zasun 3: Vpravo";
        deltaRobot.MoveToXY(200, 180);
        deltaRobot.WaitForTargetReached(5000);
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(x1, true, true);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);


        return 210;
    }

    private int MainStep210(int step)
    {
        Message = "Zasun 4: Spat";
        deltaRobot.MoveToXY(200, 140);
        deltaRobot.WaitForTargetReached(5000);
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(x2, true, true);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);

        return 220;
    }

    private int MainStep220(int step)
    {
        Message = "Zasun 5: Finalizuj";

        return 230;
    }

    private int MainStep230(int step)
    {
        Message = "Z hore";

        return 240;
    }

    private int MainStep240(int step)
    {
        Message = "Celuste zatvor";
        MotorJaws.Operation.ProfilePositionMode.MoveToPositionGear(15, true, true);
        MotorJaws.Operation.MotionInfo.WaitForTargetReached(5000);
        return 250;
    }

    private int MainStep250(int step)
    {
        Message = "Vychodiskova poloha";

        return 100;
    }

    // Dodatočné metódy z CDelta
    [RelayCommand]
    public async Task KalibrujAsync()
    {
        try
        {
            await deltaRobot.KalibrujAsync();
        }
        catch (Exception ex)
        {
            Log.Error($"Manipulator Kalibruj Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task OrientujAsync()
    {
        try
        {
            await deltaRobot.OrientujAsync();
        }
        catch (Exception ex)
        {
            Log.Error($"Manipulator Orientuj Error: {ex.Message}");
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

    [RelayCommand]
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

    [RelayCommand]
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

    [RelayCommand]
    public void SaveParameters()
    {
        SaveParametersToFile("ParametersDelta.json", deltaRobot.ParametersDelta);
    }

    [RelayCommand]
    public void LoadParameters()
    {
        LoadParametersFromFile("ParametersDelta.json", deltaRobot.ParametersDelta);

        // NABINDOVANIE NA AI (Aktualizácia kinematiky)
        deltaRobot.LoadOffsets(deltaRobot.ParametersDelta.OffsetSystem, deltaRobot.ParametersDelta.OffsetArm);
    }

    // Movement methods
    [RelayCommand]
    public async Task MoveRightAsync()
    {
        try
        {
            await Task.Run(() => { deltaRobot.MoveRight(); });
        }
        catch (Exception ea)
        {
            Log.Error($"MoveRight Error: {ea.Message}");
        }
    }

    [RelayCommand]
    public async Task MoveLeftAsync()
    {
        try
        {
            await Task.Run(() => { deltaRobot.MoveLeft(); });
        }
        catch (Exception ea)
        {
            Log.Error($"MoveLeft Error: {ea.Message}");
        }
    }

    [RelayCommand]
    public async Task MoveUpAsync()
    {
        try
        {
            await Task.Run(() => { deltaRobot.MoveUp(); });
        }
        catch (Exception ea)
        {
            Log.Error($"MoveUp Error: {ea.Message}");
        }
    }

    [RelayCommand]
    public async Task MoveDownAsync()
    {
        try
        {
            await Task.Run(() => { deltaRobot.MoveDown(); });
        }
        catch (Exception ea)
        {
            Log.Error($"MoveDown Error: {ea.Message}");
        }
    }

    [RelayCommand]
    public async Task JawsOpenAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                MotorJaws.Operation.ProfilePositionMode.MoveToPositionGear(deltaRobot.StepSize, false, true);
            });
        }
        catch (Exception ea)
        {
            Log.Error($"JawsOpen Error: {ea.Message}");
        }
    }

    [RelayCommand]
    public async Task JawsCloseAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                MotorJaws.Operation.ProfilePositionMode.MoveToPositionGear(-deltaRobot.StepSize, false, true);
            });
        }
        catch (Exception ea)
        {
            Log.Error($"JawsClose Error: {ea.Message}");
        }
    }

    [RelayCommand]
    public async Task MoveZUpAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(deltaRobot.StepSize, false, true);
            });
        }
        catch (Exception ea)
        {
            Log.Error($"MoveZUp Error: {ea.Message}");
        }
    }

    [RelayCommand]
    public async Task MoveZDownAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(-deltaRobot.StepSize, false, true);
            });
        }
        catch (Exception ea)
        {
            Log.Error($"MoveZDown Error: {ea.Message}");
        }
    }

  

   

    public bool SetPosCurrent(string measure, double midlevalue, double percentageForce, double range, int timeout)
    {
        // 1. Výpočet pre-grip pozície (na okraji tolerančného pásma podľa smeru)
        double preposition = (percentageForce > 0) ? midlevalue - range : midlevalue + range;

        // 2. Fáza rýchleho priblíženia (PPM)
        MotorJaws.Operation.ProfilePositionMode.ActivateProfilePositionMode();
        MotorJaws.Operation.ProfilePositionMode.MoveToPositionGear(preposition, true, true);
        MotorJaws.Operation.MotionInfo.WaitForTargetReached(5000);

        // 3. Fáza aplikácie sily (CST)
        MotorJaws.Operation.CurrentMode.ActivateCurrentMode();
        // MotorJaws.Operation.StateMachine.SetEnableState(); // Odstránené - redundantné

        // Čaká na dosiahnutie sily a mechanické ustálenie (obsahuje vlastný stabilizačný counter)
        MotorJaws.Operation.CurrentMode.WaitToTorqueStopMovePercentage(timeout, percentageForce);

        // Thread.Sleep(10); // Odstránené - ušetrený čas cyklu

        // 4. Načítanie skutočnej pozície
        var actual = MotorJaws.Data.PositionActualGear;

        // 5. Vyhodnotenie tolerancie (Čitateľnejší zápis pôvodnej matematickej logiky)
        // Kontroluje, či je absolútna odchýlka od stredu menšia alebo rovná tolerancii (range)
        bool isOk = Math.Abs(actual - midlevalue) <= range;

        // 6. Logovanie do DB (Formát zachovaný presne podľa zadania)
        if (isOk)
        {
            Log.Logger.ForContext("Name", MotorJaws.Name)
                .ForContext("Measure", measure)
                .Verbose(
                    $"percentage:[{percentageForce}], midle: [{midlevalue:0.00}], range: [{range:0.00}], actual: [{actual:0.00}], result: [true]");
            return true;
        }
        else
        {
            Log.Logger.ForContext("Name", MotorJaws.Name)
                .ForContext("Measure", measure)
                .Error(
                    $"percentage:[{percentageForce}], midle: [{midlevalue:0.00}], range: [{range:0.00}], actual: [{actual:0.00}], result: [false]");
            return false;
        }
    }
}