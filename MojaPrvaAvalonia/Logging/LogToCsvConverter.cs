using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Serilog;

namespace MojaPrvaAvalonia.Logging;

public static class LogToCsvConverter
{
    /// <summary>
    /// Spracuje log súbor a vytvorí CSV súbor s rovnakým názvom.
    /// </summary>
    /// <param name="filePath">Cesta k log súboru.</param>
    /// <returns>True ak spracovanie prebehlo úspešne.</returns>
    public static bool ProcessLogFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return false;

            string csvPath = Path.ChangeExtension(filePath, ".csv");
            var lines = File.ReadAllLines(filePath);
            var csvContent = new StringBuilder();

            // Hlavička pre CSV (upravte podľa reálneho formátu logu)
            csvContent.AppendLine("Timestamp;Level;Message;Data");

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Tu by mala byť logika parsovania logu. 
                // Pre začiatok urobíme jednoduché CSV kde Message je celý riadok.
                // Ak log obsahuje napr. "2023-10-27 10:00:00 [INF] Value: 123", skúsime to rozdeliť.
                
                string escapedLine = line.Replace(";", ",");
                csvContent.AppendLine($";;;{escapedLine}");
            }

            File.WriteAllText(csvPath, csvContent.ToString(), Encoding.UTF8);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Chyba v LogToCsvConverter.ProcessLogFile: {Message}", ex.Message);
            return false;
        }
    }
}