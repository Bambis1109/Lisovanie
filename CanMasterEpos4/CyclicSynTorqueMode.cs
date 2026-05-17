namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class CyclicSynTorqueMode : CCommandGroupCO
                {
                    public CyclicSynTorqueMode(ushort keyHandle, byte nodeId, CDataCO data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        Data = data;
                    }

                    public void ActivateCurrentMode()
                    {
                        SetModeOfOperation(EOperationMode.OmdCyclicSyncronicTorqueMode);
                        SetCurrentMustPercentage(0);
                        WaitForSetACK(100);
                    }

                    public short GetCurrentMust()
                    {
                        short currentMust = (short)ReadSdo(0x6080, 0x00, 2);
                        return currentMust;
                    }

                    public void SetCurrentMustPercentage(double currentMust)
                    {
                        short value = (short)(currentMust * 10);
                        // WritedSDO(0x6071, 0x00, (ushort)value, 2);
                        WritePDO3TargetTorque(value);
                    }

                    public short GetCurrentPercentage()
                    {
                        return (short)ReadSdo(0x6071, 0x00, 2);
                    }

                    public short GetTorqueActual()
                    {
                        return (short)ReadSdo(0x6077, 0x00, 2);
                    }

/*
                    public void WaitToTorqueStopMovePercentage(int timeout, double torque)
                    {
                        SetCurrentMustPercentage(torque);
                        double currentActual = 0;
                        int velocityActual = 0;
                        int counter = 0;
                        int zero = 0;

                        DateTime timeStart = DateTime.Now;
                        DateTime timeEnd = DateTime.Now.AddMilliseconds(timeout);
                        Thread.Sleep(10);
                        do
                        {
                            Thread.Sleep(10);
                            currentActual = Data.CurrentActualAveragePercentage;
                            velocityActual = Math.Abs(Data.VelocityActual);
                            if ((currentActual == torque) & (velocityActual < 5)) counter++;
                            else counter = 0;
                            if (currentActual == 0 & velocityActual == 0) zero++;
                            else zero = 0;
                            if (zero > 5)
                            {
                                throw new CDeviceException(
                                    $"Node:{NodeId}, Prud={currentActual} , rychlost={velocityActual}Target TorLocal{torque}TargetTorgue{GetCurrentPercentage()} TorgueActua{GetTorqueActual()}  Mode:{Data.ModeOfOperationDisplay} State: {GetStateCommand()}  ZERo ZERO");
                            }

                            if (DateTime.Now > timeEnd)
                            {
                                throw new CDeviceException(
                                    $"Node:{NodeId}, Prud={currentActual} rychlost={velocityActual},Target TorLocal{torque}  Mode:{Data.ModeOfOperationDisplay} State: {GetStateCommand()}  nedosiahnuty doraz, WaitToCurrent timeout {timeout}");
                            }
                        } while (counter <= 2);
                    }
                   
                    public void WaitToTorqueStopMovePercentage(int timeout, double torque)
                    {
                        SetCurrentMustPercentage(torque);
                        int counter = 0;
                        int zero = 0;

                        bool conditionMet = SpinWait.SpinUntil(() =>
                        {
                            double currentActual = Data.CurrentActualAveragePercentage;
                            int velocityActual = Math.Abs(Data.VelocityActual);

                            if (currentActual == torque && velocityActual < 5) counter++;
                            else counter = 0;
                            if (currentActual == 0 && velocityActual == 0) zero++;
                            else zero = 0;

                            if (zero > 5)
                            {
                                throw new CDeviceException(
                                    $"Node:{NodeId}, Prud={currentActual}, rychlost={velocityActual}. ZERO ZERO");
                            }

                            return counter > 2 || Data.FaultState || Data.WpdoError;
                        }, timeout);

                        if (!conditionMet)
                            throw new CDeviceException($"Node:{NodeId}, Timeout {timeout}ms. Nedosiahnuty doraz.");
                        if (Data.FaultState)
                            throw new CDeviceException($"Node:{NodeId}, Device Fault.");
                        if (Data.WpdoError)
                            throw new CDeviceException($"Node:{NodeId}, WPDO Error.");
                    }
                     */

public void WaitToTorqueStopMovePercentage(int timeoutMs, double targetTorque)
{
    // 1. Zápis požadovaného momentu (Cez PDO)
    SetCurrentMustPercentage(targetTorque);

    int stableCounter = 0;
    int zeroCounter = 0;
    long startTime = Environment.TickCount64;

    // Tolerancia pre prúd (napr. 1.0 % z nominálneho prúdu) kvôli šumu A/D prevodníka
    double torqueTolerance = 1.0; 
    // Tolerancia pre rýchlosť (šum enkodéra pri stlačení)
    int velocityDeadband = 5; 

    while (true)
    {
        // 2. ATOMICKÉ NAČÍTANIE STAVOV (Snapshot)
        // Načítame všetky potrebné premenné naraz, aby sme predišli Torn State
        double currentActual = Data.CurrentActualAveragePercentage;
        int velocityActual = Math.Abs(Data.VelocityActual);
        ushort sw = Data.Statusword;
        bool wpdoError = Data.WpdoError;

        bool faultState = (sw & 0x0008) == 0x0008;

        // 3. KONTROLA CHÝB
        if (faultState)
            throw new CDeviceException($"WaitToTorque Node:{NodeId}. Device entered Fault state.", 0);
        
        if (wpdoError)
            throw new CDeviceException($"WaitToTorque Node:{NodeId}. Async WPDO Error.", 0);

        // 4. VYHODNOTENIE USTÁLENIA (S tolerančným pásmom)
        // Skontrolujeme, či je prúd blízko žiadanej hodnoty A ZÁROVEŇ sa motor netočí
        bool isTorqueReached = Math.Abs(currentActual - targetTorque) <= torqueTolerance;
        bool isMotorStopped = velocityActual <= velocityDeadband;

        if (isTorqueReached && isMotorStopped)
        {
            stableCounter++;
        }
        else
        {
            stableCounter = 0; // Reset počítadla, ak mechanika odskočí
        }

        // Ak je stav stabilný po dobu 3 cyklov (cca 30 ms), považujeme hmat za úspešný
        if (stableCounter >= 3)
        {
            return; // Úspešné ukončenie
        }

        // 5. BEZPEČNOSTNÁ POISTKA (Zero check)
        // Ak je prúd 0 a rýchlosť 0, ale my žiadame ťah, niečo je zle (napr. odpojený motor)
        if (currentActual == 0 && velocityActual == 0 && targetTorque != 0)
        {
            zeroCounter++;
            if (zeroCounter > 10) // Tolerujeme krátky nábeh (cca 100ms)
            {
                throw new CDeviceException($"WaitToTorque Node:{NodeId}. Drive is not responding (0A, 0rpm).", 0);
            }
        }
        else
        {
            zeroCounter = 0;
        }

        // 6. KONTROLA TIMEOUTU
        if (Environment.TickCount64 - startTime > timeoutMs)
        {
            throw new CDeviceException($"WaitToTorque Node:{NodeId}. Timeout {timeoutMs}ms. Actual Torque: {currentActual:0.0}%, Velocity: {velocityActual}.", 0);
        }

        // 7. UVOĽNENIE CPU (Makro-čakanie)
        // Mechanické stlačenie trvá desiatky milisekúnd. 10ms sleep je ideálny.
        Thread.Sleep(10);
    }
}
                }
            }
        }
    }
}