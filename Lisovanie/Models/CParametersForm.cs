namespace Lisovanie.Models;

/// <summary>
/// Vrstva formy - výhradne kalibrácia sily a výšky lisu.
/// Vzniká meraním a počas života nástroja sa mení, preto ju zdieľajú všetky výrobky
/// lisované v tej istej forme (napr. ZazihacSingle a ZazihacMulti v 24 mm forme).
/// </summary>
public class CParametersForm
{
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Názov formy, zhodný s názvom súboru v Parameters/Forms.</summary>
    public string Name { get; set; } = string.Empty;

    public double VyskaKalibra { get; set; }
    public double VyskaSenAbsolut1 { get; set; }
    public double VyskaSenAbsolut2 { get; set; }
    public double SilaKalib1 { get; set; }
    public double SilaKalib2 { get; set; }
    public double VyskaSenPulz { get; set; }

    // Výsledok RecalculateCalibration()
    public double SmernicaK { get; set; }
    public double KonstantaB { get; set; }
}
