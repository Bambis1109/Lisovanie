using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EposCmd.Net;
using MojaPrvaAvalonia.Net;
using MojaPrvaAvalonia.ViewModels;
using Serilog;

namespace MojaPrvaAvalonia.Models;

public partial class CControlLis : CPlcEpos
{
    public CDeviceEpos4 MotorStred { get; set; }
    public CDeviceEpos4 MotorSlave { get; set; }
    public CDeviceEpos4 MotorMaster { get; set; }
    public CParametersLis ParametersLis { get; set; } = new();

    [ObservableProperty] private double _stepSize = 1.0;

    // --- Limity pre manuálny pohyb ---
    [ObservableProperty] private double _limitStredUp = -90.0;
    [ObservableProperty] private double _limitStredDown = -14.0;
    [ObservableProperty] private double _limitLisUp = 0.0;
    [ObservableProperty] private double _limitLisDown = -220.0;
    private Stopwatch SW;

    public Double SilaActual
    {
        get => (int)(((double)MotorSlave.EposData.AnalogInput1 - 2000) * 1.25);
    } // ToDo doplnit do zobrazenia  frmLisSetup

    public double PositionActualSensor2Float
    {
        get => (double)MotorMaster.EposData.PositionActualSensor2 / 1000;
    }// ToDo doplnit do zobrazenia  frmLisSetup

    public Double DistanceActual
    {
        get => ParametersLis.ParLis.RecomputedDistance(SilaActual, PositionActualSensor2Float);
    }// ToDo doplnit do zobrazenia  frmLisSetup

    public CControlLis(string name) : base(name)
    {
        LoadParameters();
        MotorViewModels.Add(new UcDeviceEpos4ViewModel(null, "Stred"));
        MotorViewModels.Add(new UcDeviceEpos4ViewModel(null, "Slave"));
        MotorViewModels.Add(new UcDeviceEpos4ViewModel(null, "Master"));
    }

    public override async Task ConnectAsync()
    {
        await base.ConnectAsync();

        if (Connection == EnStatusConnection.Connected)
        {
            // Aktualizácia ViewModelov po pripojení
            MotorViewModels[0].AssignDevice(MotorStred);
            MotorViewModels[1].AssignDevice(MotorSlave);
            MotorViewModels[2].AssignDevice(MotorMaster);
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

            default: return base.RunStep(step);
        }
    }

    // ==========================================
    // METÓDY PRE INIT
    // ==========================================
    private int InitStep1(int step)
    {
        Message = "Lis: Štart inicializácie";
        StatusCycle = EnStatusCycle.Moving;
        return 10;
    }

    private int InitStep10(int step)
    {
        Message = "Mazanie chyb a nastav enable";
        ClearAllFaults();
        EnableAllMotors();
        foreach (var motor in Motors)
        {
            if (motor.Operation?.StateMachine == null) continue;
            motor.Operation.HomingMode?.ActivateHomingMode();
        }

        return 20;
    } //Mazanie chyb a nastav enable


    private int InitStep20(int step)
    {
        Message = "Lis: Hladanie horneho dorazu";
        MotorSlave.Operation.HomingMode.SetHomingParameter(100, 300, 200, 10000, 2000, 0,
            EHomingMethod.HmCurrentThresholdPositiveSpeed);
        MotorMaster.Operation.HomingMode.SetHomingParameter(100, 300, 200, 10000, 2000, 0,
            EHomingMethod.HmCurrentThresholdPositiveSpeed);
        MotorMaster.Operation.HomingMode.FindHome();
        MotorSlave.Operation.HomingMode.FindHome();
        MotorMaster.Operation.MotionInfo.WaitForHomingAttained(100000);
        MotorSlave.Operation.MotionInfo.WaitForHomingAttained(100000);

        MotorSlave.Operation.StateMachine.SetDisableState();
        MotorMaster.Operation.StateMachine.SetDisableState();
        Thread.Sleep(2000);
        MotorSlave.Operation.StateMachine.SetEnableState();
        MotorMaster.Operation.StateMachine.SetEnableState();
        return 30;
    } //Lis: Hladanie horneho dorazu

