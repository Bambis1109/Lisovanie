using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lisovanie.Models;

/// <summary>Výšky lisu závislé od výrobku. Kalibrácia je vo vrstve formy.</summary>
public class CRecipeLis
{
    public double VyskaNasypacia { get; set; }
    public double VyskaPriblizenie { get; set; }
    public double VyskaCistenia { get; set; }
}

/// <summary>Polohy konzoly (motor Stred).</summary>
public class CRecipeKonzola
{
    public double VyskaOdoberacia { get; set; }
    public double VyskaNasypacia { get; set; }
    public double VyskaLisovacia { get; set; }
    public double VyskaCistenia { get; set; }
    public int CyklovCistenia { get; set; }
}

/// <summary>Požadované a medzné hodnoty výlisku.</summary>
public class CRecipeVyrobok
{
    public string Name { get; set; } = string.Empty;
    public double VyskaMax { get; set; }
    public double VyskaMin { get; set; }
    public double VyskaPozadovana { get; set; }
    public double SilaMax { get; set; }
    public double SilaMin { get; set; }
    public double SilaPozadovana { get; set; }
}

/// <summary>Kontrolné medze hmotnosti dávky a profil dávkovania pre váhy.</summary>
public class CRecipeVaha
{
    public double VahaPozadovana { get; set; }
    public double VahaMax { get; set; }
    public double VahaMin { get; set; }

    /// <summary>
    /// Parametre riadenia dávky (SDO 0x6006) posielané do všetkých aktívnych váh pri Inite.
    /// Kľúče zodpovedajú menám property v DeviceParameters, takže formát je zhodný
    /// s exportom z okna parametrov dávky. Prázdny slovník = profil ešte nebol nastavený.
    /// </summary>
    public Dictionary<string, int> Davka { get; set; } = new();
}

/// <summary>Vykladacia matica - rozteče závisia od priemeru výlisku.</summary>
public class CRecipeMatrix
{
    public int Xfirst { get; set; }
    public int Yfirst { get; set; }
    public int Xdelta { get; set; }
    public int Ydelta { get; set; }
    public int Xnum { get; set; }
    public int Ynum { get; set; }
}

/// <summary>
/// Dráhy manipulátora. Východiskové hodnoty sú pôvodné konštanty z krokov PLC,
/// aby recept bez tejto sekcie správal stroj rovnako ako pred etapou 2.
/// </summary>
public class CRecipeManipulator
{
    public double PolarParkovacia { get; set; } = 140;
    public double PolarVychodiskova { get; set; } = 160;
    public double PolarZasunuta { get; set; } = 210;
    public double PolarULisu { get; set; } = 255;

    public double ZHorna { get; set; } = -9;
    public double ZNadVyrobkom { get; set; } = -13;
    public double ZVylozenie { get; set; } = -35;

    public double CelusteOtvorene { get; set; } = 5;
    public double CelusteVysyp { get; set; } = -2;
    public double CelusteUchopStred { get; set; } = -6.7;
    public double CelusteUchopSila { get; set; } = -30;
    public double CelusteUchopTolerancia { get; set; } = 1;
    public int CelusteUchopTimeout { get; set; } = 2000;
}

/// <summary>
/// Priebeh lisovania. Východiskové hodnoty sú pôvodné konštanty z krokov PLC.
/// </summary>
public class CRecipeLisovanie
{
    public double StredVychodzia { get; set; } = -21;

    public uint ProfilRychlyVelocity { get; set; } = 300;
    public uint ProfilRychlyAcc { get; set; } = 5000;
    public uint ProfilRychlyDcc { get; set; } = 5000;

    public uint ProfilPomalyVelocity { get; set; } = 80;
    public uint ProfilPomalyAcc { get; set; } = 2000;
    public uint ProfilPomalyDcc { get; set; } = 2000;

    public long DobaDrzaniaMs { get; set; } = 2000;

    public double KrokPritlakuHruby { get; set; } = -0.5;
    public double KrokPritlakuStredny { get; set; } = -0.2;
    public double KrokPritlakuJemny { get; set; } = -0.02;

    public double PrahStredny { get; set; } = 300;
    public double PrahJemny { get; set; } = 100;

    public double KrokUdrziavania { get; set; } = -0.01;
}

/// <summary>Ktoré váhy sa v tomto recepte používajú.</summary>
public class CRecipeVahy
{
    public bool EnabledVaha1 { get; set; } = true;
    public bool EnabledVaha2 { get; set; } = true;
    public bool EnabledVaha3 { get; set; } = true;
}

/// <summary>
/// Vrstva výrobku - všetko, čo sa mení s vyrábaným kusom. Odkazuje na formu,
/// ktorej kalibráciu použije. Jeden súbor v Parameters/Recipes.
/// </summary>
public class CRecipe
{
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Názov výrobku, zhodný s názvom súboru v Parameters/Recipes.</summary>
    public string Name { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EnModeVyroby Mode { get; set; } = EnModeVyroby.Single;

    /// <summary>Názov formy v Parameters/Forms, ktorej kalibráciu recept používa.</summary>
    public string Form { get; set; } = string.Empty;

    public CRecipeLis Lis { get; set; } = new();
    public CRecipeKonzola Konzola { get; set; } = new();
    public CRecipeVyrobok Vyrobok { get; set; } = new();
    public CRecipeVaha Vaha { get; set; } = new();
    public CRecipeMatrix MatrixOk { get; set; } = new();
    public CRecipeMatrix MatrixNok { get; set; } = new();
    public CRecipeVahy Vahy { get; set; } = new();
    public CRecipeManipulator Manipulator { get; set; } = new();
    public CRecipeLisovanie Lisovanie { get; set; } = new();
}
