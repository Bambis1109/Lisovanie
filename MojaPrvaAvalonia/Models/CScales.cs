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

public partial class CScales : CPlcScale
{
    public CDeviceScale Scale1 { get; set; }
    public CDeviceScale Scale2 { get; set; }

    public CScales(string name) : base(name)
    {
        LoadParameters();
  //   ToDo   ScaleViewModels.Add(new UcScaleViewModel(null, "SC1"));
   //  ToDo   ScaleViewModels.Add(new UcScaleViewModel(null, "SC2"));
      
    }

    public override async Task ConnectAsync()
    {
        await base.ConnectAsync();

        if (Connection == EnStatusConnection.Connected)
        {
           
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
        Message = "";

        
        return 20;
    } //

    private int InitStep20(int step)
    {
        Message = "";
        
        return 30;
    } //

    private int InitStep30(int step)
    {
        Message = "Homing Z a Jaws";
      
        return 40;
    } //

    private int InitStep40(int step)
    {
        Message = "Homing delta ramien";

      

        Log.Logger.ForContext("Name", Name).Debug($"Scales inizializovany.");

        return 99;
    } //

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
        Message = "Start";
        return 120;
    } // start cyklu

    private int MainStep120(int step)
    {
        Message = "";
       
        return 130;
    } // 

    private int MainStep130(int step)
    {
        Message = "";
       
        return 140;
    } // 

    private int MainStep140(int step)
    {
        Message = "";
       
        return 150;
    } //

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