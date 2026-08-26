using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Lisovanie.Models;

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

/// <summary>
/// Priebeh lisovania a pohyby konzoly. Východiskové hodnoty sú presne tie,
/// ktoré boli do etapy 2 zadané natvrdo v krokoch CControlLis.
/// </summary>
public partial class CParLisovanie : ObservableObject
{
    // --- Konzola (motor Stred) ---
    [ObservableProperty] private double _stredVychodzia = -21;

    [ObservableProperty] private uint _profilRychlyVelocity = 300;
    [ObservableProperty] private uint _profilRychlyAcc = 5000;
    [ObservableProperty] private uint _profilRychlyDcc = 5000;

    [ObservableProperty] private uint _profilPomalyVelocity = 80;
    [ObservableProperty] private uint _profilPomalyAcc = 2000;
    [ObservableProperty] private uint _profilPomalyDcc = 2000;

    // --- Silová regulácia ---
    /// <summary>Ako dlho sa musí udržať požadovaná sila, aby bol výlisok OK [ms].</summary>
    [ObservableProperty] private long _dobaDrzaniaMs = 2000;

    [ObservableProperty] private double _krokPritlakuHruby = -0.5;
    [ObservableProperty] private double _krokPritlakuStredny = -0.2;
    [ObservableProperty] private double _krokPritlakuJemny = -0.02;

    /// <summary>Odstup od požadovanej sily, pri ktorom sa prejde na stredný krok prítlaku.</summary>
    [ObservableProperty] private double _prahStredny = 300;

    /// <summary>Odstup od požadovanej sily, pri ktorom sa prejde na jemný krok prítlaku.</summary>
    [ObservableProperty] private double _prahJemny = 100;

    [ObservableProperty] private double _krokUdrziavania = -0.01;
}

/// <summary>
/// Priebeh lisovania na vzdialenosť. Cieľom je ParVyrobok.VyskaPozadovana,
/// stropom ParVyrobok.SilaMax, výsledok sa klasifikuje pásmom VyskaMin..VyskaMax.
/// </summary>
public partial class CParLisovanieVzdialenost : ObservableObject
{
    [ObservableProperty] private double _krokPritlakuHruby = -0.5;
    [ObservableProperty] private double _krokPritlakuStredny = -0.2;
    [ObservableProperty] private double _krokPritlakuJemny = -0.02;

    /// <summary>Odstup od cieľovej hrúbky [mm], pri ktorom sa prejde na stredný krok prítlaku.</summary>
    [ObservableProperty] private double _prahStredny = 3.0;

    /// <summary>Odstup od cieľovej hrúbky [mm], pri ktorom sa prejde na jemný krok prítlaku.</summary>
    [ObservableProperty] private double _prahJemny = 1.0;

    /// <summary>Ako dlho sa má držať dosiahnutá hrúbka [ms].</summary>
    [ObservableProperty] private long _dobaDrzaniaMs = 2000;

    /// <summary>
    /// Pauza po každom prítlačnom kroku [ms]. Vzdialenosť sa obnovuje raz za 100 ms,
    /// takže pri hodnote 10 pripadnú na jedno obnovenie ~3 kroky naslepo.
    /// Hodnota 80 dá presne jeden krok na jedno obnovenie údaja.
    /// </summary>
    [ObservableProperty] private int _pauzaKrokuMs = 10;
}

/// <summary>
/// Zrovnanie prvej vrstvy v multi-mix režime. Piest zíde na absolútnu polohu a zrovná
/// kopec, ktorý vznikol nasypom, do roviny. Konzola sa počas plnenia nehýbe.
/// </summary>
public partial class CParMultiMix : ObservableObject
{
    /// <summary>Absolútna poloha piesta pri zrovnávaní 1. vrstvy [mm].</summary>
    [ObservableProperty] private double _vyskaPritlacenia = -120;

    /// <summary>Bezpečnostný strop sily pri zrovnávaní [N]. Prekročenie označí výlisok ako Nok.</summary>
    [ObservableProperty] private double _silaMaxPritlacenia = 2000;

    // Profil pohybu piesta pri zrovnávaní - zámerne pomalší než ProfilRychly.
    [ObservableProperty] private uint _profilVelocity = 80;
    [ObservableProperty] private uint _profilAcc = 2000;
    [ObservableProperty] private uint _profilDcc = 2000;
}

public partial class CParametersLis : ObservableObject
{
    [ObservableProperty] private CParLis _parLis = new();
    [ObservableProperty] private CParKonzola _parKonzola = new();
    [ObservableProperty] private CParVyrobok _parVyrobok = new();
    [ObservableProperty] private CParVaha _parVaha = new();
    [ObservableProperty] private CParLisovanie _parLisovanie = new();
    [ObservableProperty] private CParLisovanieVzdialenost _parLisovanieVzdialenost = new();
    [ObservableProperty] private CParMultiMix _parMultiMix = new();

    /// <summary>Metóda lisovania z receptu. Rozhoduje o vetvení v kroku 135.</summary>
    [ObservableProperty] private EnMetodaLisovania _metoda = EnMetodaLisovania.Sila;

    /// <summary>Režim výroby z receptu. Rozhoduje o vetvení v kroku 102.</summary>
    [ObservableProperty] private EnModeVyroby _mode = EnModeVyroby.Single;

    /// <summary>Zoznam metód pre ComboBox v okne nastavení.</summary>
    public static EnMetodaLisovania[] MetodyLisovania { get; } = Enum.GetValues<EnMetodaLisovania>();

    /// <summary>Zoznam režimov výroby pre ComboBox v okne nastavení.</summary>
    public static EnModeVyroby[] ModyVyroby { get; } = Enum.GetValues<EnModeVyroby>();

    [ObservableProperty] private int _canLine = 0;
    [ObservableProperty] private int _boardLine = 0;
    [ObservableProperty] private int _iDVaha1 = 4;
    [ObservableProperty] private int _iDVaha2 = 7;
    [ObservableProperty] private int _iDVaha3 = 8;
    [ObservableProperty] private int _iDBox = 10;
}
