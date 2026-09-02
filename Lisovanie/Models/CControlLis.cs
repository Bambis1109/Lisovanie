using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EposCmd.Net;
using Lisovanie.Net;
using Lisovanie.ViewModels;
using Serilog;
using Avalonia.Threading;

namespace Lisovanie.Models;

public partial class CControlLis : CPlcEpos
{
    public CDeviceEpos4 MotorStred { get; set; }
    public CDeviceEpos4 MotorSlave { get; set; }
    public CDeviceEpos4 MotorMaster { get; set; }
    public CParametersLis ParametersLis { get; set; } = new();
    public CProduktLis ProduktLisActual { get; set; } = new();
    public CProduktLis ProduktLisLast { get; set; } = new();

    [ObservableProperty] private double _silaActual;
    [ObservableProperty] private double _distanceActual;
    [ObservableProperty] private double _positionActualSensor2Float;

    [ObservableProperty] private double _stepSize = 1.0;

    // --- Limity pre manuálny pohyb ---
    [ObservableProperty] private double _limitStredUp = -90.0;
    [ObservableProperty] private double _limitStredDown = -14.0;
    [ObservableProperty] private double _limitLisUp = 0.0;
    [ObservableProperty] private double _limitLisDown = -220.0;
    private Stopwatch SW;
    private Stopwatch? _swZhutnovanie;
    private DispatcherTimer? _uiTimer;

    /// <summary>Hmotnosť aktuálnej dávky [g] prevzatá zo zóny pri InputFull.</summary>
    private double _aktualnaHmotnost;

    /// <summary>Logger výrobných dát (priradí ho CMainProgram). Null = neukladá sa.</summary>
    public CProductionLogger? ProductionLogger { get; set; }

    public CControlLis(string name) : base(name)
    {
        // Parametre načíta CRecipeManager.Apply() po výbere receptu pri štarte.
        MotorViewModels.Add(new UcDeviceEpos4ViewModel(null, "Stred"));
        MotorViewModels.Add(new UcDeviceEpos4ViewModel(null, "Slave"));
        MotorViewModels.Add(new UcDeviceEpos4ViewModel(null, "Master"));
        StartUiTimer();
    }

    private void StartUiTimer()
    {
        _uiTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _uiTimer.Tick += (s, e) =>
        {
            if (MotorSlave?.EposData != null)
                SilaActual = (int)(((double)MotorSlave.EposData.AnalogInput1 - 2000) * 1.25);

            if (MotorMaster?.EposData != null)
                PositionActualSensor2Float = (double)MotorMaster.EposData.PositionActualSensor2 / 1000;

            if (ParametersLis?.ParLis != null)
                DistanceActual = ParametersLis.ParLis.RecomputedDistance(SilaActual, PositionActualSensor2Float);
        };
        _uiTimer.Start();
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
            case 101: return MainStep101(step);
            case 103: return MainStep103(step);
            case 102: return MainStep102(step);
            case 105: return MainStep105(step);
            case 110: return MainStep110(step);
            case 120: return MainStep120(step);
            case 130: return MainStep130(step);
            case 135: return MainStep135(step);

            // --- Lisovanie na silu (kroky 140 - 190) ---
            case 140: return MainStep140(step);
            case 150: return MainStep150(step);
            case 160: return MainStep160(step);
            case 170: return MainStep170(step);
            case 180: return MainStep180(step);
            case 190: return MainStep190(step);

            case 200: return MainStep200(step);
            case 210: return MainStep210(step);

            // --- Lisovanie na vzdialenosť (kroky 300 - 350) ---
            case 300: return MainStep300(step);
            case 310: return MainStep310(step);
            case 320: return MainStep320(step);
            case 330: return MainStep330(step);
            case 340: return MainStep340(step);
            case 345: return MainStep345(step);
            case 350: return MainStep350(step);

            // --- Multi-mix: plnenie po vrstvách (kroky 400 - 460) ---
            case 400: return MainStep400(step);
            case 410: return MainStep410(step);
            case 420: return MainStep420(step);
            case 430: return MainStep430(step);
            case 440: return MainStep440(step);
            case 450: return MainStep450(step);
            case 460: return MainStep460(step);

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
        MotorMaster.Operation.HomingMode.ActivateHomingMode();
        MotorSlave.Operation.CyclicSynTorqueMode.ActivateCyclicSyncronicTorqueMode();
        MotorMaster.Operation.StateMachine.SetEnableState();
        MotorSlave.Operation.StateMachine.SetEnableState();
        return 20;
    } //Mazanie chyb a nastav enable

