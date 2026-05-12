using System;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MojaPrvaAvalonia.Models;

public partial class CParametersLis : ObservableObject
{
    [ObservableProperty]
    private int _rawLH;

    [ObservableProperty]
    private int _rawLD;
 
    [ObservableProperty]
    private int _offsetArm;

    [ObservableProperty]
    private int _offsetSystem;
    
    [ObservableProperty]
    private int _eposLH;

    [ObservableProperty]
    private int _eposLD;
}
