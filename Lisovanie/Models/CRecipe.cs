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

/// <summary>Kontrolné medze hmotnosti dávky.</summary>
public class CRecipeVaha
{
    public double VahaPozadovana { get; set; }
    public double VahaMax { get; set; }
    public double VahaMin { get; set; }
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
}
