using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EposCmd.Net;
using EposCmd.Net.DeviceScaleSet;
using MojaPrvaAvalonia.ViewModels;
using Serilog;

namespace MojaPrvaAvalonia.Models;

public partial class CControlScales : CPlcScale
{
    public CDeviceScale Scale1 { get; set; }
    public CDeviceScale Scale2 { get; set; }

    public CControlScales(string name) : base(name)
    {
        LoadParameters();
        ScaleViewModels.Add(new UcDeviceScaleViewModel(this, null, "SC1"));
        ScaleViewModels.Add(new UcDeviceScaleViewModel(this, null, "SC2"));
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
            case 121: return MainStep121(step);
            case 130: return MainStep130(step);
            case 140: return MainStep140(step);
            case 150: return MainStep150(step);

            case 160: return MainStep160(step);

            default: return base.RunStep(step);
        }
    }

    // ==========================================
    // METÓDY PRE INIT
    // ==========================================
    private int InitStep1(int step)
    {
        Message = "";

        return 10;
    } // Init

    private int InitStep10(int step)
    {
        Message = "Štart inicializácie váh...";

        // Súčasné odoslanie povelov pre obe váhy (paralelný štart)
        Scale1.Operation.Master.SendCommand(EMasterCommand.Init);
        Scale2.Operation.Master.SendCommand(EMasterCommand.Init);

        // Spustíme stopky - budeme strážiť, či váhy do 3 sekúnd skočia do Busy
        _initStopwatch = System.Diagnostics.Stopwatch.StartNew();

        return 20;
    }

    private int InitStep20(int step)
    {
        Message = "Čakám na potvrdenie štartu (Busy)...";

        // Bezpečné vyčítanie stavov cez Pattern Matching (operátor 'is')
        bool s1Busy = Scale1.Data is CDataScale d1 && d1.StatusMainProc == EProcStatus.Busy;
        bool s2Busy = Scale2.Data is CDataScale d2 && d2.StatusMainProc == EProcStatus.Busy;

        bool s1Error = Scale1.Data is CDataScale e1 && e1.StatusMainProc == EProcStatus.Error;
        bool s2Error = Scale2.Data is CDataScale e2 && e2.StatusMainProc == EProcStatus.Error;

        // KROK A: Obe váhy úspešne prepli do Busy (trvá to min. 5s, zachytili sme to)
        if (s1Busy && s2Busy)
        {
            // Resetujeme stopky, teraz budeme merať čas samotnej inicializácie v kroku 30
            _initStopwatch = System.Diagnostics.Stopwatch.StartNew();
            return 30;
        }

        // KROK B: Ochrana proti zamrznutiu (Timeout 3s) alebo okamžitá chyba na zbernici
        if (s1Error || s2Error || _initStopwatch.ElapsedMilliseconds > 3000)
        {
            Log.Logger.ForContext("Name", Name).Error(
                $"Inicializácia zlyhala v kroku 20 (Čakanie na Busy). " +
                $"S1_Busy: {s1Busy}, S2_Busy: {s2Busy}, S1_Err: {s1Error}, S2_Err: {s2Error}, Čas: {_initStopwatch.ElapsedMilliseconds}ms");

            Message = "Chyba: Váhy nepotvrdili štart (Busy).";
            _initStopwatch.Stop();
            return 0; // Bezpečný výskok do kroku 0 (krokomat nenastaví stav Ready)
        }

        return step; // Obe ešte nie sú Busy a čas nevypršal -> zostaň tu (slučka počká 10ms)
    }

    private int InitStep30(int step)
    {
        Message = "Prebieha inicializácia (5-10s)...";

        bool s1Ready = Scale1.Data is CDataScale d1 && d1.StatusMainProc == EProcStatus.Ready;
        bool s2Ready = Scale2.Data is CDataScale d2 && d2.StatusMainProc == EProcStatus.Ready;

        bool s1Error = Scale1.Data is CDataScale e1 && e1.StatusMainProc == EProcStatus.Error;
        bool s2Error = Scale2.Data is CDataScale e2 && e2.StatusMainProc == EProcStatus.Error;

        // KROK A: Obe váhy úspešne dokončili inicializáciu a sú Ready
        if (s1Ready && s2Ready)
        {
            _initStopwatch.Stop();
            return 40;
        }

        // KROK B: Kontrola chýb alebo timeoutu (fáza trvá 5-10s, dáme rezervu 15 sekúnd)
        if (s1Error || s2Error || _initStopwatch.ElapsedMilliseconds > 15000)
        {
            Log.Logger.ForContext("Name", Name).Error(
                $"Inicializácia zlyhala v kroku 30 (Čakanie na Ready). " +
                $"S1_Ready: {s1Ready}, S2_Ready: {s2Ready}, S1_Err: {s1Error}, S2_Err: {s2Error}, Čas: {_initStopwatch.ElapsedMilliseconds}ms");

            Message = "Chyba: Inicializácia váh zlyhala.";
            _initStopwatch.Stop();
            return 0; // Bezpečný výskok do kroku 0
        }

        return step; // Váhy stále pracujú, čakáme ďalej
    }

    private int InitStep40(int step)
    {
        Message = "Inicializácia úspešná";
        Log.Logger.ForContext("Name", Name).Information("Obe váhy boli úspešne inicializované.");

        return 99; // Skočí do finálneho kroku, kde CPlc nastaví stav EnStatusPlc.Ready
    }

    // ==========================================
    // METÓDY PRE MAIN PROGRAM (Prepojené na reálne scale)
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
        Message = "Main 100: Kontrola parkovania";
        StatusCycle = EnStatusCycle.Moving;

        if (RequestToEnd)
        {
            return 0;
        }

        return 120;
    }

    private int MainStep120(int step)
    {
        Message = "Štart dávkovania (Doser)";

        // 1. Odošleme povel na štart dávkovania (SDO)
        StartDoserProduction(Scale1);

        // 2. Okamžite prejdeme do ďalšieho kroku, kde budeme čakať na reakciu STM32
        return 121;
    }

    private int MainStep121(int step)
    {
        Message = "Čakám na potvrdenie štartu od STM32...";
        StatusCycle = EnStatusCycle.WaitForStep;

        // Čakáme, kým STM32 nezmení stav v TPDO na Busy
        //   if (Scale1.Data.StatusDoserProc == EProcStatus.Busy)
        {
            // Voliteľné: Ak STM32 vyžaduje zhodenie povelu na 0 (Clear) po prijatí
            ClearDoserCommand(Scale1);
            return 130;
        }

        //    if (Scale1.Data.StatusDoserProc == EProcStatus.Error)
        {
            throw new Exception("Chyba dávkovača pri štarte!");
        }

        return step; // Zostaň v tomto kroku (cyklus sa opakuje každých 10ms)
    }

    private int MainStep130(int step)
    {
        Message = "Dávkovanie prebieha (10-20s)...";
        StatusCycle = EnStatusCycle.Inspecting;
/*
        // Čakáme, kým STM32 nedokončí prácu (Ready) a materiál nie je pripravený (Full)
        if (Scale1.Data.StatusDoserProc == EProcStatus.Ready)
        {
            if (Scale1.Data.StatusDoserMat == EMatStatus.Full)
            {
                Log.Logger.ForContext("Name", Name).Information($"Dávka pripravená. Hmotnosť: {Scale1.Data.WeightFinal / 1000.0} kg");
                return 140; // Prejdi na vykladanie
            }
            else
            {
                // Ak je Ready, ale nie je Full, došiel materiál alebo nastal iný logický problém
                throw new Exception("Dávkovač skončil, ale materiál nie je Full!");
            }
        }
*/
        return step; // Zostaň v tomto kroku, kým sa dávkuje
    }

    private int MainStep140(int step)
    {
        Message = "Čakám na Lis...";

        // Tu komunikuješ s CControlLis (napr. cez MainProgram)
        // Ak je lis pripravený prijať dávku:
        // return 150;

        return step;
    }

    private int MainStep150(int step)
    {
        Message = "";

        return 160;
    } //

    private int MainStep160(int step)
    {
        Message = "";


        return 0;
    } //


    [RelayCommand]
    public void SaveParameters()
    {
        //ToDo  SaveParametersToFile("ParametersScale.json", ParametersScale);
    }

    [RelayCommand]
    public void LoadParameters()
    {
        //ToDo    LoadParametersFromFile("ParametersScale.json", ParametersScale);

        // NABINDOVANIE NA AI (Aktualizácia kinematiky)
    }
}