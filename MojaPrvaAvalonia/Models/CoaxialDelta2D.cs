using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using EposCmd.Net;

namespace MojaPrvaAvalonia.Models;

public partial class CoaxialDelta2D : ObservableObject
{
    // --- Hardvérové a kinematické parametre ---
    public double L1 { get; private set; }
    public double L2 { get; private set; }
    public double EncoderResolution { get; private set; }
    public double HalfResolution { get; private set; }

    // --- Referencie na motory ---
    private CDeviceEpos4 _motorDown;
    private CDeviceEpos4 _motorUp;

    // --- Dynamické polohy pre UI ---
    [ObservableProperty]
    private double _currentR;

    [ObservableProperty]
    private double _currentPhi;

    private CancellationTokenSource _cts;

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
        LogCurrentPolar("MoveRight", angle);
        MoveAngleRelative(angle);
    }

    public void MoveLeft(double angle)
    {
        LogCurrentPolar("MoveLeft", angle);
        MoveAngleRelative(-angle);
    }

    private void MoveAngleRelative(double deltaPhi)
    {
        if (_motorDown?.Data == null || _motorUp?.Data == null) return;
        
        UpdatePositions();
        MoveToPhi(CurrentPhi + deltaPhi);
    }

    /// <summary>
    /// Presun na absolútne polárne súradnice [R, Phi].
    /// </summary>
    public void MoveToPolar(double r, double phi)
    {
        if (_motorDown?.Data == null || _motorUp?.Data == null) return;

        // Kontrola softvérových limitov pre vzdialenosť (R)
        if (r < 56.0 || r > 270.0)
        {
            Serilog.Log.Logger.ForContext("Name", "Delta2D").Error($"MoveToPolar(R:{r:F1}, Phi:{phi:F1}) ZAMIETNUTÉ: Polomer mimo rozsahu (56 - 270 mm).");
            return;
        }

        double alpha = CalculateAlphaFromR(r);

        Serilog.Log.Logger.ForContext("Name", "Delta2D").Information($"MoveToPolar: R:{r:F1}mm, Phi:{phi:F2}° (Alpha:{alpha:F2})");

        // MotorDown = stred + alpha, MotorUp = stred - alpha
        _motorDown.Operation.ProfilePositionMode.MoveToPositionGear(phi + alpha, true, true);
        _motorUp.Operation.ProfilePositionMode.MoveToPositionGear(phi - alpha, true, true);
    }

    /// <summary>
    /// Presun na absolútny polomer R pri zachovaní aktuálneho uhla Phi.
    /// </summary>
    public void MoveToR(double r)
    {
        UpdatePositions();
        MoveToPolar(r, CurrentPhi);
    }

    /// <summary>
    /// Presun na absolútny uhol Phi pri zachovaní aktuálneho polomeru R.
    /// </summary>
    public void MoveToPhi(double phi)
    {
        UpdatePositions();
        MoveToPolar(CurrentR, phi);
    }

    public void MoveUp(double distance)
    {
        MoveRadialRelative(distance);
    }

    public void MoveDown(double distance)
    {
        MoveRadialRelative(-distance);
    }

    private void MoveRadialRelative(double deltaR)
    {
        if (_motorDown?.Data == null || _motorUp?.Data == null) return;

        double ad = _motorDown.Data.PositionActualGear;
        double au = _motorUp.Data.PositionActualGear;

        // 1. Získame aktuálny stav.
        // Kedze ad je kladne (napr. 135) a au je zaporne (napr. -135), 
        // stred phi = (ad + au) / 2.
        double phi = (ad + au) / 2.0;
        
        // Alpha je polovica roztvorenia medzi ramenami.
        double alphaOld = Math.Abs(ad - au) / 2.0;
        double rOld = CalculateRFromAlpha(alphaOld);

        // 2. Vypočítame nové R
        double rNew = rOld + deltaR;

        // Kontrola softvérových limitov pre vzdialenosť (R)
        if (rNew < 56.0 || rNew > 270.0)
        {
            Serilog.Log.Logger.Error($"[DELTA] MoveRadial({deltaR:F1}mm) ZAMIETNUTÉ: Cieľová vzdialenosť R={rNew:F1}mm je mimo povoleného rozsahu (56 - 270 mm).");
            return;
        }

        // Vypocet noveho uhla roztvorenia Alpha
        double alphaNew = CalculateAlphaFromR(rNew);

        Serilog.Log.Logger.Information($"[DELTA] MoveRadial({deltaR:F1}mm): R:{rOld:F1}->{rNew:F1}mm, Phi:{phi:F2}° (Alpha:{alphaOld:F2}->{alphaNew:F2})");

        // 3. Nastavíme motory na nové ABSOLÚTNE polohy (true).
        // Kedze MotorDown (ad) ide do kladnych hodnot a MotorUp (au) do zapornych,
        // MotorDown = stred + alpha, MotorUp = stred - alpha
        _motorDown.Operation.ProfilePositionMode.MoveToPositionGear(phi + alphaNew, true, true);
        _motorUp.Operation.ProfilePositionMode.MoveToPositionGear(phi - alphaNew, true, true);
    }


    public void WaitForTargetReached(uint timeout)
    {
        _motorDown.Operation.MotionInfo.WaitForTargetReached(timeout);
        _motorUp.Operation.MotionInfo.WaitForTargetReached(timeout);
    }
    private double CalculateAlphaFromR(double r)
    {
        // Kosínusová veta pre uhol alpha: cos(alpha) = (R^2 + L1^2 - L2^2) / (2 * R * L1)
        double val = (r * r + L1 * L1 - L2 * L2) / (2.0 * r * L1);
        
        // Ošetrenie limitov (matematická bezpečnosť)
        if (val > 1.0) val = 1.0;
        if (val < -1.0) val = -1.0;

        return Math.Acos(val) * 180.0 / Math.PI;
    }

    private double CalculateRFromAlpha(double alphaDeg)
    {
        double alphaRad = Math.PI * alphaDeg / 180.0;
        return L1 * Math.Cos(alphaRad) + Math.Sqrt(L2 * L2 - Math.Pow(L1 * Math.Sin(alphaRad), 2));
    }

    private void LogCurrentPolar(string method, double inputVal)
    {
        UpdatePositions();
        Serilog.Log.Logger.ForContext("Name", "Delta2D").Information($"{method}({inputVal}): Ad:{_motorDown.Data.PositionActualGear:F2}° Au:{_motorUp.Data.PositionActualGear:F2}° => R:{CurrentR:F2}mm, Phi:{CurrentPhi:F2}°");
    }

    public void UpdatePositions()
    {
        if (_motorDown?.Data == null || _motorUp?.Data == null) return;

        double ad = _motorDown.Data.PositionActualGear;
        double au = _motorUp.Data.PositionActualGear;

        CurrentPhi = (ad + au) / 2.0;
        double alphaDeg = Math.Abs(ad - au) / 2.0;
        CurrentR = CalculateRFromAlpha(alphaDeg);
    }

    public void StartMonitoring()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    UpdatePositions();
                }
                catch (Exception) { }
                await Task.Delay(100, token);
            }
        }, token);
    }
}