    private int InitStep20(int step)
    {
        Message = "Lis: Hladanie horneho dorazu";
       
        MotorMaster.Operation.HomingMode.SetHomingParameter(100, 300, 200, 10000, 2000, 0,
            EHomingMethod.HmCurrentThresholdPositiveSpeed);
        MotorMaster.Operation.HomingMode.FindHome();
        MotorMaster.Operation.MotionInfo.WaitForHomingAttained(100000);

        return 30;
    } //Lis: Hladanie horneho dorazu

    private int InitStep30(int step)
    {
        Message = "Lis: Nulovanie polohy";
        // Profil piesta sa nastavuje raz a platí pre všetky pohyby vrátane prítlačných
        // krokov (150, 310) a zrovnávania vrstvy v multi-mixe (420, 430).
        MotorMaster.Operation.ProfilePositionMode.SetPositionProfile(1600, 5000, 5000);
        MotorMaster.Operation.ProfilePositionMode.ActivateProfilePositionMode();
        return 40;
    } //Lis: Nulovanie polohy

    private int InitStep40(int step)
    {
        Message = "Lis: Pripravený";
     
    
        var par = ParametersLis.ParLisovanie;
        MotorStred.Operation.ProfilePositionMode.SetPositionProfile(
            par.ProfilRychlyVelocity, par.ProfilRychlyAcc, par.ProfilRychlyDcc);
        MotorStred.Operation.ProfilePositionMode.ActivateProfilePositionMode();
        MotorStred.Operation.StateMachine.SetEnableState();

        MotorStred.Operation.ProfilePositionMode.MoveToPositionGear(par.StredVychodzia, true, true);
        MotorStred.Operation.MotionInfo.WaitForTargetReached(3000);
        //  MotorMaster.Operation.MotionInfo.WaitForTargetReached(10000);
        ProduktLisActual.Clear();
        ProduktLisLast.Clear();
        return 99;
    } //Lis: Pripravený koniec inicializacie

    // ==========================================
    // METÓDY PRE MAIN PROGRAM
    // ==========================================
    private int MainStep100(int step)
    {
        Message = "Čakám na Init manipulatora";
        if (RequestToEnd) // ak je poziadavka na parkovanie parkujem
        {
            Log.Logger.ForContext("Name", Name).Information("Lis: Parkujem.");
            return 0;
        }

        // Lis čaká, kým mu manipulator neuvolni zonu
        if (IL.ZonePress.TryLock(EnZoneOwner.Press, EnZoneStatus.Unknown))
        {
            return 101;
        }

        return step;
    } //Čakám na Init manipulatora -> 101 (kontrola priechodnosti)

    // ------------------------------------------------------------------
    // KONTROLA PRIECHODNOSTI LISOVACEJ SÚPRAVY (kroky 101 - 103)
    //
    // Vyosená súprava sa dnes prejaví až v prevádzkovom kroku 120, teda až keď je dutina
    // plná prášku. Preto sa raz po štarte spraví ten istý pohyb naprázdno.
    //
    // Po Inite stojí konzola dole na ParLisovanie.StredVychodzia, pod úrovňou spodného
    // piesta, a horný piest je na hornom doraze. Piest preto zíde ako prvý a konzola sa
    // naň nasunie zdola - koncový stav (Master VyskaPriblizenie, Stred VyskaNasypacia) je
    // zhodný s prevádzkovým stavom po kroku 120, len sa k nemu prišlo opačným poradím.
    //
    // Kroky sú pred rozcestníkom 102, takže platia pre Single aj Multi režim. Prevádzkový
    // cyklus sa vracia 200 -> 102, čiže test prebehne práve raz na jedno stlačenie Štart.
    // Zóna počas testu ostáva zamknutá pre Press - váhy dostanú InputEmpty až v kroku
    // 105/400, takže sa nikdy nesype do dutiny, cez ktorú piest neprešiel.
    //
    // Kolíziu netreba vyhodnocovať zvlášť: WaitForTargetReached hlási Fault, Following
    // Error aj timeout ako CDeviceException, ktorú ProgramLoop premení na StatusPlc.Error
    // a vypne motory.
    // ------------------------------------------------------------------

