using CommunityToolkit.Mvvm.ComponentModel;

namespace MojaPrvaAvalonia.Models;

public partial class CMotorData : ObservableObject
{
    // Nová property pre názov motora
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private int _speed;

    [ObservableProperty]
    private int _position;

    [ObservableProperty]
    private int _current;
}