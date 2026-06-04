using System;
using CommunityToolkit.Mvvm.ComponentModel;
using EposCmd.Net;
using Serilog;

namespace MojaPrvaAvalonia.Models;

public class CJaws: ObservableObject
{
    public CJaws()
    {
        
    }

    /// <summary>
    /// Nastavenie referencií na fyzické motory pre výpočet polohy.
    /// </summary>
    public void SetMotors(CDeviceEpos4 motorJaws)
    {
        _motorJaws = motorJaws;
     
    }
    public CDeviceEpos4 _motorJaws {get; set;}

   public bool SetPosCurrent(string measure, double midlevalue, double percentageForce, double range, int timeout)
    {
        // 1. Výpočet pre-grip pozície (na okraji tolerančného pásma podľa smeru)
        double preposition = (percentageForce > 0) ? midlevalue - range : midlevalue + range;

        // 2. Fáza rýchleho priblíženia (PPM)
        _motorJaws.Operation.ProfilePositionMode.ActivateProfilePositionMode();
        _motorJaws.Operation.ProfilePositionMode.MoveToPositionGear(preposition, true, true);
        _motorJaws.Operation.MotionInfo.WaitForTargetReached(5000);

        // 3. Fáza aplikácie sily (CST)
        _motorJaws.Operation.CurrentMode.ActivateCyclicSyncronicTorqueMode();
        // MotorJaws.Operation.StateMachine.SetEnableState(); // Odstránené - redundantné

        // Čaká na dosiahnutie sily a mechanické ustálenie (obsahuje vlastný stabilizačný counter)
        _motorJaws.Operation.CurrentMode.WaitToTorqueStopMovePercentage(timeout, percentageForce);

        // Thread.Sleep(10); // Odstránené - ušetrený čas cyklu

        // 4. Načítanie skutočnej pozície
        var actual = _motorJaws.EposData.PositionActualGear;

        // 5. Vyhodnotenie tolerancie (Čitateľnejší zápis pôvodnej matematickej logiky)
        // Kontroluje, či je absolútna odchýlka od stredu menšia alebo rovná tolerancii (range)
        bool isOk = Math.Abs(actual - midlevalue) <= range;

        // 6. Logovanie do DB (Formát zachovaný presne podľa zadania)
        if (isOk)
        {
            Log.Logger.ForContext("Name",_motorJaws.Name)
                .ForContext("Measure", measure)
                .Verbose(
                    $"percentage:[{percentageForce}], midle: [{midlevalue:0.00}], range: [{range:0.00}], actual: [{actual:0.00}], result: [true]");
            return true;
        }
        else
        {
            Log.Logger.ForContext("Name", _motorJaws.Name)
                .ForContext("Measure", measure)
                .Error(
                    $"percentage:[{percentageForce}], midle: [{midlevalue:0.00}], range: [{range:0.00}], actual: [{actual:0.00}], result: [false]");
            return false;
        }
    }
}