    private int MainStep101(int step)
    {
        Message = "Kontrola priechodnosti: piest na priblíženie";
        Log.Logger.ForContext("Name", Name).Information(
            "Lis: kontrola priechodnosti lisovacej súpravy (bez náplne).");

        // Profil piesta sa nemení - platí ten z InitStep30, rovnako ako v kroku 120.
        MotorMaster.Operation.ProfilePositionMode.MoveToPositionGear(
            ParametersLis.ParLis.VyskaPriblizenie, true, true);
        MotorMaster.Operation.MotionInfo.WaitForTargetReached(10000);
        return 103;
    } //Piest na vysku priblizenia, konzola este dole -> 103

    private int MainStep103(int step)
    {
        Message = "Kontrola priechodnosti: vnorenie piesta do konzoly";

        // Vlastný test: dutina konzoly sa nasunie na stojaci piest. Pri vyosení tu vzniká
        // kolízia a EPOS4 ju nahlási skôr, než sa do dutiny dostane materiál.
        MotorStred.Operation.ProfilePositionMode.SetPositionProfile(
            ParametersLis.ParLisovanie.ProfilRychlyVelocity,
            ParametersLis.ParLisovanie.ProfilRychlyAcc,
            ParametersLis.ParLisovanie.ProfilRychlyDcc);
        MotorStred.Operation.ProfilePositionMode.MoveToPositionGear(
            ParametersLis.ParKonzola.VyskaNasypacia, true, true);
        MotorStred.Operation.MotionInfo.WaitForTargetReached(5000);

        Log.Logger.ForContext("Name", Name).Information(
            "Lis: kontrola priechodnosti OK - piest sa vnoril do dutiny konzoly.");
        return 102;
    } //Konzola hore na nasypaciu = vnorenie piesta do dutiny -> 102 (rozcestnik rezimu)

    /// <summary>
    /// Rozcestník podľa režimu výroby z receptu. Obe vetvy sú od tohto miesta oddelené;
    /// multi-mix sa vráti do spoločného reťazca až v kroku 120 (priblíženie lisu).
    /// </summary>
    private int MainStep102(int step)
    {
        if (ParametersLis.Mode == EnModeVyroby.Multi)
        {
            Message = "Multi-mix: plnenie po vrstvách";
            return 400;
        }

        return 105;
    } //Volba rezimu -> 105 (single) alebo 400 (multi-mix)

    private int MainStep105(int step)
    {
        Message = "Presun do nasypacej polohy";
        MotorStred.Operation.ProfilePositionMode.SetPositionProfile(
            ParametersLis.ParLisovanie.ProfilRychlyVelocity,
            ParametersLis.ParLisovanie.ProfilRychlyAcc,
            ParametersLis.ParLisovanie.ProfilRychlyDcc);
        MotorMaster.Operation.ProfilePositionMode.MoveToPositionGear(ParametersLis.ParLis.VyskaNasypacia, true, true);
        MotorStred.Operation.ProfilePositionMode.MoveToPositionGear(ParametersLis.ParKonzola.VyskaNasypacia, true,
            true);
        MotorStred.Operation.MotionInfo.WaitForTargetReached(5000);
        MotorMaster.Operation.MotionInfo.WaitForTargetReached(10000);
        IL.ZonePress.Release(EnZoneOwner.Press, EnZoneStatus.InputEmpty);
        return 110;
    } // Presun do nasypacej polohy

    private int MainStep110(int step)
    {
        Message = "Čakám na material";
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
            _aktualnaHmotnost = IL.ZonePress.PayloadHmotnost; // hmotnosť dávky z váhy
            ProduktLisLast.Copy(ProduktLisActual);
            ProduktLisActual.Clear();
            return 120;
        }

