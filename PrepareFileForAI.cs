using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

// ==========================================
// 1. OPRAVA PRACOVNÉHO ADRESÁRA
// ==========================================
string GetScriptDirectory([CallerFilePath] string filePath = "")
{
    return Path.GetDirectoryName(filePath) ?? string.Empty;
}

string scriptDir = GetScriptDirectory();
if (!string.IsNullOrEmpty(scriptDir))
{
    // Nastavíme pracovný adresár na zložku, kde fyzicky leží tento .cs súbor.
    // Týmto opravíme všetky problémy s relatívnymi cestami nižšie.
    Environment.CurrentDirectory = scriptDir;
}

// ==========================================
// 2. NASTAVENIA SKRIPTU
// ==========================================

// Názov výstupného súboru (vytvorí sa pri tomto .cs skripte / .sln súbore)
string outputFile = "ProjektPreAI.txt";

// NÁZOV TVOJHO PROJEKTU (priečinka, do ktorého má skript vojsť)
string projectFolder = "MojaPrvaAvalonia";

// Priečinky, ktoré chceme prehľadať
string[] targetDirectories = { 
    $"{projectFolder}/Converters", 
    $"{projectFolder}/Logging", 
    $"{projectFolder}/Models", 
    $"{projectFolder}/ViewModels", 
    $"{projectFolder}/Views" 
};

// Konkrétne súbory, ktoré chceme pridať
string[] rootFiles = { 
    $"{projectFolder}/App.axaml", 
    $"{projectFolder}/App.axaml.cs", 
    $"{projectFolder}/app.manifest", 
    $"{projectFolder}/Program.cs", 
    $"{projectFolder}/ViewLocator.cs" 
};

// Povolené prípony
string[] allowedExtensions = { ".cs", ".axaml", ".manifest", ".json" };

// ==========================================
// 3. SPRACOVANIE
// ==========================================

Console.WriteLine("Spúšťam generovanie kontextu pre AI...");
Console.WriteLine($"Pracovný adresár je nastavený na: {Environment.CurrentDirectory}");

// Zmažeme starý súbor, ak existuje, aby sme mali čerstvé dáta
if (File.Exists(outputFile))
{
    File.Delete(outputFile);
}

List<string> filesToProcess = new List<string>();

// Zozbieranie súborov z hlavného adresára
foreach (var file in rootFiles)
{
    if (File.Exists(file))
    {
        filesToProcess.Add(file);
    }
    else
    {
        Console.WriteLine($"[UPOZORNENIE] Súbor '{file}' sa nenašiel a bude preskočený.");
    }
}

// Zozbieranie súborov z podadresárov
foreach (var dir in targetDirectories)
{
    if (Directory.Exists(dir))
    {
        // Najde všetky súbory v danom priečinku a jeho podadresároch
        var filesInDir = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)
                                  .Where(f => allowedExtensions.Contains(Path.GetExtension(f).ToLower()));
        
        filesToProcess.AddRange(filesInDir);
    }
    else
    {
        Console.WriteLine($"[UPOZORNENIE] Priečinok '{dir}' sa nenašiel.");
    }
}

// Spájanie súborov a zápis do výsledného textového súboru
using (StreamWriter writer = new StreamWriter(outputFile))
{
    foreach (var filePath in filesToProcess)
    {
        try
        {
            // Zjednotenie lomiek v ceste pre lepšiu čitateľnosť
            string normalizedPath = filePath.Replace("\\", "/");
            string content = File.ReadAllText(filePath);

            // Zápis hlavičky s názvom súboru
            writer.WriteLine("// ==========================================");
            writer.WriteLine($"// SÚBOR: {normalizedPath}");
            writer.WriteLine("// ==========================================");
            
            // Zápis samotného kódu
            writer.WriteLine(content);
            writer.WriteLine(); // Prázdny riadok
            writer.WriteLine(); // Prázdny riadok pre vizuálne oddelenie ďalšieho súboru
            
            Console.WriteLine($"Pridané: {normalizedPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CHYBA] Problém pri čítaní súboru {filePath}: {ex.Message}");
        }
    }
}

Console.WriteLine();
Console.WriteLine("=====================================================");
Console.WriteLine($"HOTOVO! Súbor bol úspešne vygenerovaný ako: {Path.GetFullPath(outputFile)}");