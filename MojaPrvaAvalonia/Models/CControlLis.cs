using System;
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
        return 99;
    } //Lis: Pripravený

    // ==========================================
    // METÓDY PRE MAIN PROGRAM
    // ==========================================
    private int MainStep100(int step)
    {
        Message = "Lis: Čakám na diel";
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
            return 110;
        }
        return step;
    }

    private int MainStep110(int step)
    {
        Message = "";
        return 120;
    } //

    private int MainStep120(int step)
    {
        Message = "Presun do nasypacich poloh";
        StatusCycle = EnStatusCycle.Moving;

        MotorMaster.Operation.ProfilePositionMode.MoveToPositionGear(ParametersLis.ParLis.VyskaNasypacia, true, true);
        MotorStred.Operation.ProfilePositionMode.MoveToPositionGear(ParametersLis.ParKonzola.VyskaNasypacia, true,
            true);
        MotorStred.Operation.MotionInfo.WaitForTargetReached(5000);
        MotorStred.Operation.MotionInfo.WaitForTargetReached(10000);
        return 130;
    } //Presun do nasypacich poloh ->130

    private int MainStep130(int step)
    {
        Message = "Caka na ready vahy";
        StatusCycle = EnStatusCycle.WaitForStep;
        // if (Vaha1.DeviceESD.Data.VaStatus == EVaStatus.Ready)
        // {
        Thread.Sleep(2000);
        return 140;
        // }

        return step;
    } //Caka na ready vahy

    private int MainStep140(int step)
    {
        Message = "Test pripraveni davky";
        StatusCycle = EnStatusCycle.Inspecting;
        Thread.Sleep(100);
        //  if (Vaha1.DeviceESD.Data.VaStatus2 == EVaStatus2.Full)
        //  {

        return 50;
        //  }
        return 40;
    } //Test pripraveni davky

    private int MainStep150(int step)
    {
        Message = "";
        return 100;
    } //

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