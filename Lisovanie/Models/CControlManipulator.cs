using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EposCmd.Net;
using Lisovanie.Net;
using Lisovanie.ViewModels;
using Serilog;

namespace Lisovanie.Models;

public partial class CControlManipulator : CPlcEpos
{
    public CDeviceEpos4 MotorDown { get; set; }
    public CDeviceEpos4 MotorUp { get; set; }
    public CDeviceEpos4 MotorJaws { get; set; }
    public CDeviceEpos4 MotorZ { get; set; }
    public CoaxialDelta2D deltaRobot { get; set; } = new(115.0, 165.0, 262144.0, 56.0, 270.0, 82.0);
    public CJaws jaws { get; set; } = new CJaws();
    [ObservableProperty] private Matrix _matrixOK;
    [ObservableProperty] private Matrix _matrixNOK;
    public CProduktLis ProduktLisActual { get; set; } = new();
    public bool ResultUchopenie { get; private set; }

    public CControlManipulator(string name) : base(name)
    {
        LoadParameters();
        MotorViewModels.Add(new UcDeviceEpos4ViewModel(null, "Up"));
        MotorViewModels.Add(new UcDeviceEpos4ViewModel(null, "Down"));
        MotorViewModels.Add(new UcDeviceEpos4ViewModel(null, "Jaws"));
        MotorViewModels.Add(new UcDeviceEpos4ViewModel(null, "Z"));
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
            case 110: return MainStep110(step);
            case 120: return MainStep120(step);
            case 130: return MainStep130(step);
            case 140: return MainStep140(step);
            case 150: return MainStep150(step);
            case 160: return MainStep160(step);
            case 170: return MainStep170(step);
            case 180: return MainStep180(step);
            case 190: return MainStep190(step);
            case 200: return MainStep200(step);
            case 210: return MainStep210(step);
            case 220: return MainStep220(step);
            case 230: return MainStep230(step);
            case 240: return MainStep240(step);
            case 250: return MainStep250(step);
            case 260: return MainStep260(step);
            case 270: return MainStep270(step);
            case 280: return MainStep280(step);
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
        MatrixOK = new Matrix(-306, -68, 21, 19, 6, 7);
        MatrixOK.RecalculDIA();
        MatrixNOK = new Matrix(165, -68, 21, 19, 3, 2);
        MatrixNOK.RecalculDIA();

        return 10;
    } // Mastavenie vykladacich matric

    private int InitStep10(int step)
    {
        Message = "Vypocet polohy ramena";

        // DÔLEŽITÁ OPRAVA: Načítame aktívne offsety z uložených parametrov do kinematiky (ako vo funkčnej verzii)
        deltaRobot.LoadOffsets(deltaRobot.ParametersDelta.OffsetSystem, deltaRobot.ParametersDelta.OffsetArm);

        double eposPositionLH;
        double eposPositionLD;

        deltaRobot.CalculateColdStartPositions(MotorUp.EposData.PositionActualSensor2,
            MotorDown.EposData.PositionActualSensor2, out eposPositionLH,
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
        ProduktLisActual.Clear();
        IL.ZonePress.Release(EnZoneOwner.Manipulator, EnZoneStatus.Unknown);
        return 99;
    } //Homing Delta ramien - nastavenie parametrov

    // ==========================================
    // METÓDY PRE MAIN PROGRAM (Prepojené na reálne motory)
    // ==========================================
    private int MainStep100(int step)
    {
        Message = "Cakanie na vyrobok";
        StatusCycle = EnStatusCycle.Moving;

        if (RequestToEnd)
        {
            return 0;
        }


        return 110;
    } // -> 110

    private int MainStep110(int step)
    {
        Message = "Start";
        if (RequestToEnd)
        {
            return 0;
        }

        return 120;
    } // -> 120

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
        deltaRobot.MoveToPolar(0, 210);
        deltaRobot.WaitForTargetReached(5000);
        return 130;
    } // Vychodiskova poloha ->130

    private int MainStep130(int step)
    {
        Message = "Cakanie na Lis";
        if (RequestToEnd)
        {
            return 0;
        }

        if (IL.ZonePress.TryLock(EnZoneOwner.Manipulator, EnZoneStatus.OutputFullOk))
        {
            ProduktLisActual.Status = EnProduktLis.Ok;
            return 140;
        }

        if (IL.ZonePress.TryLock(EnZoneOwner.Manipulator, EnZoneStatus.OutputFullNok))
        {
            ProduktLisActual.Status = EnProduktLis.Nok;
            return 140;
        }

        return step;
    } // Cakanie na lis ->140

    private int MainStep140(int step)
    {
        Message = "Vysunutie k lisu";
        deltaRobot.MoveToPolar(0, 255);
        deltaRobot.WaitForTargetReached(5000);
        return 150;
    } // Vysunutie k lisu ->150

