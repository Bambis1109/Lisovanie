using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EposCmd.Net;
using Serilog;

namespace MojaPrvaAvalonia.Models;

public enum EnMovementMode
{
    Polar,
    XY
}

public partial class CoaxialDelta2D : ObservableObject
{
    // --- Hardvérové a kinematické parametre ---
    private double _l1;
    private double _l2;
    private double _encoderResolution;
    private double _halfResolution;

    // --- Referencie na motory ---
    private CDeviceEpos4 _motorDown;
    private CDeviceEpos4 _motorUp;

    // --- Parametre ---
    public CParameters ParametersDelta { get; set; } = new();

    // --- Dynamické polohy pre UI ---
    [ObservableProperty] private double _currentR;

    [ObservableProperty] private double _currentPhi;

    [ObservableProperty] private double _currentX;

    [ObservableProperty] private double _currentY;

    [ObservableProperty] private double _stepSize = 1.0;

    private double _minTCP = 56.0;
    private double _maxTCP = 270.0;
    private double _offsetGCP = 0.0;

    [ObservableProperty] private EnMovementMode _movementMode = EnMovementMode.Polar;

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
    /// <param name="minTCP">Minimálny povolený polomer TCP [mm]</param>
    /// <param name="maxTCP">Maximálny povolený polomer TCP [mm]</param>
    /// <param name="offsetGCP">Vzdialenosť od TCP k stredu grippera [mm]</param>
    public CoaxialDelta2D(double l1, double l2, double encoderResolution, double minTCP, double maxTCP,
        double offsetGCP)
    {
        _l1 = l1;
        _l2 = l2;
        _encoderResolution = encoderResolution;
        _halfResolution = encoderResolution / 2.0;
        _minTCP = minTCP;
        _maxTCP = maxTCP;
        _offsetGCP = offsetGCP;
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
    /// Jemné doladenie orientácie systému.
    /// Ak je manipulátor mechanicky nastavený presne na os +Y, ale softvér ukazuje odchýlku CurrentPhi,
    /// táto metóda túto odchýlku eliminuje úpravou OffsetSystem.
    /// </summary>
    public void OrientSystem()
    {
        UpdatePositions();
        double phiErrorPulses = CurrentPhi * (_encoderResolution / 360.0);
        OffsetSystem -= phiErrorPulses;
        UpdatePositions();
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
        actualLH = ((lh_shifted % _encoderResolution) + _encoderResolution) % _encoderResolution;
        actualLD = ((ld_shifted % _encoderResolution) + _encoderResolution) % _encoderResolution;

        // 3. Konverzia na Signed hodnoty s ASYMETRICKÝMI HRANICAMI

        // Hranica pre LH: +90° (Všetko fyzicky za +90° je považované za záporné natočenie, napr. -256°)
        double boundaryLH = _encoderResolution * (90.0 / 360.0); // 262 144 pulzov
        if (actualLH > boundaryLH)
        {
            actualLH -= _encoderResolution;
        }

        // Hranica pre LD: +315° (Všetko fyzicky za +315° je považované za záporné natočenie, max je +275°)
        // Umožňuje dolnému ramenu jít do záporu maximálně po -45°.
        double boundaryLD = _encoderResolution * (315.0 / 360.0); // 917 504 pulzov
        if (actualLD > boundaryLD)
        {
            actualLD -= _encoderResolution;
        }
    }

    public void MoveRight()
    {
        if (MovementMode == EnMovementMode.Polar)
        {
            LogCurrentPolar("MoveRight(Polar)", StepSize);
            MoveAngleRelative(StepSize);
        }
        else
        {
            Serilog.Log.Logger.ForContext("Name", "Delta2D").Information($"MoveRight(XY): X + {StepSize}");
            MoveToXY(CurrentX + StepSize, CurrentY);
        }
    }

    public void MoveLeft()
    {
        if (MovementMode == EnMovementMode.Polar)
        {
            LogCurrentPolar("MoveLeft(Polar)", StepSize);
            MoveAngleRelative(-StepSize);
        }
        else
        {
            Serilog.Log.Logger.ForContext("Name", "Delta2D").Information($"MoveLeft(XY): X - {StepSize}");
            MoveToXY(CurrentX - StepSize, CurrentY);
        }
    }

    private void MoveAngleRelative(double deltaPhi)
    {
        if (_motorDown?.Data == null || _motorUp?.Data == null) return;

        UpdatePositions();
        MoveToPhi(CurrentPhi + deltaPhi);
    }

    /// <summary>
    /// Presun na absolútne polárne súradnice [R, Phi].
    /// r je v tomto prípade cieľový GCP polomer (stred grippera).
    /// </summary>
    public void MoveToPolar(double phi,double r )
    {
        if (_motorDown?.Data == null || _motorUp?.Data == null) return;

        // Musíme vypočítať TCP polomer (kĺb ramien), aby sme mohli vypočítať Alpha
        double rTcp = r - _offsetGCP;

        // Kontrola softvérových limitov pre mechaniku (TCP)
        if (rTcp < _minTCP || rTcp > _maxTCP)
        {
            Serilog.Log.Logger.ForContext("Name", "Delta2D").Error(
                $"MoveToPolar(GCP-R:{r:F1}, Phi:{phi:F1}) ZAMIETNUTÉ: Mechanický polomer TCP ({rTcp:F1}) mimo rozsahu ({_minTCP:F1} - {_maxTCP:F1} mm).");
            return;
        }

        double alpha = CalculateAlphaFromR(rTcp);

        //    Serilog.Log.Logger.ForContext("Name", "Delta2D").Information($"MoveToPolar: GCP-R:{r:F1}mm (TCP-R:{rTcp:F1}), Phi:{phi:F2}° (Alpha:{alpha:F2})");

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
        MoveToPolar( phi,CurrentR);
    }

    /// <summary>
    /// Presun na absolútne karteziánske súradnice [X, Y].
    /// Bod [0,0] je stred rotácie manipulátora.
    /// Orientácia: +Y = 0 stupňov (vpred), +X = +90 stupňov (vpravo).
    /// </summary>
    public void MoveToXY(double x, double y)
    {
        // Vypočet vzdialenosti (R) od stredu pomocou Pytagorovej vety
        double r = Math.Sqrt(x * x + y * y);

        // Vypočet uhla (Phi) tak, aby +Y bolo 0° a +X bolo +90°
        // Použijeme Atan2(x, y) namiesto štandardného (y, x)
        double phiDeg = Math.Atan2(x, y) * 180.0 / Math.PI;

        // Serilog.Log.Logger.ForContext("Name", "Delta2D").Information($"MoveToXY(X:{x:F1}, Y:{y:F1}) -> Prepočítané na R:{r:F1}mm, Phi:{phiDeg:F2}°");

        MoveToPolar(phiDeg,r);
    }

    public void MoveUp()
    {
        if (MovementMode == EnMovementMode.Polar)
        {
            MoveRadialRelative(StepSize);
        }
        else
        {
            Serilog.Log.Logger.ForContext("Name", "Delta2D").Information($"MoveUp(XY): Y + {StepSize}");
            MoveToXY(CurrentX, CurrentY + StepSize);
        }
    }

    public void MoveDown()
    {
        if (MovementMode == EnMovementMode.Polar)
        {
            MoveRadialRelative(-StepSize);
        }
        else
        {
            Serilog.Log.Logger.ForContext("Name", "Delta2D").Information($"MoveDown(XY): Y - {StepSize}");
            MoveToXY(CurrentX, CurrentY - StepSize);
        }
    }

    private void MoveRadialRelative(double deltaR)
    {
        if (_motorDown?.Data == null || _motorUp?.Data == null) return;

        double ad = _motorDown.EposData.PositionActualGear;
        double au = _motorUp.EposData.PositionActualGear;

        // 1. Získame aktuálny stav (TCP).
        double phi = (ad + au) / 2.0;
        double alphaOld = Math.Abs(ad - au) / 2.0;
        double rTcpOld = CalculateRFromAlpha(alphaOld);

        // Aktuálne GCP polomer
        double rGcpOld = rTcpOld + _offsetGCP;

        // 2. Vypočítame nové GCP R
        double rGcpNew = rGcpOld + deltaR;
        double rTcpNew = rGcpNew - _offsetGCP;

        // Kontrola softvérových limitov pre mechaniku (TCP)
        if (rTcpNew < _minTCP || rTcpNew > _maxTCP)
        {
            Serilog.Log.Logger.Error(
                $"[DELTA] MoveRadial({deltaR:F1}mm) ZAMIETNUTÉ: Cieľová vzdialenosť GCP-R={rGcpNew:F1}mm (TCP-R={rTcpNew:F1}mm) je mimo povoleného rozsahu ({_minTCP:F1} - {_maxTCP:F1} mm).");
            return;
        }

        // Vypocet noveho uhla roztvorenia Alpha z noveho TCP R
        double alphaNew = CalculateAlphaFromR(rTcpNew);

        Serilog.Log.Logger.Information(
            $"[DELTA] MoveRadial({deltaR:F1}mm): GCP-R:{rGcpOld:F1}->{rGcpNew:F1}mm, Phi:{phi:F2}° (Alpha:{alphaOld:F2}->{alphaNew:F2})");

        // 3. Nastavíme motory
        _motorDown.Operation.ProfilePositionMode.MoveToPositionGear(phi + alphaNew, true, true);
        _motorUp.Operation.ProfilePositionMode.MoveToPositionGear(phi - alphaNew, true, true);
    }


    public void WaitForTargetReached(uint timeoutMs)
    {
        // Bezpečnostná kontrola, či sú motory priradené
        if (_motorDown == null || _motorUp == null)
            throw new InvalidOperationException("Motory pre Delta robota nie sú inicializované.");

        long startTime = Environment.TickCount64;

        while (true)
        {
            // --- 1. ATOMICKÉ NAČÍTANIE STAVOV PRE OBA MOTORY ---
            ushort swDown = _motorDown.EposData.Statusword;
            bool wpdoErrorDown = _motorDown.Data.WpdoError;

            ushort swUp = _motorUp.EposData.Statusword;
            bool wpdoErrorUp = _motorUp.Data.WpdoError;

            // --- 2. BITOVÁ EXTRAKCIA PRE MOTOR DOWN ---
            bool enableDown = (swDown & 0x007F) == 0x0037; // Operation Enabled
            bool faultDown = (swDown & 0x0008) == 0x0008; // Bit 3 (Fault)
            bool targetDown = (swDown & 0x0400) == 0x0400; // Bit 10 (Target Reached)
            bool ackDown = (swDown & 0x1000) == 0x1000; // Bit 12 (Setpoint Acknowledge)
            bool folErrorDown = (swDown & 0x2000) == 0x2000; // Bit 13 (Following Error)

            // --- 3. BITOVÁ EXTRAKCIA PRE MOTOR UP ---
            bool enableUp = (swUp & 0x007F) == 0x0037;
            bool faultUp = (swUp & 0x0008) == 0x0008;
            bool targetUp = (swUp & 0x0400) == 0x0400;
            bool ackUp = (swUp & 0x1000) == 0x1000;
            bool folErrorUp = (swUp & 0x2000) == 0x2000;

            // --- 4. FAIL-FAST KONTROLA CHÝB (Okamžitá reakcia na kolíziu) ---

            // Kontrola Motor Down
            if (wpdoErrorDown)
                throw new CDeviceException($"DeltaRobot: Async WPDO Error na motore Down (Node:{_motorDown.NodeId}).",
                    0);
            if (!enableDown)
                throw new CDeviceException($"DeltaRobot: Motor Down (Node:{_motorDown.NodeId}) stratil stav Enable.",
                    0);
            if (faultDown)
                throw new CDeviceException($"DeltaRobot: Motor Down (Node:{_motorDown.NodeId}) je v stave Fault.", 0);
            if (folErrorDown)
                throw new CDeviceException($"DeltaRobot: Following Error na motore Down (Node:{_motorDown.NodeId}).",
                    0);

            // Kontrola Motor Up
            if (wpdoErrorUp)
                throw new CDeviceException($"DeltaRobot: Async WPDO Error na motore Up (Node:{_motorUp.NodeId}).", 0);
            if (!enableUp)
                throw new CDeviceException($"DeltaRobot: Motor Up (Node:{_motorUp.NodeId}) stratil stav Enable.", 0);
            if (faultUp)
                throw new CDeviceException($"DeltaRobot: Motor Up (Node:{_motorUp.NodeId}) je v stave Fault.", 0);
            if (folErrorUp)
                throw new CDeviceException($"DeltaRobot: Following Error na motore Up (Node:{_motorUp.NodeId}).", 0);

            // --- 5. KONTROLA ÚSPEŠNÉHO DOKONČENIA OBOCH MOTOROV ---
            bool downFinished = targetDown && !ackDown;
            bool upFinished = targetUp && !ackUp;

            if (downFinished && upFinished)
            {
                return; // Oba motory úspešne dosiahli cieľ, môžeme pokračovať na ďalší krok
            }

            // --- 6. KONTROLA TIMEOUTU ---
            if (Environment.TickCount64 - startTime > timeoutMs)
            {
                throw new CDeviceException(
                    $"DeltaRobot: Timeout {timeoutMs}ms vypršal. Stav dokončenia -> Down:{downFinished}, Up:{upFinished}",
                    0);
            }

            // --- 7. UVOĽNENIE CPU ---
            // Uspí vlákno na 10ms. Zabezpečí 0% záťaž CPU, ale garantuje reakciu na chybu do 10ms.
            Thread.Sleep(10);
        }
    }

    private double CalculateAlphaFromR(double r)
    {
        // Kosínusová veta pre uhol alpha: cos(alpha) = (R^2 + _l1^2 - _l2^2) / (2 * R * _l1)
        double val = (r * r + _l1 * _l1 - _l2 * _l2) / (2.0 * r * _l1);

        // Ošetrenie limitov (matematická bezpečnosť)
        if (val > 1.0) val = 1.0;
        if (val < -1.0) val = -1.0;

        return Math.Acos(val) * 180.0 / Math.PI;
    }

    private double CalculateRFromAlpha(double alphaDeg)
    {
        double alphaRad = Math.PI * alphaDeg / 180.0;
        return _l1 * Math.Cos(alphaRad) + Math.Sqrt(_l2 * _l2 - Math.Pow(_l1 * Math.Sin(alphaRad), 2));
    }

    private void LogCurrentPolar(string method, double inputVal)
    {
        UpdatePositions();
        Serilog.Log.Logger.ForContext("Name", "Delta2D").Information(
            $"{method}({inputVal}): Ad:{_motorDown.EposData.PositionActualGear:F2}° Au:{_motorUp.EposData.PositionActualGear:F2}° => R:{CurrentR:F2}mm, Phi:{CurrentPhi:F2}°");
    }

    public void UpdatePositions()
    {
        if (_motorDown?.Data == null || _motorUp?.Data == null) return;

        double ad = _motorDown.EposData.PositionActualGear;
        double au = _motorUp.EposData.PositionActualGear;
     
        // Phi je rovnaké pre TCP aj GCP
        CurrentPhi = (ad + au) / 2.0;

        // Výpočet mechanického polomeru (TCP)
        double alphaDeg = Math.Abs(ad - au) / 2.0;
        double rTcp = CalculateRFromAlpha(alphaDeg);

        // Prepisujeme CurrentR tak, aby zobrazoval polohu GCP (stred grippera)
        CurrentR = rTcp + _offsetGCP;

        // Prepočet na karteziánske súradnice [X, Y] pre bod GCP
        double phiRad = CurrentPhi * Math.PI / 180.0;
        CurrentX = CurrentR * Math.Sin(phiRad);
        CurrentY = CurrentR * Math.Cos(phiRad);
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
                catch (Exception)
                {
                }

                await Task.Delay(100, token);
            }
        }, token);
    }

    [RelayCommand]
    public async Task KalibrujAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                if (_motorUp != null && _motorUp.Operation != null)
                {
                    ParametersDelta.RawLH = _motorUp.Operation.HomingMode.GetSSiEncoderActualPositionA();
                }

                if (_motorDown != null && _motorDown.Operation != null)
                {
                    ParametersDelta.RawLD = _motorDown.Operation.HomingMode.GetSSiEncoderActualPositionA();
                }

                // Vypočítaj offsety na základe prečítaných hodnôt
                CalculateAndSetCalibrationOffsets(ParametersDelta.RawLH, ParametersDelta.RawLD);

                // Ulož vypočítané offsety do parametrov (pretypovanie na int kvôli štruktúre json)
                ParametersDelta.OffsetSystem = (int)this.OffsetSystem;
                ParametersDelta.OffsetArm = (int)this.OffsetArm;

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    Log.Information(
                        $"Delta2D: Kalibruj dokončené. RawLH: {ParametersDelta.RawLH}, RawLD: {ParametersDelta.RawLD}, OffsetSystem: {ParametersDelta.OffsetSystem}, OffsetArm: {ParametersDelta.OffsetArm}");
                });
            });
        }
        catch (Exception ex)
        {
            Log.Error($"Delta2D Kalibruj Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task OrientujAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                OrientSystem();

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    ParametersDelta.OffsetSystem = (int)OffsetSystem;
                    Log.Information($"Delta2D: Orientuj dokončené. Nový OffsetSystem: {ParametersDelta.OffsetSystem}");
                });
            });
        }
        catch (Exception ex)
        {
            Log.Error($"Delta2D Orientuj Error: {ex.Message}");
        }
    }
}