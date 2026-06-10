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
}
