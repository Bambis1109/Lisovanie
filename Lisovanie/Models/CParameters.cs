using System;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Lisovanie.Models;

public partial class CParameters : ObservableObject
{
    [ObservableProperty]
    private int _rawLH;

    [ObservableProperty]
    private int _rawLD;
 
    [ObservableProperty]
    private int _offsetArm = 1940;

    [ObservableProperty]
    private int _offsetSystem = -109995;
    
    [ObservableProperty]
    private int _eposLH;

    [ObservableProperty]
    private int _eposLD;

    // --- Vykladacia matica OK (default = pôvodné natvrdo zadané hodnoty z InitStep1) ---
    [ObservableProperty] private int _matrixOkXfirst = -306;
    [ObservableProperty] private int _matrixOkYfirst = -68;
    [ObservableProperty] private int _matrixOkXdelta = 21;
    [ObservableProperty] private int _matrixOkYdelta = 19;
    [ObservableProperty] private int _matrixOkXnum = 6;
    [ObservableProperty] private int _matrixOkYnum = 7;

    // --- Vykladacia matica NOK (default = pôvodné natvrdo zadané hodnoty z InitStep1) ---
    [ObservableProperty] private int _matrixNokXfirst = 165;
    [ObservableProperty] private int _matrixNokYfirst = -68;
    [ObservableProperty] private int _matrixNokXdelta = 21;
    [ObservableProperty] private int _matrixNokYdelta = 19;
    [ObservableProperty] private int _matrixNokXnum = 3;
    [ObservableProperty] private int _matrixNokYnum = 2;
}

/// <summary>
/// Dráhy manipulátora závislé od výrobku. Východiskové hodnoty sú presne tie,
/// ktoré boli do etapy 2 zadané natvrdo v krokoch CControlManipulator.
/// </summary>
public partial class CParManipulator : ObservableObject
{
    // --- Polohy ramena (polárne, uhol 0) ---
    [ObservableProperty] private double _polarParkovacia = 140;
    [ObservableProperty] private double _polarVychodiskova = 160;
    [ObservableProperty] private double _polarZasunuta = 210;
    [ObservableProperty] private double _polarULisu = 255;

    // --- Výšky osi Z ---
    [ObservableProperty] private double _zHorna = -9;
    [ObservableProperty] private double _zNadVyrobkom = -13;
    [ObservableProperty] private double _zVylozenie = -35;

    // --- Čeľuste ---
    [ObservableProperty] private double _celusteOtvorene = 5;
    [ObservableProperty] private double _celusteVysyp = -2;
    [ObservableProperty] private double _celusteUchopStred = -6.7;
    [ObservableProperty] private double _celusteUchopSila = -30;
    [ObservableProperty] private double _celusteUchopTolerancia = 1;
    [ObservableProperty] private int _celusteUchopTimeout = 2000;

    /// <summary>
    /// Názov výrobku - iba kontext do logu pri kontrole uchopenia.
    /// Nastavuje ho CRecipeManager z receptu, do súboru sa neukladá.
    /// </summary>
    [ObservableProperty] private string _vyrobokName = string.Empty;
}
