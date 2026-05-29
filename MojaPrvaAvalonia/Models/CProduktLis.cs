using CommunityToolkit.Mvvm.ComponentModel;
namespace MojaPrvaAvalonia.Models;

public class CProduktLis: ObservableObject
{
    public double Sila { get; set; }
    public double Vyska { get; set; }
    public EnProduktLis Status { get; set; }

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