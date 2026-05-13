using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MojaPrvaAvalonia.Models;

public partial class CParLis : ObservableObject
{
    [ObservableProperty] private double _vyskaNasypacia = -50;
    [ObservableProperty] private double _vyskaPriblizenie = -140;
    [ObservableProperty] private double _vyskaKalibra = 12.08;
    [ObservableProperty] private double _vyskaSenAbsolut1 = 20.326;
    [ObservableProperty] private double _vyskaSenAbsolut2 = 20.028;
    [ObservableProperty] private double _silaKalib1 = 1000;
    [ObservableProperty] private double _silaKalib2 = 5000;
    [ObservableProperty] private double _vyskaSenPulz = 200;
    [ObservableProperty] private double _vyskaCistenia = 0;
    [ObservableProperty] private double _smernicaK = 7.4500000000000456E-05;
    [ObservableProperty] private double _konstantaB = -8.3205000000000009;

    public void RecalculateCalibration()
    {
        double y1 = VyskaKalibra - VyskaSenAbsolut1;
        double y2 = VyskaKalibra - VyskaSenAbsolut2;

        double dx = SilaKalib2 - SilaKalib1;
        if (Math.Abs(dx) < 1e-12)
            throw new ArgumentException("SilaKalib1 a SilaKalib2 musia byť rôzne.");

        SmernicaK = (y2 - y1) / dx;
        KonstantaB = y1 - SmernicaK * SilaKalib1;
    }

    public double PredictElongation(double sila)
    {
        return SmernicaK * sila + KonstantaB;
    }

    public double RecomputedDistance(double sila, double senAbsolut)
    {
        double y = PredictElongation(sila);
        double s0 = senAbsolut + y;
        return Math.Round(s0, 2);
    }
}

public partial class CParKonzola : ObservableObject
{
    [ObservableProperty] private double _vyskaOdoberacia = -18;
    [ObservableProperty] private double _vyskaNasypacia = -48;
    [ObservableProperty] private double _vyskaLisovacia = -30;
    [ObservableProperty] private double _vyskaCistenia = -70;
    [ObservableProperty] private int _cyklovCistenia = 9;
}

public partial class CParVyrobok : ObservableObject
{
    [ObservableProperty] private string _name = "KV4";
    [ObservableProperty] private double _vyskaMax = 13.5;
    [ObservableProperty] private double _vyskaMin = 11;
    [ObservableProperty] private double _vyskaPozadovana = 13;
    [ObservableProperty] private double _silaMax = 4800;
    [ObservableProperty] private double _silaMin = 4300;
    [ObservableProperty] private double _silaPozadovana = 3800;
}

public partial class CParVaha : ObservableObject
{
    [ObservableProperty] private double _vahaPozadovana = 5.5;
    [ObservableProperty] private double _vahaMax = 5.55;
    [ObservableProperty] private double _vahaMin = 5.45;
}

public partial class CParametersLis : ObservableObject
{
    [ObservableProperty] private CParLis _parLis = new();
    [ObservableProperty] private CParKonzola _parKonzola = new();
    [ObservableProperty] private CParVyrobok _parVyrobok = new();
    [ObservableProperty] private CParVaha _parVaha = new();

    [ObservableProperty] private int _canLine = 0;
    [ObservableProperty] private int _boardLine = 0;
    [ObservableProperty] private int _iDVaha1 = 4;
    [ObservableProperty] private int _iDVaha2 = 7;
    [ObservableProperty] private int _iDVaha3 = 8;
    [ObservableProperty] private int _iDBox = 10;
}
