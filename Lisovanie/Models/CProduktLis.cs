using CommunityToolkit.Mvvm.ComponentModel;
namespace Lisovanie.Models;

public partial class CProduktLis: ObservableObject
{
    [ObservableProperty] private double _sila;
    [ObservableProperty] private double _vyska;
    [ObservableProperty] private EnProduktLis _status;

    public void Clear()
    {
        Sila = 0;
        Vyska = 0;
        Status = EnProduktLis.Unknow;
    }

    public void Copy(CProduktLis other)
    {
        if (other == null) return;
        Sila = other.Sila;
        Vyska = other.Vyska;
        Status = other.Status;
    }
}

public enum EnProduktLis
{
    Unknow,
    Ok,
    Nok
}