    private int InitStep30(int step)
    {
        Message = "Lis: Nulovanie polohy";
        MotorSlave.Operation.HomingMode.SetHomingParameter(100, 300, 200, 0, 2000, 0,
            EHomingMethod.HmActualPosition);
        MotorMaster.Operation.HomingMode.SetHomingParameter(100, 300, 200, 0, 2000, 0,
            EHomingMethod.HmActualPosition);
        MotorMaster.Operation.HomingMode.FindHome();
        MotorSlave.Operation.HomingMode.FindHome();
        MotorMaster.Operation.MotionInfo.WaitForHomingAttained(20000);
        MotorSlave.Operation.MotionInfo.WaitForHomingAttained(20000);

        return 40;
    } //Lis: Nulovanie polohy

    private int InitStep40(int step)
    {
        Message = "Lis: Pripravený";
        MotorSlave.Operation.ProfilePositionMode.SetPositionProfile(1600, 2000, 2000);
        MotorMaster.Operation.ProfilePositionMode.SetPositionProfile(1600, 2000, 2000);
        MotorMaster.Operation.ProfilePositionMode.ActivateProfilePositionMode();
        MotorSlave.Operation.CyclicSynPositionMode.ActivateCyclicSynPositionMode();

        Thread.Sleep(100);
        MotorMaster.Operation.StateMachine.SetEnableState();
        MotorSlave.Operation.StateMachine.SetEnableState();

        MotorStred.Operation.ProfilePositionMode.SetPositionProfile(300, 5000, 5000);
        MotorStred.Operation.ProfilePositionMode.ActivateProfilePositionMode();
        MotorStred.Operation.StateMachine.SetEnableState();
        MotorMaster.Operation.ProfilePositionMode.MoveToPositionGear(-10, true, true);
        MotorStred.Operation.ProfilePositionMode.MoveToPositionGear(-21, true, true);
        MotorStred.Operation.MotionInfo.WaitForTargetReached(3000);
        MotorMaster.Operation.MotionInfo.WaitForTargetReached(10000);
        return 99;
    } //Lis: Pripravený koniec inicializacie

    // ==========================================
    // METÓDY PRE MAIN PROGRAM
    // ==========================================
    private int MainStep100(int step)
    {
        Message = "Čakám na material";
        StatusCycle = EnStatusCycle.Moving;

        if (RequestToEnd) // ak je poziadavka na parkovanie parkujem
        {
            Log.Logger.ForContext("Name", Name).Information("Lis: Parkujem.");
            return 0;
        }

        // Lis čaká, kým mu váha nenechá InputFull
        if (IL.ZonePress.TryLock(EnZoneOwner.Press, EnZoneStatus.InputFull))
        {
            // 1. Zóna je naša. Okamžite ju označíme ako "spracováva sa"
            IL.ZonePress.Status = EnZoneStatus.OutputProced;
            Thread.Sleep(2000);
            return 110;
        }

        return step;
    }

    private int MainStep110(int step)
    {
        Message = "Zaciatok lisovania ";
        Thread.Sleep(100);
        return 120;
    } //Zaciatok lisovania -> 120

    private int MainStep120(int step)
    {
        Message = "Priblizenie lisu";
        MotorMaster.Operation.ProfilePositionMode.MoveToPositionGear(ParametersLis.ParLis.VyskaPriblizenie, true, true);
        MotorMaster.Operation.MotionInfo.WaitForTargetReached(10000);
        return 130;
    } //Priblize nie lisy -> 130

    private int MainStep130(int step)
    {
        Message = "Konzola na poziciu lisovania a uvolnenie";
        MotorStred.Operation.ProfilePositionMode.MoveToPositionGear(-40, true, true);
        MotorStred.Operation.MotionInfo.WaitForTargetReached(10000);
        MotorStred.Operation.StateMachine.SetDisableState();
        return 140;
    } //Konzola na poziciu lisovania a uvolnenie-> 140

