using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;

namespace Lisovanie.Models;

/// <summary>
/// Spravuje tri vrstvy parametrov - Stroj, Forma, Výrobok - a ich prenos
/// do bežiacich objektov (CParameters, CParametersLis, CParametersScale).
///
/// Bežiace objekty sa zámerne nemenia, aby ostali platné všetky existujúce väzby v XAML.
/// Mapovanie je písané priradenie po priradení, bez reflexie - pri riadení stroja je
/// dôležitejšie, aby sa dalo prečítať a overiť očami.
/// </summary>
public class CRecipeManager
{
    /// <summary>Názvy použité pri migrácii pôvodných súborov na vrstvenú štruktúru.</summary>
    public const string MigrationRecipeName = "KV4";
    public const string MigrationFormName = "Forma18";

    /// <summary>Aktuálna verzia schémy receptu. Staršie sa pri načítaní povýšia.</summary>
    private const int CurrentRecipeVersion = 2;

    /// <summary>
    /// Krok 130 lisu posielal konzolu natvrdo na -40, kým parameter ParKonzola.VyskaLisovacia
    /// mal nepoužívanú hodnotu -30. Od verzie 2 krok číta parameter, takže mu treba nastaviť
    /// skutočne používanú hodnotu - inak by sa zmenilo správanie stroja.
    /// </summary>
    private const double PouzivanaVyskaLisovacia = -40;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly CMainProgram _main;

    public CParametersMachine Machine { get; private set; } = new();
    public CParametersForm Form { get; private set; } = new();
    public CRecipe Recipe { get; private set; } = new();

    /// <summary>Názov práve načítaného receptu. Prázdny, kým sa nezavolá Apply.</summary>
    public string ActiveRecipeName { get; private set; } = string.Empty;

    public CRecipeManager(CMainProgram main)
    {
        _main = main;
    }

    // ==========================================
    // CESTY
    // ==========================================

    public static string ParametersDir =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Parameters");

    public static string MachinePath => Path.Combine(ParametersDir, "Machine.json");
    public static string FormsDir => Path.Combine(ParametersDir, "Forms");
    public static string RecipesDir => Path.Combine(ParametersDir, "Recipes");

    public static string FormPath(string name) => Path.Combine(FormsDir, name + ".json");
    public static string RecipePath(string name) => Path.Combine(RecipesDir, name + ".json");

    // ==========================================
    // PRÍSTUP K BEŽIACIM OBJEKTOM
    // ==========================================

    private CControlManipulator Manipulator => (CControlManipulator)_main.ZoznamPlc[0];
    private CControlLis Lis => (CControlLis)_main.ZoznamPlc[1];
    private CControlScales Scales => (CControlScales)_main.ZoznamPlc[2];

    // ==========================================
    // VEREJNÉ OPERÁCIE
    // ==========================================

