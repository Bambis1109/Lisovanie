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
    public CJaws jaws { get; set; } = new CJaws();
    public Matrix matrix { get; set; }

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
            jaws.SetMotors(MotorJaws);
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

            case 230: return MainStep230(step);
            case 240: return MainStep240(step);
            case 250: return MainStep250(step);
            case 260: return MainStep260(step);

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
        matrix = new Matrix(-260, -120, 21, 19, 2, 2);
        matrix.RecalculDIA();

        return 10;
    } // Init

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
    } //Vypocet polohy ramena

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
    } //Mazanie chyb a nastavenie enable

    private int InitStep30(int step)
    {
        Message = "Homing Z a Jaws";
        jaws._motorJaws.Operation.HomingMode.SetHomingParameter(1000, 100, 20, 1500, 300, 0,
            EHomingMethod.HmCurrentThresholdNegativeSpeed);
        MotorZ.Operation.HomingMode.SetHomingParameter(10000, 1500, 500, 0, 300, 0,
            EHomingMethod.HmHomeSwitchPositiveSpeed);

        jaws._motorJaws.Operation.HomingMode.FindHome();
        MotorZ.Operation.HomingMode.FindHome();

        jaws._motorJaws.Operation.MotionInfo.WaitForHomingAttained(5000);
        MotorZ.Operation.MotionInfo.WaitForHomingAttained(5000);

        jaws._motorJaws.Operation.ProfilePositionMode.ActivateProfilePositionMode();
        MotorZ.Operation.ProfilePositionMode.ActivateProfilePositionMode();

        jaws._motorJaws.Operation.ProfilePositionMode.SetPositionProfile(2000, 10000, 10000);
        MotorZ.Operation.ProfilePositionMode.SetPositionProfile(4000, 15000, 15000);


        return 40;
    } //Homing Jaws a Z - nastavenie parametrov

    private int InitStep40(int step)
    {
        Message = "Homing delta ramien";

        MotorDown.Operation.HomingMode.SetHomingParameter(100, 20, 10, 0, 100, deltaRobot.ParametersDelta.EposLD,
            EHomingMethod.HmActualPosition);
        MotorUp.Operation.HomingMode.SetHomingParameter(100, 20, 10, 0, 100, deltaRobot.ParametersDelta.EposLH,
            EHomingMethod.HmActualPosition);

        MotorDown.Operation.HomingMode.FindHome();
        MotorUp.Operation.HomingMode.FindHome();

        MotorDown.Operation.MotionInfo.WaitForHomingAttained(1000);
        MotorUp.Operation.MotionInfo.WaitForHomingAttained(1000);

        uint velocity = 20;
        uint acceleration = 100;
        uint deceleration = 100;

        MotorDown.Operation.ProfilePositionMode.ActivateProfilePositionMode();
        MotorDown.Operation.ProfilePositionMode.SetPositionProfile(velocity, acceleration, deceleration);

        MotorUp.Operation.ProfilePositionMode.ActivateProfilePositionMode();
        MotorUp.Operation.ProfilePositionMode.SetPositionProfile(velocity, acceleration, deceleration);

        deltaRobot.MoveToPolar(0, 140);
        deltaRobot.WaitForTargetReached(3000);

        Log.Logger.ForContext("Name", Name).Debug($"Manipulator inizializovany.");

        return 99;
    } //Homing Delta ramien - nastavenie parametrov

    // ==========================================
    // METÓDY PRE MAIN PROGRAM (Prepojené na reálne motory)
    // ==========================================
    private int MainStep100(int step)
    {
        Message = "Main 100: Kontrola parkovania";
        StatusCycle = EnStatusCycle.Moving;

        if (RequestToEnd)
        {
            return 0;
        }

        return 110;
    }

    private int MainStep110(int step)
    {
        Message = "Start";
        return 120;
    } // start cyklu

    private int MainStep120(int step)
    {
        Message = "Vychodiskova poloha";
        deltaRobot.MoveToPolar(0, 160);
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(-9, true, true);
        jaws._motorJaws.Operation.ProfilePositionMode.ActivateProfilePositionMode();
        jaws._motorJaws.Operation.ProfilePositionMode.MoveToPositionGear(5, true, true);
        deltaRobot.WaitForTargetReached(5000);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);
        jaws._motorJaws.Operation.MotionInfo.WaitForTargetReached(5000);

        return 130;
    } // Vychodiskova poloha

    private int MainStep130(int step)
    {
        Message = "Cakanie na Lis";
        if (RequestToEnd)
        {
            return 0;
        }

        if (!resultUchopenie) Thread.Sleep(3000);
        return 140;
    } // Cakanie na lis

    private int MainStep140(int step)
    {
        Message = "Vysunutie k lisu";
        deltaRobot.MoveToPolar(0, 255);
        deltaRobot.WaitForTargetReached(5000);
        return 150;
    } //Vysunutie k lisu

    private int MainStep150(int step)
    {
        Message = "Dolu nad vyrobok";
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(-13, true, true);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);
        return 160;
    } // Dolu nad vyrobok

    bool resultUchopenie;

    private int MainStep160(int step)
    {
        Message = "Uchopenie";
        if (!jaws.SetPosCurrent("kv4", -6.7, -30, 1, 2000))
        {
            resultUchopenie = false;
            return 110;
        }

        resultUchopenie = true;
        return 170;
    } //Uchopenie OK->170  NOK->120

    private int MainStep170(int step)
    {
        Message = "Kontrola uchopenia";

        return 180;
    } //KOntrola uchopenia

    private int MainStep180(int step)
    {
        Message = "Zdvih";
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(-9, true, true);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);
        return 190;
    } //Zdvih

    private int MainStep190(int step)
    {
        Message = "Zasunutie";
        deltaRobot.MoveToPolar(0, 140);
        deltaRobot.WaitForTargetReached(5000);
       
        return 220;
    } //Zasunutie

    private int MainStep200(int step)
    {
        Message = "Otocenie na vykladanie";
        deltaRobot.MoveToPolar(-90, 140);
        deltaRobot.WaitForTargetReached(5000);
        return 210;
    } //Otocenie na vykladanie

    private int MainStep210(int step)
    {
        Message = "Vyska nad box";
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(-22, true, true);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);
        return 220;
    } // vyska nad box

    private int MainStep220(int step)
    {
        Message = "Vysunutie na vykladanie";
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(-22, true, true);
      
        deltaRobot.MoveToXY(matrix.Xactual, matrix.Yactual);
        //  deltaRobot.MoveToPolar(-90, 300);
        deltaRobot.WaitForTargetReached(5000);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);
      

        return 230;
    } //Vysunutie na vykladanie

    private int MainStep230(int step)
    {
        Message = "Vyska na vylozenie";
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(-37, true, true);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);
        return 240;
    } //Vyska na vylozenie

    private int MainStep240(int step)
    {
        Message = "Celuste vyloz";
        jaws._motorJaws.Operation.ProfilePositionMode.ActivateProfilePositionMode();
        jaws._motorJaws.Operation.ProfilePositionMode.MoveToPositionGear(-2, true, true);
        jaws._motorJaws.Operation.MotionInfo.WaitForTargetReached(5000);
        return 250;
    } //Celuste vyloz

    private int MainStep250(int step)
    {
        Message = "Vyska nad box";
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(-22, true, true);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);
        if (matrix.SetNextItemTestLast()) return 260; // Ak je pocet OK koniec
        return 100; 
    } //Vyska nad box test naplnenia OK->260 koniec, NOK opakuj ->100

    private int MainStep260(int step)
    {
        Message = "Parkuj";
       
        deltaRobot.MoveToPolar(0, 160);
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(-9, true, true);
        jaws._motorJaws.Operation.ProfilePositionMode.ActivateProfilePositionMode();
        jaws._motorJaws.Operation.ProfilePositionMode.MoveToPositionGear(5, true, true);
        deltaRobot.WaitForTargetReached(5000);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);
        jaws._motorJaws.Operation.MotionInfo.WaitForTargetReached(5000);
        return 0;
    } // Parkuj

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
                jaws._motorJaws.Operation.ProfilePositionMode.MoveToPositionGear(deltaRobot.StepSize, false, true);
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
                jaws._motorJaws.Operation.ProfilePositionMode.MoveToPositionGear(-deltaRobot.StepSize, false, true);
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
}