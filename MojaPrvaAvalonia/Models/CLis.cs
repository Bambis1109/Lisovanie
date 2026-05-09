using System.Collections.ObjectModel;
using MojaPrvaAvalonia.ViewModels;
using Serilog;

namespace MojaPrvaAvalonia.Models;

public partial class CLis : CPlc
{
    public ObservableCollection<UcMotorViewModel> MotorViewModels { get; } = new ObservableCollection<UcMotorViewModel>();

    public CLis(string name) : base(name)
    {
        MotorViewModels.Add(new UcMotorViewModel(null, "Press"));
        MotorViewModels.Add(new UcMotorViewModel(null, "Ejector"));
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
        Message = "Lis: Kontrola hydrauliky";
        return 20;
    }

    private int InitStep20(int step)
    {
        Message = "Lis: Kontrola bezpečnostných bariér";
        return 30;
    }

    private int InitStep30(int step)
    {
        Message = "Lis: Nastavenie lisovacej sily";
        return 40;
    }

    private int InitStep40(int step)
    {
        Message = "Lis: Pripravený";
        return 99; 
    }

    // ==========================================
    // METÓDY PRE MAIN PROGRAM
    // ==========================================
    private int MainStep100(int step)
    {
        Message = "Lis: Čakám na diel";
        StatusCycle = EnStatusCycle.Moving;

        if (RequestToEnd)
        {
            Log.Logger.ForContext("Name", Name).Information("Lis: Parkujem.");
            return 0;
        }

        return 110;
    }

    private int MainStep110(int step)
    {
        Message = "Lis: Lisovanie v procese";
        return 120;
    }

    private int MainStep120(int step)
    {
        Message = "Lis: Chladenie";
        StatusCycle = EnStatusCycle.Inspecting;
        return 130;
    }

    private int MainStep130(int step)
    {
        Message = "Lis: Vyťahovanie piesta";
        return 140;
    }

    private int MainStep140(int step)
    {
        Message = "Lis: Vyhadzovanie hotového dielu";
        return 150;
    }

    private int MainStep150(int step)
    {
        Message = "Lis: Cyklus dokončený";
        return 100; 
    }
}