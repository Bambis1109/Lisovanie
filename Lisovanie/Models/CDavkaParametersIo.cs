using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Serilog;

namespace Lisovanie.Models;

/// <summary>
/// Spoločná logika pre parametre riadenia dávky (SDO 0x6006): preklad property na SDO adresu
/// a čítanie/zápis plochého JSON súboru.
/// Rovnaké mená kľúčov používa aj sekcia Vaha.Davka v recepte, takže export z okna
/// jednej váhy aj export zo spoločného okna sú navzájom zameniteľné.
/// </summary>
public static class CDavkaParametersIo
{
    public const string DavkaCategory = "6. RIADENIE DÁVKY (0x6006)";

    // Index je v názve kategórie: "6. RIADENIE DÁVKY (0x6006)" -> 0x6006
    private static readonly Regex IndexRegex = new(@"\(0x([0-9A-Fa-f]{4})\)", RegexOptions.Compiled);

    // Subindex je v DisplayName: "0x20: rs_target_weight_mg" -> 0x20
    private static readonly Regex SubIndexRegex = new(@"0x([0-9A-Fa-f]{2})", RegexOptions.Compiled);

    /// <summary>Property triedy DeviceParameters patriace do kategórie riadenia dávky.</summary>
    public static IEnumerable<PropertyInfo> DavkaProperties =>
        typeof(DeviceParameters)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<CategoryAttribute>()?.Category == DavkaCategory);

    /// <summary>Vytiahne SDO index a subindex z atribútov Category a DisplayName danej property.</summary>
    public static bool TryGetSdoAddress(PropertyInfo property, out ushort index, out byte subIndex)
    {
        index = 0;
        subIndex = 0;

        var category = property.GetCustomAttribute<CategoryAttribute>()?.Category;
        var displayName = property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
        if (category == null || displayName == null) return false;

        var indexMatch = IndexRegex.Match(category);
        var subMatch = SubIndexRegex.Match(displayName);
        if (!indexMatch.Success || !subMatch.Success) return false;

        index = Convert.ToUInt16(indexMatch.Groups[1].Value, 16);
        subIndex = Convert.ToByte(subMatch.Groups[1].Value, 16);
        return index != 0;
    }

    /// <summary>
    /// Prevedie parametre riadenia dávky na slovník meno -> hodnota.
    /// Kľúče sú zhodné s formátom súboru, takže recept aj export sú navzájom čitateľné.
    /// </summary>
    public static Dictionary<string, int> ToDictionary(DeviceParameters source)
    {
        var data = new Dictionary<string, int>();

        foreach (var property in DavkaProperties)
        {
            data[property.Name] = (int)(property.GetValue(source) ?? 0);
        }

        return data;
    }

    /// <summary>Prenesie hodnoty zo slovníka do parametrov. Neznáme kľúče sa ignorujú.</summary>
    public static void FromDictionary(IReadOnlyDictionary<string, int> data, DeviceParameters target)
    {
        foreach (var property in DavkaProperties)
        {
            if (!property.CanWrite) continue;
            if (data.TryGetValue(property.Name, out int value)) property.SetValue(target, value);
        }
    }

    /// <summary>Načíta parametre riadenia dávky zo súboru.</summary>
    public static bool Load(string path, DeviceParameters target)
        => Load(path, target, DavkaProperties);

    /// <summary>Načíta zvolenú množinu parametrov zo súboru. Neznáme kľúče sa ignorujú.</summary>
    public static bool Load(string path, DeviceParameters target, IEnumerable<PropertyInfo> properties)
    {
        try
        {
            if (!File.Exists(path))
            {
                Log.Warning($"Súbor s parametrami neexistuje: {path}");
                return false;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(path));

            foreach (var property in properties)
            {
                if (!property.CanWrite) continue;

                if (document.RootElement.TryGetProperty(property.Name, out var element) &&
                    element.TryGetInt32(out int value))
                {
                    property.SetValue(target, value);
                }
            }

            Log.Information($"Parametre načítané zo súboru: {path}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Chyba pri načítaní parametrov zo súboru {path}: {ex.Message}");
            return false;
        }
    }

    /// <summary>Uloží parametre riadenia dávky do súboru.</summary>
    public static bool Save(string path, DeviceParameters source)
        => Save(path, source, DavkaProperties);

    /// <summary>Uloží zvolenú množinu parametrov do súboru ako plochý slovník meno -> hodnota.</summary>
    public static bool Save(string path, DeviceParameters source, IEnumerable<PropertyInfo> properties)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            var data = properties.ToDictionary(p => p.Name, p => p.GetValue(source));
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);

            Log.Information($"Parametre uložené do súboru: {path}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Chyba pri ukladaní parametrov do súboru {path}: {ex.Message}");
            return false;
        }
    }
}
