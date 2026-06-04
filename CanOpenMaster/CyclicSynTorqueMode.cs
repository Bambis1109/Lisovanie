namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class CyclicSynTorqueMode : CEpos4CommandGroupCO
                {
                    public CyclicSynTorqueMode(ushort keyHandle, byte nodeId, CDataEpos4 data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        BaseData = data;
                        ;
                    }

                    public void ActivateCyclicSyncronicTorqueMode()
                    {
                        SetCurrentMustPercentage(0);
                        SetModeOfOperation(EOperationMode.OmdCyclicSyncronicTorqueMode);
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
                                throw new CDeviceException($"WaitToTorque Node:{NodeId}. Device entered Fault state.",
                                    0);

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
                                    throw new CDeviceException(
                                        $"WaitToTorque Node:{NodeId}. Drive is not responding (0A, 0rpm).", 0);
                                }
                            }
                            else
                            {
                                zeroCounter = 0;
                            }

                            // 6. KONTROLA TIMEOUTU
                            if (Environment.TickCount64 - startTime > timeoutMs)
                            {
                                throw new CDeviceException(
                                    $"WaitToTorque Node:{NodeId}. Timeout {timeoutMs}ms. Actual Torque: {currentActual:0.0}%, Velocity: {velocityActual}.",
                                    0);
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