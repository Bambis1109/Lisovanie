using System;
using System.ComponentModel;
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

    // --- Vykladacia matica OK (default = pôvodné natvrdo zadané hodnoty z InitStep1) ---
    [ObservableProperty] private int _matrixOkXfirst = -306;
    [ObservableProperty] private int _matrixOkYfirst = -68;
    [ObservableProperty] private int _matrixOkXdelta = 21;
    [ObservableProperty] private int _matrixOkYdelta = 19;
    [ObservableProperty] private int _matrixOkXnum = 6;
    [ObservableProperty] private int _matrixOkYnum = 7;

    // --- Vykladacia matica NOK (default = pôvodné natvrdo zadané hodnoty z InitStep1) ---
    [ObservableProperty] private int _matrixNokXfirst = 165;
    [ObservableProperty] private int _matrixNokYfirst = -68;
    [ObservableProperty] private int _matrixNokXdelta = 21;
    [ObservableProperty] private int _matrixNokYdelta = 19;
    [ObservableProperty] private int _matrixNokXnum = 3;
    [ObservableProperty] private int _matrixNokYnum = 2;

    /// <summary>
    /// Rozloženie oboch vykladacích matíc. Zámerne jedna hodnota pre OK aj NOK -
    /// obe matice ukladajú ten istý výrobok, takže rozteč aj vzor sú spoločné.
    /// </summary>
    [ObservableProperty] private EnRozlozenieMatice _rozlozenieMatice = EnRozlozenieMatice.DIA;

    /// <summary>Zoznam rozložení pre ComboBox v okne nastavení.</summary>
    public static EnRozlozenieMatice[] RozlozeniaMatice { get; } = Enum.GetValues<EnRozlozenieMatice>();
}

/// <summary>
/// Dráhy manipulátora závislé od výrobku. Východiskové hodnoty sú presne tie,
/// ktoré boli do etapy 2 zadané natvrdo v krokoch CControlManipulator.
/// </summary>
public partial class CParManipulator : ObservableObject
{
    // --- Polohy ramena (polárne, uhol 0) ---
    [ObservableProperty]
    [property: DisplayName("PolarParkovacia")]
    [property: Description("Parkovacia poloha ramena (polárna súradnica) mimo pracovného priestoru. (Predvolené: 140)")]
    private double _polarParkovacia = 140;

    [ObservableProperty]
    [property: DisplayName("PolarVychodiskova")]
    [property: Description("Východisková poloha ramena pred zasunutím k lisu. (Predvolené: 160)")]
    private double _polarVychodiskova = 160;

    [ObservableProperty]
    [property: DisplayName("PolarZasunuta")]
    [property: Description("Poloha ramena zasunutého do pracovného priestoru. (Predvolené: 210)")]
    private double _polarZasunuta = 210;

    [ObservableProperty]
    [property: DisplayName("PolarULisu")]
    [property: Description("Poloha ramena pri lise, nad výliskom. (Predvolené: 255)")]
    private double _polarULisu = 255;

    // --- Výšky osi Z ---
    [ObservableProperty]
    [property: DisplayName("ZHorna")]
    [property: Description("Horná (prejazdová) výška osi Z. (Predvolené: -9)")]
    private double _zHorna = -9;

    [ObservableProperty]
    [property: DisplayName("ZNadVyrobkom")]
    [property: Description("Výška osi Z tesne nad výrobkom pred uchopením. (Predvolené: -13)")]
    private double _zNadVyrobkom = -13;

    [ObservableProperty]
    [property: DisplayName("ZVylozenie")]
    [property: Description("Výška osi Z pri vykladaní výrobku do matice. (Predvolené: -35)")]
    private double _zVylozenie = -35;

    // --- Čeľuste ---
    [ObservableProperty]
    [property: DisplayName("CelusteOtvorene")]
    [property: Description("Poloha naplno otvorených čeľustí. (Predvolené: 5)")]
    private double _celusteOtvorene = 5;

    [ObservableProperty]
    [property: DisplayName("CelusteVysyp")]
    [property: Description("Poloha čeľustí pri vyložení výrobku do matice (krok 240). (Predvolené: -2)")]
    private double _celusteVysyp = -2;

    [ObservableProperty]
    [property: DisplayName("CelusteUchopStred")]
    [property: Description("Stredová poloha čeľustí pri uchopení výrobku. (Predvolené: -6.7)")]
    private double _celusteUchopStred = -6.7;

    [ObservableProperty]
    [property: DisplayName("CelusteUchopSila")]
    [property: Description("Sila (prúd) zovretia čeľustí pri uchopení. (Predvolené: -30)")]
    private double _celusteUchopSila = -30;

    [ObservableProperty]
    [property: DisplayName("CelusteUchopTolerancia")]
    [property: Description("Povolená odchýlka polohy pri kontrole uchopenia. (Predvolené: 1)")]
    private double _celusteUchopTolerancia = 1;

    [ObservableProperty]
    [property: DisplayName("CelusteUchopTimeout")]
    [property: Description("Časový limit uchopenia v ms. (Predvolené: 2000)")]
    private int _celusteUchopTimeout = 2000;

    /// <summary>
    /// Názov výrobku - iba kontext do logu pri kontrole uchopenia.
    /// Nastavuje ho CRecipeManager z receptu, do súboru sa neukladá.
    /// </summary>
    [ObservableProperty] private string _vyrobokName = string.Empty;
}
