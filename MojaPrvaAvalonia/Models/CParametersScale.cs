using CommunityToolkit.Mvvm.ComponentModel;

namespace MojaPrvaAvalonia.Models;

public partial class CParametersScale : ObservableObject
{
    [ObservableProperty] private int _nodeIdVaha1 = 1;
    [ObservableProperty] private int _nodeIdVaha2 = 2;
}