    private int MainStep140(int step)
    {
        Message = "Merania tlaku";
        if (SilaActual > ParametersLis.ParVyrobok.SilaPozadovana)
        {
            SW = new Stopwatch(); // ak je sila vatsia ako pozadovana spusti meranie casu a skoc na 160
            SW.Start();
            return 160;
        }

        return 150;
    } //Meranie sily  ak je sila vatsia ako pozadovana spusti meranie casu -> 160 inac  ->150

    private int MainStep150(int step)
    {
        Message = "Dotlac na silu";
        if (DistanceActual < ParametersLis.ParVyrobok.VyskaMin)
            return 140;
        double pos = -0.5;
        if (SilaActual > ParametersLis.ParVyrobok.SilaPozadovana - 300) pos = -0.2;
        if (SilaActual > ParametersLis.ParVyrobok.SilaPozadovana - 100) pos = -0.02;
        MotorMaster.Operation.ProfilePositionMode.MoveToPositionGear(pos, false, true);
        Thread.Sleep(10);
        return 140; // vrat sa na meranie tlaku
    } // Dotlac na silu ak nie je  -> 140

    [RelayCommand]
    public void SaveParameters()
    {
        SaveParametersToFile("ParametersLis.json", ParametersLis);
    }

    [RelayCommand]
    public void LoadParameters()
    {
        LoadParametersFromFile("ParametersLis.json", ParametersLis);
    }

