namespace Lisovanie.Models;

/// <summary>
/// Vrstva stroja - hodnoty viazané na konkrétny stroj, nie na formu ani výrobok.
/// Existuje práve jedna inštancia (Parameters/Machine.json) a recept ju nikdy neprepíše.
/// </summary>
public class CParametersMachine
{
    public int SchemaVersion { get; set; } = 1;

    // --- Kalibrácia delta ramena (výsledok Kalibruj / Orientuj) ---
    public int OffsetArm { get; set; }
    public int OffsetSystem { get; set; }
    public int RawLH { get; set; }
    public int RawLD { get; set; }

    // Tieto dve prepisuje InitStep10 pri každom Inite. Ukladajú sa len preto,
    // aby sa zachovalo pôvodné správanie ParametersDelta.json.
    public int EposLH { get; set; }
    public int EposLD { get; set; }

    // --- CAN zbernica a identifikátory uzlov ---
    public int CanLine { get; set; }
    public int BoardLine { get; set; }
    public int IDVaha1 { get; set; }
    public int IDVaha2 { get; set; }
    public int IDVaha3 { get; set; }
    public int IDBox { get; set; }

    public int NodeIdVaha1 { get; set; }
    public int NodeIdVaha2 { get; set; }
    public int NodeIdVaha3 { get; set; }
}