        return step;
    } //Cakanie na material

    private int MainStep120(int step)
    {
        Message = "Priblizenie lisu";
        MotorMaster.Operation.ProfilePositionMode.MoveToPositionGear(ParametersLis.ParLis.VyskaPriblizenie, true, true);
        MotorMaster.Operation.MotionInfo.WaitForTargetReached(10000);
        return 130;
    } //Priblizenie lisu -> 130

    private int MainStep130(int step)
    {
        Message = "Konzola na poziciu lisovania a uvolnenie";
        MotorStred.Operation.ProfilePositionMode.MoveToPositionGear(
            ParametersLis.ParKonzola.VyskaLisovacia, true, true);
        MotorStred.Operation.MotionInfo.WaitForTargetReached(10000);
        MotorStred.Operation.StateMachine.SetDisableState();
        _swZhutnovanie = Stopwatch.StartNew(); // meranie času zhutňovania až po dosiahnutie sily
        return 135;
    } //Konzola na poziciu lisovania a uvolnenie-> 135

    /// <summary>
    /// Rozcestník podľa metódy lisovania z receptu. Obe vetvy sú od tohto miesta
    /// úplne oddelené a majú vlastné ukončovacie kroky.
    /// </summary>
    private int MainStep135(int step)
    {
        if (ParametersLis.Metoda == EnMetodaLisovania.Vzdialenost)
        {
            Message = "Lisovanie na vzdialenosť";
            return 300;
        }

        Message = "Lisovanie na silu";
        return 140;
    } //Volba metody -> 140 (sila) alebo 300 (vzdialenost)

    private int MainStep140(int step)
    {
        Message = "Merania sily a hrubky";
        if (SilaActual > ParametersLis.ParVyrobok.SilaPozadovana)
        {
            _swZhutnovanie?.Stop(); // dosiahnutá sila -> koniec merania času zhutňovania
            SW = new Stopwatch(); // ak je sila vatsia ako pozadovana spusti meranie casu a skoc na 160
            SW.Start();
            return 160; // testovanie uplynutia casu
        }

        if (DistanceActual < ParametersLis.ParVyrobok.VyskaMin)
        {
            ProduktLisActual.Status = EnProduktLis.Nok;
            return 180; // zatlac dolu ??????????????????????????????????????
        }

        return 150;
    }
    //(SilaActual > SilaPozadovana) Dosiahnutie sily StopWatch start -> 160
    //(DistanceActual < VyskaMin) dosiahnutie minimalnej hrubky lisovania ->  0
    //->150

    private int MainStep150(int step)
    {
        Message = "Zatlac dolu podla sily";
        if (DistanceActual < ParametersLis.ParVyrobok.VyskaMin)
            return 140;

        var par = ParametersLis.ParLisovanie;
        double silaPozadovana = ParametersLis.ParVyrobok.SilaPozadovana;

        double pos = par.KrokPritlakuHruby;
        if (SilaActual > silaPozadovana - par.PrahStredny) pos = par.KrokPritlakuStredny;
        if (SilaActual > silaPozadovana - par.PrahJemny) pos = par.KrokPritlakuJemny;
        MotorMaster.Operation.ProfilePositionMode.MoveToPositionGear(pos, false, true);
        Thread.Sleep(10);
        return 140;
    } // Zatlaci dolu podla sily  a vrati sa na meranie sily a hrubky-> 140

    private int MainStep160(int step)
    {
        Message = "Meranie doby OK tlaku";
        if (SW.ElapsedMilliseconds > ParametersLis.ParLisovanie.DobaDrzaniaMs)
        {
            ProduktLisActual.Status = EnProduktLis.Ok;
            return 180; //ak je cas vatsi tak koniec  
        }

        return 170; // Presun na udrzanie sily
    } // Ak je cas vatsi tak koniec  -> 180 inac  udrzuj silu ->170 

    private int MainStep170(int step)
    {
        Message = "Skontroluje a doplni silu";
        if (SilaActual < ParametersLis.ParVyrobok.SilaPozadovana)
        {
            MotorMaster.Operation.ProfilePositionMode.MoveToPositionGear(
                ParametersLis.ParLisovanie.KrokUdrziavania, false, true);
        }

        Thread.Sleep(50);
        return 160;
    } // Udrzuje silu

    private int MainStep180(int step)
    {
        Message = "Uvolnenie koniec lisovania";
        ProduktLisActual.Sila = SilaActual;
        ProduktLisActual.Vyska = DistanceActual;

        MotorMaster.Operation.ProfilePositionMode.MoveToPositionGear(ParametersLis.ParLis.VyskaNasypacia, true, true);
        Thread.Sleep(100);
        MotorStred.Operation.StateMachine.SetEnableState();
        MotorStred.Operation.ProfilePositionMode.SetPositionProfile(
            ParametersLis.ParLisovanie.ProfilPomalyVelocity,
            ParametersLis.ParLisovanie.ProfilPomalyAcc,
            ParametersLis.ParLisovanie.ProfilPomalyDcc);
        MotorStred.Operation.ProfilePositionMode.MoveToPositionGear(ParametersLis.ParKonzola.VyskaOdoberacia, true,
            true);
        MotorStred.Operation.MotionInfo.WaitForTargetReached(5000);
   //     MotorMaster.Operation.MotionInfo.WaitForTargetReached(10000);

        return 190;
    } // Koniec lisovania uvolnenie

    private int MainStep190(int step)
    {
        Message = "Uvolnenie zony a nastavenie priznaku";

        // Trvalý záznam výrobných dát (neblokujúci zápis cez Channel).
        ProductionLogger?.Enqueue(new CProductionRecord
        {
            TimestampUtc = DateTime.UtcNow,
            Hmotnost = _aktualnaHmotnost,
            Sila = ProduktLisActual.Sila,
            Vzdialenost = ProduktLisActual.Vyska,
            CasZhutnovaniaMs = _swZhutnovanie?.ElapsedMilliseconds ?? 0,
            CasZotrvaniaMs = SW?.ElapsedMilliseconds ?? 0,
            Status = ProduktLisActual.Status,
            Metoda = EnMetodaLisovania.Sila
        });

        switch (ProduktLisActual.Status)
        {
            case EnProduktLis.Ok:
                IL.ZonePress.Release(EnZoneOwner.Press, EnZoneStatus.OutputFullOk);
                break;
            case EnProduktLis.Nok:
                IL.ZonePress.Release(EnZoneOwner.Press, EnZoneStatus.OutputFullNok);
                break;
            default:
                IL.ZonePress.Release(EnZoneOwner.Press, EnZoneStatus.OutputFullNok);
                break;
        }

        return 200;
    } // Uvolni zony a nastavi priznak
    
    

    private int MainStep200(int step)
    {
        Message = "Cakanie na odobratie vyrobku";
        if (RequestToEnd) // ak je poziadavka na parkovanie parkujem
        {
            Log.Logger.ForContext("Name", Name).Information("Lis: Parkujem.");
            return 0;
        }

        // caka pokial manipulator nastavi EnZoneStatus.OutputEmpty
        if (IL.ZonePress.TryLock(EnZoneOwner.Press, EnZoneStatus.OutputEmpty))
        {
            return 102; // cez rozcestnik, aby dalsi cyklus rozhodol podla rezimu vyroby
        }
        // caka pokial manipulator nastavi EnZoneStatus.StackFull plny zasobnik
        if (IL.ZonePress.TryLock(EnZoneOwner.Press, EnZoneStatus.StackFull))
        {
            
            return 210;
        }
        
        return step;
    } // Caka na odobratie vyrobku a navrat na zaciatok ->102 alebo ak je zasobnik plny tak zaparkovat a koniec
    private int MainStep210(int step)
    {
        Message = "Presun do cistiacej  polohy";
        MotorStred.Operation.ProfilePositionMode.SetPositionProfile(
            ParametersLis.ParLisovanie.ProfilRychlyVelocity,
            ParametersLis.ParLisovanie.ProfilRychlyAcc,
            ParametersLis.ParLisovanie.ProfilRychlyDcc);
        MotorMaster.Operation.ProfilePositionMode.MoveToPositionGear(ParametersLis.ParLis.VyskaCistenia, true, true);
        MotorStred.Operation.ProfilePositionMode.MoveToPositionGear(ParametersLis.ParKonzola.VyskaCistenia, true,
            true);
        MotorStred.Operation.MotionInfo.WaitForTargetReached(5000);
        MotorMaster.Operation.MotionInfo.WaitForTargetReached(10000);
        IL.ZonePress.Release(EnZoneOwner.Press, EnZoneStatus.Unknown);
        return 0;
    } // Presun do nasypacej polohy

    // ==========================================
    // METÓDY PRE LISOVANIE NA VZDIALENOSŤ (300 - 350)
    // ==========================================

    private int MainStep300(int step)
    {
        Message = "Meranie vzdialenosti a sily";

        // Cieľ sa testuje ako prvý: kus, ktorý cieľovú hrúbku dosiahne práve na hranici
        // sily, je ešte dobrý. SilaMax znamená Nok len vtedy, keď sa cieľ dosiahnuť nedá.
        if (DistanceActual <= ParametersLis.ParVyrobok.VyskaPozadovana)
        {
            _swZhutnovanie?.Stop(); // dosiahnutá hrúbka -> koniec merania času zhutňovania
            SW = new Stopwatch();
            SW.Start();
            return 320; // výdrž na dosiahnutej hrúbke
        }

        if (SilaActual > ParametersLis.ParVyrobok.SilaMax)
        {
            _swZhutnovanie?.Stop();
            SW = new Stopwatch(); // nespustené - výdrž neprebehla, do záznamu ide 0
            ProduktLisActual.Status = EnProduktLis.Nok;
            Log.Logger.ForContext("Name", Name).Warning(
                $"Lis: sila {SilaActual:F0} prekročila strop {ParametersLis.ParVyrobok.SilaMax:F0} " +
                $"pri hrúbke {DistanceActual:F2} - predávkovaná dutina.");
            return 340;
        }

        if (DistanceActual < ParametersLis.ParVyrobok.VyskaMin)
        {
            _swZhutnovanie?.Stop();
            SW = new Stopwatch(); // nespustené - výdrž neprebehla, do záznamu ide 0
            ProduktLisActual.Status = EnProduktLis.Nok;
            Log.Logger.ForContext("Name", Name).Error(
                $"Lis: hrúbka {DistanceActual:F2} klesla pod VyskaMin " +
                $"{ParametersLis.ParVyrobok.VyskaMin:F2} - havarijná zarážka.");
            return 340;
        }

        return 310;
    }
    //(Distance <= VyskaPozadovana) dosiahnutie cielovej hrubky -> 320
    //(Sila > SilaMax) predavkovana dutina -> 340
    //(Distance < VyskaMin) havarijna zarazka -> 340
    //->310

    private int MainStep310(int step)
    {
        Message = "Zatlač dolu podľa vzdialenosti";

        var par = ParametersLis.ParLisovanieVzdialenost;

        // Prah je zostávajúci odstup od cieľa - rovnaká sémantika ako pri silovej metóde,
        // len meraná v milimetroch.
        double odstup = DistanceActual - ParametersLis.ParVyrobok.VyskaPozadovana;

        double pos = par.KrokPritlakuHruby;
        if (odstup < par.PrahStredny) pos = par.KrokPritlakuStredny;
        if (odstup < par.PrahJemny) pos = par.KrokPritlakuJemny;

        MotorMaster.Operation.ProfilePositionMode.MoveToPositionGear(pos, false, true);
        Thread.Sleep(par.PauzaKrokuMs);
        return 300;
    } // Zatlaci dolu podla vzdialenosti a vrati sa na meranie -> 300

    private int MainStep320(int step)
    {
        Message = "Výdrž na dosiahnutej hrúbke";

        // Bez korekcie - MotorMaster je v ProfilePositionMode a navelenú polohu drží sám.
        // Prášok počas výdrže relaxuje, sila klesá; výsledná hrúbka sa meria až v kroku 330.
        if (SW.ElapsedMilliseconds > ParametersLis.ParLisovanieVzdialenost.DobaDrzaniaMs)
        {
            return 330;
        }

        return step;
    } // Drzi hrubku po zadanu dobu -> 330

    private int MainStep330(int step)
    {
        Message = "Vyhodnotenie výlisku";

        double vyska = DistanceActual;
        var vyrobok = ParametersLis.ParVyrobok;

        if (vyska >= vyrobok.VyskaMin && vyska <= vyrobok.VyskaMax)
        {
            ProduktLisActual.Status = EnProduktLis.Ok;
        }
        else
        {
            ProduktLisActual.Status = EnProduktLis.Nok;
            Log.Logger.ForContext("Name", Name).Warning(
                $"Lis: hrúbka {vyska:F2} je mimo pásma " +
                $"{vyrobok.VyskaMin:F2} - {vyrobok.VyskaMax:F2}.");
        }

        return 340;
    } // Klasifikacia podla pasma VyskaMin..VyskaMax -> 340

    private int MainStep340(int step)
    {
        Message = "Nadvihnutie konzoly";
        ProduktLisActual.Sila = SilaActual;
        ProduktLisActual.Vyska = DistanceActual;

        MotorMaster.Operation.ProfilePositionMode.MoveToPositionGear(ParametersLis.ParLis.VyskaNasypacia, true, true);
        Thread.Sleep(500);
        MotorStred.Operation.StateMachine.SetEnableState();
        MotorStred.Operation.ProfilePositionMode.SetPositionProfile(
            ParametersLis.ParLisovanie.ProfilPomalyVelocity,
            ParametersLis.ParLisovanie.ProfilPomalyAcc,
            ParametersLis.ParLisovanie.ProfilPomalyDcc);
        MotorStred.Operation.ProfilePositionMode.MoveToPositionGear(-1, false,
            true);
        MotorStred.Operation.MotionInfo.WaitForTargetReached(5000);

        return 345;
    } // Koniec lisovania uvolnenie

    private int MainStep345(int step)
    {
        Message = "Vylisovanie konzolou";
  
        MotorStred.Operation.ProfilePositionMode.MoveToPositionGear(ParametersLis.ParKonzola.VyskaOdoberacia, true,
            true);
        MotorStred.Operation.MotionInfo.WaitForTargetReached(10000);
        MotorMaster.Operation.MotionInfo.WaitForTargetReached(5000);
        return 350;
    } // Koniec lisovania uvolnenie
    private int MainStep350(int step)
    {
        Message = "Uvolnenie zony a nastavenie priznaku";

        ProductionLogger?.Enqueue(new CProductionRecord
        {
            TimestampUtc = DateTime.UtcNow,
            Hmotnost = _aktualnaHmotnost,
            Sila = ProduktLisActual.Sila,
            Vzdialenost = ProduktLisActual.Vyska,
            CasZhutnovaniaMs = _swZhutnovanie?.ElapsedMilliseconds ?? 0,
            CasZotrvaniaMs = SW?.ElapsedMilliseconds ?? 0,
            Status = ProduktLisActual.Status,
            Metoda = EnMetodaLisovania.Vzdialenost
        });

        switch (ProduktLisActual.Status)
        {
            case EnProduktLis.Ok:
                IL.ZonePress.Release(EnZoneOwner.Press, EnZoneStatus.OutputFullOk);
                break;
            case EnProduktLis.Nok:
                IL.ZonePress.Release(EnZoneOwner.Press, EnZoneStatus.OutputFullNok);
                break;
            default:
                IL.ZonePress.Release(EnZoneOwner.Press, EnZoneStatus.OutputFullNok);
                break;
        }

        return 200;
    } // Uvolni zonu a nastavi priznak, pokracuje spolocnym krokom 200

    // ==========================================
    // MULTI-MIX: PLNENIE PO VRSTVÁCH (Kroky 400 - 460)
    //
    // Výlisok vzniká z troch zmesí. Po prvej vrstve piest zíde na absolútnu polohu
    // a zrovná kopec vzniknutý nasypom do roviny, aby ďalšie zmesi sadli rovnomerne -
    // je to len pohyb dole a späť, bez merania sily a bez zmeny profilu.
    // Konzola (motor Stred) sa počas celého plnenia nehýbe - ostáva v nasypacej polohe;
    // na lisovaciu polohu ju presunie až spoločný krok 130.
    //
    // Ktorý dávkovač má sypať, hovorí váham IL.ZonePress.VrstvaRequest.
    // ==========================================

    private int MainStep400(int step)
    {
        Message = "Multi-mix: nasypacia poloha (vrstva 1)";
        MotorStred.Operation.ProfilePositionMode.SetPositionProfile(
            ParametersLis.ParLisovanie.ProfilRychlyVelocity,
            ParametersLis.ParLisovanie.ProfilRychlyAcc,
            ParametersLis.ParLisovanie.ProfilRychlyDcc);
        MotorMaster.Operation.ProfilePositionMode.MoveToPositionGear(ParametersLis.ParLis.VyskaNasypacia, true, true);
        MotorStred.Operation.ProfilePositionMode.MoveToPositionGear(ParametersLis.ParKonzola.VyskaNasypacia, true,
            true);
        MotorStred.Operation.MotionInfo.WaitForTargetReached(5000);
        MotorMaster.Operation.MotionInfo.WaitForTargetReached(10000);

        IL.ZonePress.Release(EnZoneOwner.Press, EnZoneStatus.InputEmpty, 1);
        return 410;
    } //Presun do nasypacej polohy a vyziadanie 1. zmesi -> 410

    private int MainStep410(int step)
    {
        Message = "Multi-mix: čakám na 1. zmes";
        if (RequestToEnd) // ak je poziadavka na parkovanie parkujem
        {
            Log.Logger.ForContext("Name", Name).Information("Lis: Parkujem.");
            return 0;
        }

        if (IL.ZonePress.TryLock(EnZoneOwner.Press, EnZoneStatus.InputFull))
        {
            IL.ZonePress.Status = EnZoneStatus.OutputProced;

            // Prvá vrstva zakladá súčet hmotností - ďalšie sa už pripočítavajú.
            _aktualnaHmotnost = IL.ZonePress.PayloadHmotnost;
            ProduktLisLast.Copy(ProduktLisActual);
            ProduktLisActual.Clear();
            return 420;
        }

        return step;
    } //Cakanie na 1. zmes -> 420

    private int MainStep420(int step)
    {
        Message = "Multi-mix: zrovnanie 1. vrstvy";

        // Iba pohyb piesta prevádzkovým profilom - žiadna zmena rýchlosti ani kontrola sily.
        MotorMaster.Operation.ProfilePositionMode.MoveToPositionGear(
            ParametersLis.ParMultiMix.VyskaPritlacenia, true, true);
        MotorMaster.Operation.MotionInfo.WaitForTargetReached(10000);

        return 430;
    } //Zrovnanie 1. vrstvy -> 430

    private int MainStep430(int step)
    {
        Message = "Multi-mix: návrat do nasypacej polohy";

        // Vracia sa len piest; konzola ostáva tam, kde bola pri plnení.
        MotorMaster.Operation.ProfilePositionMode.MoveToPositionGear(ParametersLis.ParLis.VyskaNasypacia, true, true);
        MotorMaster.Operation.MotionInfo.WaitForTargetReached(10000);

        IL.ZonePress.Release(EnZoneOwner.Press, EnZoneStatus.InputEmpty, 2);
        return 440;
    } //Navrat do nasypacej polohy a vyziadanie 2. zmesi -> 440

    private int MainStep440(int step)
    {
        Message = "Multi-mix: čakám na 2. zmes";
        if (RequestToEnd) // ak je poziadavka na parkovanie parkujem
        {
            Log.Logger.ForContext("Name", Name).Information("Lis: Parkujem.");
            return 0;
        }

        if (IL.ZonePress.TryLock(EnZoneOwner.Press, EnZoneStatus.InputFull))
        {
            IL.ZonePress.Status = EnZoneStatus.OutputProced;
            _aktualnaHmotnost += IL.ZonePress.PayloadHmotnost;
            return 450;
        }

        return step;
    } //Cakanie na 2. zmes -> 450

    private int MainStep450(int step)
    {
        Message = "Multi-mix: uvoľnenie pre 3. zmes";

        // Bez pohybu - piest aj konzola sú už v nasypacej polohe.
        IL.ZonePress.Release(EnZoneOwner.Press, EnZoneStatus.InputEmpty, 3);
        return 460;
    } //Vyziadanie 3. zmesi -> 460

    private int MainStep460(int step)
    {
        Message = "Multi-mix: čakám na 3. zmes";
        if (RequestToEnd) // ak je poziadavka na parkovanie parkujem
        {
            Log.Logger.ForContext("Name", Name).Information("Lis: Parkujem.");
            return 0;
        }

        if (IL.ZonePress.TryLock(EnZoneOwner.Press, EnZoneStatus.InputFull))
        {
            IL.ZonePress.Status = EnZoneStatus.OutputProced;
            _aktualnaHmotnost += IL.ZonePress.PayloadHmotnost;

            Log.Logger.ForContext("Name", Name).Information(
                $"Multi-mix: dutina naplnená tromi vrstvami, spolu {_aktualnaHmotnost:F3} g.");
            return 120; // Dutina je plná, pokracuje spolocnym retazcom lisovania
        }

        return step;
    } //Cakanie na 3. zmes -> 120

    // Parametre lisu sú rozdelené do vrstiev Stroj / Forma / Výrobok, preto sa ukladajú
    // a načítavajú vždy všetky naraz cez CRecipeManager.
    [RelayCommand]
    public void SaveParameters()
    {
        Program.MainProgram?.RecipeManager.SaveAll();
    }

    [RelayCommand]
    public void LoadParameters()
    {
        Program.MainProgram?.RecipeManager.Reload();
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