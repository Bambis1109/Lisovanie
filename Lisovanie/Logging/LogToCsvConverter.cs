using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Serilog;

namespace Lisovanie.Logging;

public static class LogToCsvConverter
{
    public static bool ProcessLogFile(string inputFilePath)
    {
        if (!File.Exists(inputFilePath)) return false;
        try
        {
            var lines = File.ReadAllLines(inputFilePath);
            var result = ProcessLines(lines);
            if (result == null || result.Count == 0) return false;
            string outputFilePath = Path.ChangeExtension(inputFilePath, "_AI_Optimized.txt");
            File.WriteAllLines(outputFilePath, result, Encoding.UTF8);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Chyba v LogToCsvConverter.ProcessLogFile");
            return false;
        }
    }

    // Jadro konverzie — pracuje nad ľubovoľnou sekvenciou riadkov.
    // Vstup: surové riadky logu (zo súboru alebo terminálu po odstránení prefixu).
    // Výstup: riadky v AI formáte, alebo null ak žiadna kompletná dávka.
    public static List<string>? ProcessLines(IEnumerable<string> lines)
    {
        var staticParamKeys = new HashSet<string>
            { "TargetWeight", "TolPlus", "TolMinus", "BulkLimit", "FlowTarget",
              "RpmMax", "RpmMin", "KiBulk", "KpFine", "KiFine" };
        var globalParams = new Dictionary<string, string>();

        List<string> mainBuffer  = [];
        List<string> doseBuffer  = [];

        bool inDose              = false;
        bool isFirstDataRow      = true;
        bool globalHeaderWritten = false;

        string currentDoseId         = "";
        string currentAdaptRpm       = "0";
        string currentPreact         = "0";
        string currentPhase          = "";
        int    phaseRowCounter        = 0;
        string lastFormattedDataLine = "";
        string lastPrintedDataLine   = "";
        string doseStatus            = "";

        try
        {
            foreach (var line in lines)
            {
                // 1. Začiatok dávky
                if (line.Contains("--- DOSE_START:"))
                {
                    inDose           = true;
                    isFirstDataRow   = true;
                    currentPhase     = "";
                    phaseRowCounter  = 0;
                    lastFormattedDataLine = "";
                    lastPrintedDataLine   = "";
                    doseBuffer.Clear();

                    int idx = line.IndexOf("DOSE_START:") + 11;
                    currentDoseId = line[idx..].Replace("-", "").Trim();
                    continue;
                }

                if (!inDose) continue;

                // 2. Parametre
                if (line.Contains("PARAM:"))
                {
                    int pi = line.IndexOf("PARAM:") + 6;
                    var kvp = line[pi..].Trim().Split('=');
                    if (kvp.Length == 2)
                    {
                        string key = kvp[0].Trim();
                        string val = CleanNumber(kvp[1].Trim());
                        if (staticParamKeys.Contains(key) && !globalHeaderWritten)
                            globalParams[key] = val;
                        else if (key == "AdaptBulkRpm")   currentAdaptRpm = val;
                        else if (key == "PreactLearned")  currentPreact   = val;
                    }
                    continue;
                }

                // 3. Dátové riadky
                if (line.Contains("DATA:"))
                {
                    if (!globalHeaderWritten && globalParams.Count > 0)
                    {
                        mainBuffer.Add("[GLOBÁLNE PARAMETRE]; " +
                            string.Join("; ", globalParams.Select(kv => $"{kv.Key}:{kv.Value}")));
                        mainBuffer.Add("");
                        globalHeaderWritten = true;
                    }

                    if (isFirstDataRow)
                        doseBuffer.Add($"# DOSE START; ID: {currentDoseId}; AdaptRpm: {currentAdaptRpm}; Preact: {currentPreact}");

                    int di = line.IndexOf("DATA:") + 5;
                    var parts = line[di..].Trim()
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length >= 8)
                    {
                        string phase  = parts[0];
                        string time   = CleanNumber(parts[1]);
                        string weight = CleanNumber(parts[2]);
                        string flow   = CleanNumber(parts[3]);
                        string rpm    = CleanNumber(parts[4]);
                        string preact = CleanNumber(parts[7]);

                        if (isFirstDataRow)
                        {
                            flow = "0.000";
                            isFirstDataRow = false;
                        }

                        if (phase != currentPhase)
                        {
                            if (currentPhase != "" && lastFormattedDataLine != lastPrintedDataLine)
                                doseBuffer.Add(lastFormattedDataLine);

                            currentPhase    = phase;
                            phaseRowCounter = 0;
                            doseBuffer.Add($"[FAZA {currentPhase}]");
                        }

                        lastFormattedDataLine = (phase is "1" or "1.5")
                            ? $"{time};{weight};{flow};{rpm}"
                            : $"{time};{weight};{flow};{rpm};{preact}";

                        bool shouldPrint =
                            phaseRowCounter < 3 ||
                            (phase == "1"   && phaseRowCounter % 6 == 0) ||
                             phase == "1.5" ||
                            (phase == "2"   && phaseRowCounter % 3 == 0);

                        if (shouldPrint)
                        {
                            doseBuffer.Add(lastFormattedDataLine);
                            lastPrintedDataLine = lastFormattedDataLine;
                        }

                        phaseRowCounter++;
                    }
                    continue;
                }

                // 4. Výsledok — status
                if (line.Contains("=== VYS D"))
                {
                    if (lastFormattedDataLine != lastPrintedDataLine)
                        doseBuffer.Add(lastFormattedDataLine);

                    doseStatus = line.Contains("[OK]") ? "OK" : "NOK";
                    continue;
                }

                // 5. Výsledok — čas + váha → COMMIT
                if (line.Contains("T:") && line.Contains("| C:") && line.Contains("| R:"))
                {
                    try
                    {
                        var p = line.Split('|');
                        string timeStr        = p[0][(p[0].IndexOf("T:") + 2)..].Replace("s", "").Trim();
                        string finalWeightStr = p[2][(p[2].IndexOf("R:") + 2)..].Replace("g", "").Trim();

                        doseBuffer.Add($"# DOSE RESULT; ID: {currentDoseId}; " +
                                       $"FinalWeight: {CleanNumber(finalWeightStr)}; " +
                                       $"Time: {CleanNumber(timeStr)}; Status: {doseStatus}");
                        doseBuffer.Add("");

                        mainBuffer.AddRange(doseBuffer);
                        doseBuffer.Clear();
                    }
                    catch { /* ignorujeme chybu parsovania výsledku */ }

                    inDose = false;
                }
            }

            return mainBuffer.Count > 0 ? mainBuffer : null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Chyba v LogToCsvConverter.ProcessLines");
            return null;
        }
    }

    private static string CleanNumber(string val) =>
        string.IsNullOrWhiteSpace(val) || val.Equals("nan", StringComparison.OrdinalIgnoreCase)
            ? "0.000"
            : val;
}
