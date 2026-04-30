using System.Collections.ObjectModel;
using Serilog;

namespace MojaPrvaAvalonia.Models;

public partial class CManipulator : CPlc
{
    public ObservableCollection<CMotorData> Motory { get; } = new ObservableCollection<CMotorData>();

    public CManipulator(string name) : base(name)
    {
        Motory.Add(new CMotorData { Name = "Zdvih" });
        Motory.Add(new CMotorData { Name = "Axis X" });
        Motory.Add(new CMotorData { Name = "Axis Y" });
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
        Message = "Init 10: Resetovanie pohonov";
        foreach (var motor in Motory)
        {
            motor.Speed = 0;
            motor.Position = 0;
            motor.Current = 0;
        }
        return 20;
    }

    private int InitStep20(int step)
    {
        Message = "Init 20: Kontrola senzorov a kamier";
        return 30;
    }

    private int InitStep30(int step)
    {
        Message = "Init 30: Presun do Home pozície";
        foreach (var motor in Motory)
        {
            motor.Speed = 500;
            motor.Position = 10;
        }
        return 40;
    }

    private int InitStep40(int step)
    {
        Message = "Init 40: Inicializácia dokončená";
        foreach (var motor in Motory)
        {
            motor.Speed = 0;
        }
        // Vraciame 99 -> Slučka to zachytí a nastaví stav na Ready
        return 99; 
    }

    // ==========================================
    // METÓDY PRE MAIN PROGRAM
    // ==========================================
    private int MainStep100(int step)
    {
        Message = "Main 100: Kontrola parkovania";
        StatusCycle = EnStatusCycle.Moving;

        // Ak bolo stlačené tlačidlo "Parkovať", ukončíme slučku
        if (RequestToEnd)
        {
            Log.Logger.ForContext("Name", Name).Information("Zachytená požiadavka na parkovanie, ukončujem program.");
            return 0; // Vraciame 0 -> Slučka to zachytí a nastaví stav na NotInit
        }

        return 110;
    }

    private int MainStep110(int step)
    {
        Message = "Main 110: Zdvih dole";
        Motory[0].Speed = 1000;
        Motory[0].Position = 150;
        return 120;
    }

    private int MainStep120(int step)
    {
        Message = "Main 120: Presun X a Y nad diel";
        Motory[1].Speed = 2000; Motory[1].Position = 300;
        Motory[2].Speed = 2000; Motory[2].Position = 450;
        return 130;
    }

    private int MainStep130(int step)
    {
        Message = "Main 130: Inšpekcia dielu";
        StatusCycle = EnStatusCycle.Inspecting;
        return 140;
    }

    private int MainStep140(int step)
    {
        Message = "Main 140: Spracovanie dielu";
        StatusCycle = EnStatusCycle.Moving;
        return 150;
    }

    private int MainStep150(int step)
    {
        Message = "Main 150: Zdvih hore";
        Motory[0].Speed = 1000;
        Motory[0].Position = 10;
        return 160;
    }

    private int MainStep160(int step)
    {
        Message = "Main 160: Návrat X a Y";
        Motory[1].Speed = 2000; Motory[1].Position = 10;
        Motory[2].Speed = 2000; Motory[2].Position = 10;
        return 170;
    }

    private int MainStep170(int step)
    {
        Message = "Main 170: Koniec cyklu";
        foreach (var motor in Motory)
        {
            motor.Speed = 0;
        }
        
        // Návrat na začiatok hlavného cyklu
        return 100; 
    }
}