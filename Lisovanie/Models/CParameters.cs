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