    /// <summary>Zoznam receptov nájdených v Parameters/Recipes.</summary>
    public IReadOnlyList<string> GetRecipeNames()
    {
        try
        {
            Directory.CreateDirectory(RecipesDir);
            return Directory.EnumerateFiles(RecipesDir, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList()!;
        }
        catch (Exception ex)
        {
            Log.Error($"Chyba pri čítaní zoznamu receptov: {ex.Message}");
            return Array.Empty<string>();
        }
    }

    /// <summary>Načíta recept zo súboru bez toho, aby ho aplikoval. Slúži na výpis v dialógu.</summary>
    public CRecipe? PeekRecipe(string name) => ReadJson<CRecipe>(RecipePath(name));

    /// <summary>
    /// Načíta vrstvy Stroj, Forma a Výrobok a prenesie ich do bežiacich objektov.
    /// </summary>
    public bool Apply(string recipeName)
    {
        if (string.IsNullOrWhiteSpace(recipeName))
        {
            Log.Error("Apply: nie je zadaný názov receptu.");
            return false;
        }

        var recipe = ReadJson<CRecipe>(RecipePath(recipeName));
        if (recipe == null)
        {
            Log.Error($"Recept sa nepodarilo načítať: {recipeName}");
            return false;
        }

        var machine = ReadJson<CParametersMachine>(MachinePath);
        if (machine == null)
        {
            Log.Error($"Parametre stroja sa nepodarilo načítať: {MachinePath}");
            return false;
        }

        var form = ReadJson<CParametersForm>(FormPath(recipe.Form));
        if (form == null)
        {
            Log.Error($"Parametre formy sa nepodarilo načítať: {recipe.Form}");
            return false;
        }

        if (UpgradeRecipe(recipe)) WriteJson(RecipePath(recipeName), recipe);

        Machine = machine;
        Form = form;
        Recipe = recipe;
        ActiveRecipeName = recipeName;

        MachineToRuntime();
        FormToRuntime();
        RecipeToRuntime();

        // Kinematika musí dostať offsety zo stroja - dnes to robí CControlManipulator.LoadParameters.
        var delta = Manipulator.deltaRobot;
        delta.LoadOffsets(delta.ParametersDelta.OffsetSystem, delta.ParametersDelta.OffsetArm);

        Log.Information(
            $"Načítaný recept '{recipe.Name}' (režim {recipe.Mode}, forma {recipe.Form}).");
        return true;
    }

    /// <summary>Znovu načíta aktívny recept - používajú ho tlačidlá Load v nastaveniach.</summary>
    public bool Reload() => Apply(ActiveRecipeName);

    /// <summary>
    /// Uloží všetky tri vrstvy naraz. Zápis po častiach by mohol nechať
    /// nastavenie v polovične uloženom stave.
    /// </summary>
    public bool SaveAll()
    {
        if (string.IsNullOrWhiteSpace(ActiveRecipeName) || string.IsNullOrWhiteSpace(Recipe.Form))
        {
            Log.Error("SaveAll: nie je načítaný žiadny recept, niet čo uložiť.");
            return false;
        }

        RuntimeToMachine();
        RuntimeToForm();
        RuntimeToRecipe();

        bool ok = WriteJson(MachinePath, Machine);
        ok &= WriteJson(FormPath(Recipe.Form), Form);
        ok &= WriteJson(RecipePath(ActiveRecipeName), Recipe);

        if (ok)
        {
            Log.Information(
                $"Parametre uložené: Machine.json, Forms/{Recipe.Form}.json, Recipes/{ActiveRecipeName}.json");
        }

        return ok;
    }

    /// <summary>
    /// Ak ešte neexistuje žiadny recept, vytvorí vrstvenú štruktúru z pôvodných troch súborov.
    /// Pôvodné súbory ostávajú nedotknuté ako záloha.
    /// </summary>
    public bool MigrateIfNeeded()
    {
        try
        {
            Directory.CreateDirectory(FormsDir);
            Directory.CreateDirectory(RecipesDir);

            if (Directory.EnumerateFiles(RecipesDir, "*.json").Any()) return false;
        }
        catch (Exception ex)
        {
            Log.Error($"Migrácia: nepodarilo sa pripraviť priečinky: {ex.Message}");
            return false;
        }

        Log.Warning("Recepty neexistujú - spúšťam migráciu z pôvodných súborov parametrov.");

        // Pôvodné súbory načítame presne tak, ako to robil kód v konštruktoroch PLC.
        Manipulator.LoadParametersFromFile("ParametersDelta.json", Manipulator.deltaRobot.ParametersDelta);
        Lis.LoadParametersFromFile("ParametersLis.json", Lis.ParametersLis);
        Scales.LoadParametersFromFile("ParametersScale.json", Scales.ParametersScale);

        Form = new CParametersForm { Name = MigrationFormName };
        Recipe = new CRecipe
        {
            Name = MigrationRecipeName,
            Mode = EnModeVyroby.Single,
            Form = MigrationFormName
        };
        ActiveRecipeName = MigrationRecipeName;

        RuntimeToMachine();
        RuntimeToForm();
        RuntimeToRecipe();
        UpgradeRecipe(Recipe);

        WriteJson(MachinePath, Machine);
        WriteJson(FormPath(MigrationFormName), Form);
        WriteJson(RecipePath(MigrationRecipeName), Recipe);

        _main.ParametersMain.ActiveRecipe = MigrationRecipeName;
        _main.SaveParametersMain();

        Log.Information(
            $"Migrácia dokončená: Machine.json, Forms/{MigrationFormName}.json, Recipes/{MigrationRecipeName}.json");
        return true;
    }

    /// <summary>
    /// Povýši starší recept na aktuálnu schému. Vracia true, ak sa niečo zmenilo
    /// a súbor treba prepísať.
    /// </summary>
    private static bool UpgradeRecipe(CRecipe recipe)
    {
        if (recipe.SchemaVersion >= CurrentRecipeVersion) return false;

        // Verzia 1 -> 2: krok 130 prestal používať natvrdo zadanú hodnotu.
        recipe.Konzola.VyskaLisovacia = PouzivanaVyskaLisovacia;

        recipe.SchemaVersion = CurrentRecipeVersion;
        Log.Warning(
            $"Recept '{recipe.Name}' povýšený na verziu {CurrentRecipeVersion}: " +
            $"VyskaLisovacia nastavená na {PouzivanaVyskaLisovacia} (hodnota, ktorú krok 130 doteraz používal natvrdo).");
        return true;
    }

    // ==========================================
    // MAPOVANIE: VRSTVA STROJA
    // ==========================================

    private void MachineToRuntime()
    {
        var delta = Manipulator.deltaRobot.ParametersDelta;
        delta.OffsetArm = Machine.OffsetArm;
        delta.OffsetSystem = Machine.OffsetSystem;
        delta.RawLH = Machine.RawLH;
        delta.RawLD = Machine.RawLD;
        delta.EposLH = Machine.EposLH;
        delta.EposLD = Machine.EposLD;

        var lis = Lis.ParametersLis;
        lis.CanLine = Machine.CanLine;
        lis.BoardLine = Machine.BoardLine;
        lis.IDVaha1 = Machine.IDVaha1;
        lis.IDVaha2 = Machine.IDVaha2;
        lis.IDVaha3 = Machine.IDVaha3;
        lis.IDBox = Machine.IDBox;

        var scales = Scales.ParametersScale;
        scales.NodeIdVaha1 = Machine.NodeIdVaha1;
        scales.NodeIdVaha2 = Machine.NodeIdVaha2;
        scales.NodeIdVaha3 = Machine.NodeIdVaha3;
    }

    private void RuntimeToMachine()
    {
        var delta = Manipulator.deltaRobot.ParametersDelta;
        Machine.OffsetArm = delta.OffsetArm;
        Machine.OffsetSystem = delta.OffsetSystem;
        Machine.RawLH = delta.RawLH;
        Machine.RawLD = delta.RawLD;
        Machine.EposLH = delta.EposLH;
        Machine.EposLD = delta.EposLD;

        var lis = Lis.ParametersLis;
        Machine.CanLine = lis.CanLine;
        Machine.BoardLine = lis.BoardLine;
        Machine.IDVaha1 = lis.IDVaha1;
        Machine.IDVaha2 = lis.IDVaha2;
        Machine.IDVaha3 = lis.IDVaha3;
        Machine.IDBox = lis.IDBox;

        var scales = Scales.ParametersScale;
        Machine.NodeIdVaha1 = scales.NodeIdVaha1;
        Machine.NodeIdVaha2 = scales.NodeIdVaha2;
        Machine.NodeIdVaha3 = scales.NodeIdVaha3;
    }

    // ==========================================
    // MAPOVANIE: VRSTVA FORMY
    // ==========================================

    private void FormToRuntime()
    {
        var p = Lis.ParametersLis.ParLis;
        p.VyskaKalibra = Form.VyskaKalibra;
        p.VyskaSenAbsolut1 = Form.VyskaSenAbsolut1;
        p.VyskaSenAbsolut2 = Form.VyskaSenAbsolut2;
        p.SilaKalib1 = Form.SilaKalib1;
        p.SilaKalib2 = Form.SilaKalib2;
        p.VyskaSenPulz = Form.VyskaSenPulz;
        p.SmernicaK = Form.SmernicaK;
        p.KonstantaB = Form.KonstantaB;
    }

    private void RuntimeToForm()
    {
        var p = Lis.ParametersLis.ParLis;
        Form.VyskaKalibra = p.VyskaKalibra;
        Form.VyskaSenAbsolut1 = p.VyskaSenAbsolut1;
        Form.VyskaSenAbsolut2 = p.VyskaSenAbsolut2;
        Form.SilaKalib1 = p.SilaKalib1;
        Form.SilaKalib2 = p.SilaKalib2;
        Form.VyskaSenPulz = p.VyskaSenPulz;
        Form.SmernicaK = p.SmernicaK;
        Form.KonstantaB = p.KonstantaB;
    }

    // ==========================================
    // MAPOVANIE: VRSTVA VÝROBKU
    // ==========================================

    private void RecipeToRuntime()
    {
        var lis = Lis.ParametersLis.ParLis;
        lis.VyskaNasypacia = Recipe.Lis.VyskaNasypacia;
        lis.VyskaPriblizenie = Recipe.Lis.VyskaPriblizenie;
        lis.VyskaCistenia = Recipe.Lis.VyskaCistenia;

        var konzola = Lis.ParametersLis.ParKonzola;
        konzola.VyskaOdoberacia = Recipe.Konzola.VyskaOdoberacia;
        konzola.VyskaNasypacia = Recipe.Konzola.VyskaNasypacia;
        konzola.VyskaLisovacia = Recipe.Konzola.VyskaLisovacia;
        konzola.VyskaCistenia = Recipe.Konzola.VyskaCistenia;
        konzola.CyklovCistenia = Recipe.Konzola.CyklovCistenia;

        var vyrobok = Lis.ParametersLis.ParVyrobok;
        vyrobok.Name = Recipe.Vyrobok.Name;
        vyrobok.VyskaMax = Recipe.Vyrobok.VyskaMax;
        vyrobok.VyskaMin = Recipe.Vyrobok.VyskaMin;
        vyrobok.VyskaPozadovana = Recipe.Vyrobok.VyskaPozadovana;
        vyrobok.SilaMax = Recipe.Vyrobok.SilaMax;
        vyrobok.SilaMin = Recipe.Vyrobok.SilaMin;
        vyrobok.SilaPozadovana = Recipe.Vyrobok.SilaPozadovana;

        var vaha = Lis.ParametersLis.ParVaha;
        vaha.VahaPozadovana = Recipe.Vaha.VahaPozadovana;
        vaha.VahaMax = Recipe.Vaha.VahaMax;
        vaha.VahaMin = Recipe.Vaha.VahaMin;

        var delta = Manipulator.deltaRobot.ParametersDelta;
        delta.MatrixOkXfirst = Recipe.MatrixOk.Xfirst;
        delta.MatrixOkYfirst = Recipe.MatrixOk.Yfirst;
        delta.MatrixOkXdelta = Recipe.MatrixOk.Xdelta;
        delta.MatrixOkYdelta = Recipe.MatrixOk.Ydelta;
        delta.MatrixOkXnum = Recipe.MatrixOk.Xnum;
        delta.MatrixOkYnum = Recipe.MatrixOk.Ynum;

        delta.MatrixNokXfirst = Recipe.MatrixNok.Xfirst;
        delta.MatrixNokYfirst = Recipe.MatrixNok.Yfirst;
        delta.MatrixNokXdelta = Recipe.MatrixNok.Xdelta;
        delta.MatrixNokYdelta = Recipe.MatrixNok.Ydelta;
        delta.MatrixNokXnum = Recipe.MatrixNok.Xnum;
        delta.MatrixNokYnum = Recipe.MatrixNok.Ynum;

        var scales = Scales.ParametersScale;
        scales.EnabledVaha1 = Recipe.Vahy.EnabledVaha1;
        scales.EnabledVaha2 = Recipe.Vahy.EnabledVaha2;
        scales.EnabledVaha3 = Recipe.Vahy.EnabledVaha3;

        var man = Manipulator.ParManipulator;
        man.PolarParkovacia = Recipe.Manipulator.PolarParkovacia;
        man.PolarVychodiskova = Recipe.Manipulator.PolarVychodiskova;
        man.PolarZasunuta = Recipe.Manipulator.PolarZasunuta;
        man.PolarULisu = Recipe.Manipulator.PolarULisu;
        man.ZHorna = Recipe.Manipulator.ZHorna;
        man.ZNadVyrobkom = Recipe.Manipulator.ZNadVyrobkom;
        man.ZVylozenie = Recipe.Manipulator.ZVylozenie;
        man.CelusteOtvorene = Recipe.Manipulator.CelusteOtvorene;
        man.CelusteVysyp = Recipe.Manipulator.CelusteVysyp;
        man.CelusteUchopStred = Recipe.Manipulator.CelusteUchopStred;
        man.CelusteUchopSila = Recipe.Manipulator.CelusteUchopSila;
        man.CelusteUchopTolerancia = Recipe.Manipulator.CelusteUchopTolerancia;
        man.CelusteUchopTimeout = Recipe.Manipulator.CelusteUchopTimeout;

        // Odvodená hodnota - do receptu sa nezapisuje, slúži len ako kontext do logu.
        man.VyrobokName = Recipe.Vyrobok.Name;

        var lisovanie = Lis.ParametersLis.ParLisovanie;
        lisovanie.StredVychodzia = Recipe.Lisovanie.StredVychodzia;
        lisovanie.ProfilRychlyVelocity = Recipe.Lisovanie.ProfilRychlyVelocity;
        lisovanie.ProfilRychlyAcc = Recipe.Lisovanie.ProfilRychlyAcc;
        lisovanie.ProfilRychlyDcc = Recipe.Lisovanie.ProfilRychlyDcc;
        lisovanie.ProfilPomalyVelocity = Recipe.Lisovanie.ProfilPomalyVelocity;
        lisovanie.ProfilPomalyAcc = Recipe.Lisovanie.ProfilPomalyAcc;
        lisovanie.ProfilPomalyDcc = Recipe.Lisovanie.ProfilPomalyDcc;
        lisovanie.DobaDrzaniaMs = Recipe.Lisovanie.DobaDrzaniaMs;
        lisovanie.KrokPritlakuHruby = Recipe.Lisovanie.KrokPritlakuHruby;
        lisovanie.KrokPritlakuStredny = Recipe.Lisovanie.KrokPritlakuStredny;
        lisovanie.KrokPritlakuJemny = Recipe.Lisovanie.KrokPritlakuJemny;
        lisovanie.PrahStredny = Recipe.Lisovanie.PrahStredny;
        lisovanie.PrahJemny = Recipe.Lisovanie.PrahJemny;
        lisovanie.KrokUdrziavania = Recipe.Lisovanie.KrokUdrziavania;
    }

    private void RuntimeToRecipe()
    {
        var lis = Lis.ParametersLis.ParLis;
        Recipe.Lis.VyskaNasypacia = lis.VyskaNasypacia;
        Recipe.Lis.VyskaPriblizenie = lis.VyskaPriblizenie;
        Recipe.Lis.VyskaCistenia = lis.VyskaCistenia;

        var konzola = Lis.ParametersLis.ParKonzola;
        Recipe.Konzola.VyskaOdoberacia = konzola.VyskaOdoberacia;
        Recipe.Konzola.VyskaNasypacia = konzola.VyskaNasypacia;
        Recipe.Konzola.VyskaLisovacia = konzola.VyskaLisovacia;
        Recipe.Konzola.VyskaCistenia = konzola.VyskaCistenia;
        Recipe.Konzola.CyklovCistenia = konzola.CyklovCistenia;

        var vyrobok = Lis.ParametersLis.ParVyrobok;
        Recipe.Vyrobok.Name = vyrobok.Name;
        Recipe.Vyrobok.VyskaMax = vyrobok.VyskaMax;
        Recipe.Vyrobok.VyskaMin = vyrobok.VyskaMin;
        Recipe.Vyrobok.VyskaPozadovana = vyrobok.VyskaPozadovana;
        Recipe.Vyrobok.SilaMax = vyrobok.SilaMax;
        Recipe.Vyrobok.SilaMin = vyrobok.SilaMin;
        Recipe.Vyrobok.SilaPozadovana = vyrobok.SilaPozadovana;

        var vaha = Lis.ParametersLis.ParVaha;
        Recipe.Vaha.VahaPozadovana = vaha.VahaPozadovana;
        Recipe.Vaha.VahaMax = vaha.VahaMax;
        Recipe.Vaha.VahaMin = vaha.VahaMin;

        var delta = Manipulator.deltaRobot.ParametersDelta;
        Recipe.MatrixOk.Xfirst = delta.MatrixOkXfirst;
        Recipe.MatrixOk.Yfirst = delta.MatrixOkYfirst;
        Recipe.MatrixOk.Xdelta = delta.MatrixOkXdelta;
        Recipe.MatrixOk.Ydelta = delta.MatrixOkYdelta;
        Recipe.MatrixOk.Xnum = delta.MatrixOkXnum;
        Recipe.MatrixOk.Ynum = delta.MatrixOkYnum;

        Recipe.MatrixNok.Xfirst = delta.MatrixNokXfirst;
        Recipe.MatrixNok.Yfirst = delta.MatrixNokYfirst;
        Recipe.MatrixNok.Xdelta = delta.MatrixNokXdelta;
        Recipe.MatrixNok.Ydelta = delta.MatrixNokYdelta;
        Recipe.MatrixNok.Xnum = delta.MatrixNokXnum;
        Recipe.MatrixNok.Ynum = delta.MatrixNokYnum;

        var scales = Scales.ParametersScale;
        Recipe.Vahy.EnabledVaha1 = scales.EnabledVaha1;
        Recipe.Vahy.EnabledVaha2 = scales.EnabledVaha2;
        Recipe.Vahy.EnabledVaha3 = scales.EnabledVaha3;

        // VyrobokName sa zámerne nekopíruje späť - je odvodený z Recipe.Vyrobok.Name.
        var man = Manipulator.ParManipulator;
        Recipe.Manipulator.PolarParkovacia = man.PolarParkovacia;
        Recipe.Manipulator.PolarVychodiskova = man.PolarVychodiskova;
        Recipe.Manipulator.PolarZasunuta = man.PolarZasunuta;
        Recipe.Manipulator.PolarULisu = man.PolarULisu;
        Recipe.Manipulator.ZHorna = man.ZHorna;
        Recipe.Manipulator.ZNadVyrobkom = man.ZNadVyrobkom;
        Recipe.Manipulator.ZVylozenie = man.ZVylozenie;
        Recipe.Manipulator.CelusteOtvorene = man.CelusteOtvorene;
        Recipe.Manipulator.CelusteVysyp = man.CelusteVysyp;
        Recipe.Manipulator.CelusteUchopStred = man.CelusteUchopStred;
        Recipe.Manipulator.CelusteUchopSila = man.CelusteUchopSila;
        Recipe.Manipulator.CelusteUchopTolerancia = man.CelusteUchopTolerancia;
        Recipe.Manipulator.CelusteUchopTimeout = man.CelusteUchopTimeout;

        var lisovanie = Lis.ParametersLis.ParLisovanie;
        Recipe.Lisovanie.StredVychodzia = lisovanie.StredVychodzia;
        Recipe.Lisovanie.ProfilRychlyVelocity = lisovanie.ProfilRychlyVelocity;
        Recipe.Lisovanie.ProfilRychlyAcc = lisovanie.ProfilRychlyAcc;
        Recipe.Lisovanie.ProfilRychlyDcc = lisovanie.ProfilRychlyDcc;
        Recipe.Lisovanie.ProfilPomalyVelocity = lisovanie.ProfilPomalyVelocity;
        Recipe.Lisovanie.ProfilPomalyAcc = lisovanie.ProfilPomalyAcc;
        Recipe.Lisovanie.ProfilPomalyDcc = lisovanie.ProfilPomalyDcc;
        Recipe.Lisovanie.DobaDrzaniaMs = lisovanie.DobaDrzaniaMs;
        Recipe.Lisovanie.KrokPritlakuHruby = lisovanie.KrokPritlakuHruby;
        Recipe.Lisovanie.KrokPritlakuStredny = lisovanie.KrokPritlakuStredny;
        Recipe.Lisovanie.KrokPritlakuJemny = lisovanie.KrokPritlakuJemny;
        Recipe.Lisovanie.PrahStredny = lisovanie.PrahStredny;
        Recipe.Lisovanie.PrahJemny = lisovanie.PrahJemny;
        Recipe.Lisovanie.KrokUdrziavania = lisovanie.KrokUdrziavania;
    }

    // ==========================================
    // JSON
    // ==========================================

    private static T? ReadJson<T>(string path) where T : class
    {
        try
        {
            if (!File.Exists(path))
            {
                Log.Error($"Súbor s parametrami neexistuje: {path}");
                return null;
            }

            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions);
        }
        catch (Exception ex)
        {
            Log.Error($"Chyba pri načítaní {path}: {ex.Message}");
            return null;
        }
    }

    private static bool WriteJson<T>(string path, T value)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            File.WriteAllText(path, JsonSerializer.Serialize(value, JsonOptions));
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Chyba pri ukladaní {path}: {ex.Message}");
            return false;
        }
    }
}