    // --- Ovládanie Stred ---
    [RelayCommand]
    public async Task EnableStredAsync()
    {
        try
        {
            await Task.Run(() => MotorStred?.Operation?.StateMachine?.SetEnableState());
        }
        catch (Exception ex)
        {
            Log.Error($"EnableStred Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task DisableStredAsync()
    {
        try
        {
            await Task.Run(() => MotorStred?.Operation?.StateMachine?.SetDisableState());
        }
        catch (Exception ex)
        {
            Log.Error($"DisableStred Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task MoveStredUpAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                if (MotorStred?.Data == null) return;

                double current = MotorStred.EposData.PositionActualGear;
                double future = current - StepSize; // Smer UP odoberá (otočená os)

                double min = Math.Min(LimitStredUp, LimitStredDown);
                double max = Math.Max(LimitStredUp, LimitStredDown);

                if (future < min || future > max)
                {
                    Log.Logger.ForContext("Name", Name)
                        .Error(
                            $"Pohyb Stred UP zrušený. Budúca poloha {future:F2} prekračuje povolený rozsah <{min}, {max}>.");
                    return;
                }

                MotorStred.Operation?.ProfilePositionMode?.MoveToPositionGear(-StepSize, false, true);
            });
        }
        catch (Exception ex)
        {
            Log.Error($"MoveStredUp Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task MoveStredDownAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                if (MotorStred?.Data == null) return;

                double current = MotorStred.EposData.PositionActualGear;
                double future = current + StepSize; // Smer DOWN pridáva (otočená os)

                double min = Math.Min(LimitStredUp, LimitStredDown);
                double max = Math.Max(LimitStredUp, LimitStredDown);

                if (future < min || future > max)
                {
                    Log.Logger.ForContext("Name", Name)
                        .Error(
                            $"Pohyb Stred DOWN zrušený. Budúca poloha {future:F2} prekračuje povolený rozsah <{min}, {max}>.");
                    return;
                }

                MotorStred.Operation?.ProfilePositionMode?.MoveToPositionGear(StepSize, false, true);
            });
        }
        catch (Exception ex)
        {
            Log.Error($"MoveStredDown Error: {ex.Message}");
        }
    }

    // --- Ovládanie Lis ---
    [RelayCommand]
    public async Task EnableLisAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                MotorMaster?.Operation?.StateMachine?.SetEnableState();
                MotorSlave?.Operation?.StateMachine?.SetEnableState();
            });
        }
        catch (Exception ex)
        {
            Log.Error($"EnableLis Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task DisableLisAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                MotorMaster?.Operation?.StateMachine?.SetDisableState();
                MotorSlave?.Operation?.StateMachine?.SetDisableState();
            });
        }
        catch (Exception ex)
        {
            Log.Error($"DisableLis Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task MoveLisUpAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                if (MotorMaster?.Data == null) return;

                double current = MotorMaster.EposData.PositionActualGear;
                double future = current + StepSize; // UP smeruje k 0

                double min = Math.Min(LimitLisUp, LimitLisDown);
                double max = Math.Max(LimitLisUp, LimitLisDown);

                if (future < min || future > max)
                {
                    Log.Logger.ForContext("Name", Name)
                        .Error(
                            $"Pohyb Lis UP zrušený. Budúca poloha {future:F2} prekračuje povolený rozsah <{min}, {max}>.");
                    return;
                }

                MotorMaster.Operation?.ProfilePositionMode?.MoveToPositionGear(StepSize, false, true);
            });
        }
        catch (Exception ex)
        {
            Log.Error($"MoveLisUp Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task MoveLisDownAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                if (MotorMaster?.Data == null) return;

                double current = MotorMaster.EposData.PositionActualGear;
                double future = current - StepSize; // DOWN smeruje k -220

                double min = Math.Min(LimitLisUp, LimitLisDown);
                double max = Math.Max(LimitLisUp, LimitLisDown);

                if (future < min || future > max)
                {
                    Log.Logger.ForContext("Name", Name)
                        .Error(
                            $"Pohyb Lis DOWN zrušený. Budúca poloha {future:F2} prekračuje povolený rozsah <{min}, {max}>.");
                    return;
                }

                MotorMaster.Operation?.ProfilePositionMode?.MoveToPositionGear(-StepSize, false, true);
            });
        }
        catch (Exception ex)
        {
            Log.Error($"MoveLisDown Error: {ex.Message}");
        }
    }

    // --- Priame presuny na polohy ---
    [RelayCommand]
    public async Task MoveStredPos84Async()
    {
        try
        {
            await Task.Run(() => MotorStred.Operation?.ProfilePositionMode?.MoveToPositionGear(-84, true, true));
        }
        catch (Exception ex)
        {
            Log.Error($"MoveStredPos84 Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task MoveStredPos60Async()
    {
        try
        {
            await Task.Run(() => MotorStred.Operation?.ProfilePositionMode?.MoveToPositionGear(-60, true, true));
        }
        catch (Exception ex)
        {
            Log.Error($"MoveStredPos60 Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task MoveStredPos37Async()
    {
        try
        {
            await Task.Run(() => MotorStred.Operation?.ProfilePositionMode?.MoveToPositionGear(-37, true, true));
        }
        catch (Exception ex)
        {
            Log.Error($"MoveStredPos37 Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task MoveStredPos14Async()
    {
        try
        {
            await Task.Run(() => MotorStred.Operation?.ProfilePositionMode?.MoveToPositionGear(-14, true, true));
        }
        catch (Exception ex)
        {
            Log.Error($"MoveStredPos14 Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task MoveMasterPos0Async()
    {
        try
        {
            await Task.Run(() => MotorMaster.Operation?.ProfilePositionMode?.MoveToPositionGear(0, true, true));
        }
        catch (Exception ex)
        {
            Log.Error($"MoveMasterPos0 Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task MoveMasterPosMinus100Async()
    {
        try
        {
            await Task.Run(() => MotorMaster.Operation?.ProfilePositionMode?.MoveToPositionGear(-100, true, true));
        }
        catch (Exception ex)
        {
            Log.Error($"MoveMasterPosMinus100 Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task MoveMasterPosMinus160Async()
    {
        try
        {
            await Task.Run(() => MotorMaster.Operation?.ProfilePositionMode?.MoveToPositionGear(-160, true, true));
        }
        catch (Exception ex)
        {
            Log.Error($"MoveMasterPosMinus160 Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task MoveMasterPosMinus220Async()
    {
        try
        {
            await Task.Run(() => MotorMaster.Operation?.ProfilePositionMode?.MoveToPositionGear(-220, true, true));
        }
        catch (Exception ex)
        {
            Log.Error($"MoveMasterPosMinus220 Error: {ex.Message}");
        }
    }
}