using System;
using EposCmd.Net;

namespace MojaPrvaAvalonia.Models;

public class CoaxialDelta2D
{
    // --- Hardvérové a kinematické parametre ---
    public double L1 { get; private set; }
    public double L2 { get; private set; }
    public double EncoderResolution { get; private set; }
    public double HalfResolution { get; private set; }

    // --- Referencie na motory ---
    private CDeviceEpos4 _motorDown;
    private CDeviceEpos4 _motorUp;

    // --- Vypočítané offsety (v pulzoch) ---
    public double OffsetSystem { get; private set; }
    public double OffsetArm { get; private set; }

    /// <summary>
    /// Konštruktor pre inicializáciu delta manipulátora.
    /// </summary>
    /// <param name="l1">Dĺžka krátkych (hnacích) ramien [mm]</param>
    /// <param name="l2">Dĺžka dlhých (pasívnych) ramien [mm]</param>
    /// <param name="encoderResolution">Rozlíšenie absolútneho snímača na otáčku (napr. 1048576)</param>
    public CoaxialDelta2D(double l1, double l2, double encoderResolution)
    {
        L1 = l1;
        L2 = l2;
        EncoderResolution = encoderResolution;
        HalfResolution = encoderResolution / 2.0;
    }

    /// <summary>
    /// Nastavenie referencií na fyzické motory pre výpočet polohy.
    /// </summary>
    public void SetMotors(CDeviceEpos4 motorDown, CDeviceEpos4 motorUp)
    {
        _motorDown = motorDown;
        _motorUp = motorUp;
    }

    /// <summary>
    /// Vypočíta kalibračné offsety na základe surových dát zo snímačov,
    /// keď sú obe ramená mechanicky nastavené presne na os +Y.
    /// Tieto dáta je vhodné následne uložiť do súboru.
    /// </summary>
    /// <param name="rawLH_Calib">Surová hodnota enkodéra Horného ramena na osi +Y</param>
    /// <param name="rawLD_Calib">Surová hodnota enkodéra Dolného ramena na osi +Y</param>
    public void CalculateAndSetCalibrationOffsets(double rawLH_Calib, double rawLD_Calib)
    {
        // OffsetSystem posunie horné rameno (Master) presne na os +Y (na nulu)
        OffsetSystem = -rawLH_Calib;

        // OffsetArm dorovnáva dolné rameno tak, aby bolo zhodné s horným
        OffsetArm = rawLH_Calib - rawLD_Calib;
    }

    /// <summary>
    /// Načítanie uložených offsetov pri štarte aplikácie (Krok 2 v tvojom scenári).
    /// </summary>
    public void LoadOffsets(double offsetSystem, double offsetArm)
    {
        OffsetSystem = offsetSystem;
        OffsetArm = offsetArm;
    }

    /// <summary>
    /// Spracuje surové dáta zo snímačov po zapnutí napájania a vypočíta 
    /// interné (signed) polohy v pulzoch s asymetrickým rozsahom pracovného priestoru.
    /// </summary>
    public void CalculateColdStartPositions(double rawLH_Startup, double rawLD_Startup, out double actualLH,
        out double actualLD)
    {
        // 1. Aplikovanie kalibračných offsetov
        double lh_shifted = rawLH_Startup + OffsetSystem;
        double ld_shifted = rawLD_Startup + OffsetArm + OffsetSystem;

        // 2. Bezpečné matematické modulo (vždy vráti 0 až 1 048 575)
        actualLH = ((lh_shifted % EncoderResolution) + EncoderResolution) % EncoderResolution;
        actualLD = ((ld_shifted % EncoderResolution) + EncoderResolution) % EncoderResolution;

        // 3. Konverzia na Signed hodnoty s ASYMETRICKÝMI HRANICAMI

        // Hranica pre LH: +90° (Všetko fyzicky za +90° je považované za záporné natočenie, napr. -256°)
        double boundaryLH = EncoderResolution * (90.0 / 360.0); // 262 144 pulzov
        if (actualLH > boundaryLH)
        {
            actualLH -= EncoderResolution;
        }

        // Hranica pre LD: +315° (Všetko fyzicky za +315° je považované za záporné natočenie, max je +275°)
        // Umožňuje dolnému ramenu ísť do záporu maximálne po -45°.
        double boundaryLD = EncoderResolution * (315.0 / 360.0); // 917 504 pulzov
        if (actualLD > boundaryLD)
        {
            actualLD -= EncoderResolution;
        }

       
    }
    public void MoveRight(double angle)
    {
        _motorDown.Operation.ProfilePositionMode.MoveToPositionGear(angle, false, true);
        _motorUp.Operation.ProfilePositionMode.MoveToPositionGear(angle, false, true);
    }
    public void MoveLeft(double angle)
    {
        _motorDown.Operation.ProfilePositionMode.MoveToPositionGear(-angle, false, true);
        _motorUp.Operation.ProfilePositionMode.MoveToPositionGear(-angle, false, true);
    }
    public void MoveUp(double angle)
    {
        _motorDown.Operation.ProfilePositionMode.MoveToPositionGear(-angle, false, true);
        _motorUp.Operation.ProfilePositionMode.MoveToPositionGear(angle, false, true);
    }
    public void MoveDown(double angle)
    {
        _motorDown.Operation.ProfilePositionMode.MoveToPositionGear(angle, false, true);
        _motorUp.Operation.ProfilePositionMode.MoveToPositionGear(-angle, false, true);
        _motorDown.Data.PositionActualGear
    }
}
