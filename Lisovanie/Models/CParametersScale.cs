using CommunityToolkit.Mvvm.ComponentModel;

namespace Lisovanie.Models;

public partial class CParametersScale : ObservableObject
{
    [ObservableProperty] private int _nodeIdVaha1 = 1;
    [ObservableProperty] private int _nodeIdVaha2 = 2;
    [ObservableProperty] private int _nodeIdVaha3 = 3;

    [ObservableProperty] private bool _enabledVaha1 = true;
    [ObservableProperty] private bool _enabledVaha2 = true;
    [ObservableProperty] private bool _enabledVaha3 = true;
}
