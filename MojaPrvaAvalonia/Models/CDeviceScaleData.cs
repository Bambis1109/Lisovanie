using CommunityToolkit.Mvvm.ComponentModel;

namespace MojaPrvaAvalonia.Models;

public partial class CDeviceScaleData : ObservableObject
{
    [ObservableProperty] private string _scaleName = string.Empty;
    [ObservableProperty] private int _nodeId;
    [ObservableProperty] private int _weightFinal;
    [ObservableProperty] private int _weightRaw;
    [ObservableProperty] private int _weight32Actual;
    [ObservableProperty] private string _statusMainProc = string.Empty;
    [ObservableProperty] private string _statusMainMat = string.Empty;
}