    private int MainStep150(int step)
    {
        Message = "Dolu nad vyrobok";
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(-13, true, true);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);
        return 160;
    } // Dolu nad vyrobok -> 160

    private int MainStep160(int step)
    {
        Message = "Uchopenie";
        if (!jaws.SetPosCurrent("kv4", -6.7, -30, 1, 2000))
        {
            ResultUchopenie = false;
            return 170;
        }

        ResultUchopenie = true;
        return 170;
    } //Uchopenie OK->170  NOK->170

    private int MainStep170(int step)
    {
        Message = "Kontrola uchopenia";

        return 180;
    } //KOntrola uchopenia -> 180

    private int MainStep180(int step)
    {
        Message = "Zdvih";
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(-9, true, true);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);
        return 190;
    } //Zdvih ->190

    private int MainStep190(int step)
    {
        Message = "Zasunutie";
        deltaRobot.MoveToPolar(0, 210);
        deltaRobot.WaitForTargetReached(5000);
        if (MatrixNOK.LastItem || MatrixOK.LastItem)
        {
            IL.ZonePress.Release(EnZoneOwner.Manipulator, EnZoneStatus.StackFull);
        }
        else
        {
            IL.ZonePress.Release(EnZoneOwner.Manipulator, EnZoneStatus.OutputEmpty);
        }

        return 200;
    } //Odchod z lisu a uvolnenie zony -> 220

    private int MainStep200(int step)
    {
        Message = "";
        deltaRobot.MoveToPolar(0, 140);
        deltaRobot.WaitForTargetReached(5000);
        return 220;
    } //Zasunutie na vykladanie

    private int MainStep210(int step)
    {
        Message = "";

        return 220;
    } //

    private int MainStep220(int step)
    {
        Message = "Vysunutie na vykladanie";

        if (ProduktLisActual.Status == EnProduktLis.Ok)
        {
            deltaRobot.MoveToXY(MatrixOK.Xactual, MatrixOK.Yactual);
        }
        else
        {
            deltaRobot.MoveToXY(MatrixNOK.Xactual, MatrixNOK.Yactual);
        }

        deltaRobot.WaitForTargetReached(5000);

        return 230;
    } //Vysunutie na vykladanie -> 230

    private int MainStep230(int step)
    {
        Message = "Vyska na vylozenie";
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(-35, true, true);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);
        return 240;
    } //Vyska na vylozenie ->240

    private int MainStep240(int step)
    {
        Message = "Celuste vyloz";
        jaws._motorJaws.Operation.ProfilePositionMode.ActivateProfilePositionMode();
        jaws._motorJaws.Operation.ProfilePositionMode.MoveToPositionGear(-2, true, true);
        jaws._motorJaws.Operation.MotionInfo.WaitForTargetReached(5000);
        return 250;
    } //Celuste vyloz - > 250

    private int MainStep250(int step)
    {
        Message = "Vyska nad box";
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(-9, true, true);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);
        return 260;
    } //Vyska nad box test  -> 260

    private int MainStep260(int step)
    {
        Message = "Test naplnenia zasobnikov";
        if (MatrixNOK.LastItem || MatrixOK.LastItem) //test ze bol posledny OK alebo NOK
        {
            if (ProduktLisActual.Status == EnProduktLis.Ok)
            {
                if (MatrixOK.SetNextItem()) ; // Ak je  OK nastav dalsi polozku 
            }
            else
            {
                if (MatrixNOK.SetNextItem()) ; // Ak je NOK  nastav dalsi polozku
            }
            return 270;
        }
        
        if (ProduktLisActual.Status == EnProduktLis.Ok)
        {
            if (MatrixOK.SetNextItem()) ; // Ak je  OK nastav dalsi polozku 
        }
        else
        {
            if (MatrixNOK.SetNextItem()) ; // Ak je NOK  nastav dalsi polozku
        }
        

        return 100;
    } // Test naplnenia OK/NOK parkuj a koniec->270 , ak nie ideme dalsi cyklus ->100

    private int MainStep270(int step)
    {
        Message = "Parkuj";
        Log.Logger.ForContext("Name", Name)
            .Information(
                $"Manipulator: Ulozene OK: {MatrixOK.ActualItem}/{MatrixOK.CountItem}, NOK: {MatrixNOK.ActualItem}/{MatrixNOK.CountItem}");
        deltaRobot.MoveToPolar(0, 140);
        MotorZ.Operation.ProfilePositionMode.MoveToPositionGear(-9, true, true);
        jaws._motorJaws.Operation.ProfilePositionMode.ActivateProfilePositionMode();
        jaws._motorJaws.Operation.ProfilePositionMode.MoveToPositionGear(5, true, true);
        deltaRobot.WaitForTargetReached(5000);
        MotorZ.Operation.MotionInfo.WaitForTargetReached(5000);
        jaws._motorJaws.Operation.MotionInfo.WaitForTargetReached(5000);
        
        return 280;
    } // Manipulator zaparkovany -> 280 
    
    private int MainStep280(int step)
    {
        Message = "Caka na zaparkovanie lisu";
        if (IL.ZonePress.TryLock(EnZoneOwner.Manipulator, EnZoneStatus.Unknown))
        {
            ProduktLisActual.Status = EnProduktLis.Unknow;
            return 0;
        }
        return Step;
    } // cakaj na zaparkovanie lisu. Prebratie zony a Koniec-> 0 
    

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

        var actualSSIDown = MotorDown.EposData.PositionActualSensor2;
        var actualDown = MotorDown.EposData.PositionActual;
        var actualDownGear = MotorDown.EposData.PositionActualGear;
        var actualSSIUp = MotorUp.EposData.PositionActualSensor2;
        var actualUp = MotorUp.EposData.PositionActual;
        var actualUpGear = MotorUp.EposData.PositionActualGear;
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