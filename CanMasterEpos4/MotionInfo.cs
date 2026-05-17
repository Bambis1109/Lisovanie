namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class MotionInfo : CCommandGroupCO
                {
                    public MotionInfo(ushort keyHandle, byte nodeId, CDataCO data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        Data = data;
                    }

                    public int GetCurrentIs() => (int)ReadSdo(0x6078, 0x00, 4);
                    public int GetCurrentIsAveraged() => (int)ReadSdo(0x2027, 0x00, 4);
                    public int GetPositionIs() => (int)ReadSdo(0x6064, 0x00, 4);
                    public int GetFwVersion() => (int)ReadSdo(0x1F56, 0x01, 4);
                    public double GetPositionGearIs() => GetPositionIs() / Data.Gear;
                    public int GetVelocityIs() => (int)ReadSdo(0x606C, 0x00, 4);
                    public int GetVelocityIsAveraged() => (int)ReadSdo(0x2028, 0x00, 4);


                    public void WaitForTargetReached(uint timeoutMs)
                    {
                        long startTime = Environment.TickCount64;

                        while (true)
                        {
                            // 1. ATOMICKÉ NAČÍTANIE STAVU (Zamedzenie Torn State)
                            ushort sw = Data.Statusword;
                            bool wpdoError = Data.WpdoError;

                            // 2. BITOVÁ EXTRAKCIA Z JEDNÉHO SNAPSHOTU
                            bool enableState = (sw & 0x007F) == 0x0037; // Operation Enabled
                            bool faultState = (sw & 0x0008) == 0x0008; // Bit 3
                            bool targetReached = (sw & 0x0400) == 0x0400; // Bit 10
                            bool ack = (sw & 0x1000) == 0x1000; // Bit 12 (Setpoint Acknowledge)
                            bool followingError = (sw & 0x2000) == 0x2000; // Bit 13

                            // 3. EXAKTNÉ VYHODNOTENIE CHYBOVÝCH STAVOV (Safety First)
                            if (wpdoError)
                                throw new CDeviceException(
                                    $"WaitForTargetReached Node:{NodeId}. Async WPDO Error on PDO {Data.WpdoErrorPdoNumber}",
                                    0);

                            if (!enableState)
                                throw new CDeviceException(
                                    $"WaitForTargetReached Node:{NodeId}. Device lost Enable state.", 0);

                            if (faultState)
                                throw new CDeviceException(
                                    $"WaitForTargetReached Node:{NodeId}. Device is in Fault state.", 0);

                            if (followingError)
                                throw new CDeviceException(
                                    $"WaitForTargetReached Node:{NodeId}. Following Error occurred.", 0);

                            // 4. VYHODNOTENIE ÚSPEŠNÉHO STAVU
                            // EPOS4 PPM: Target Reached = 1 a ACK = 0 (pohyb je dokončený a nový setpoint nie je spracovávaný)
                            if (targetReached && !ack)
                                return;

                            // 5. KONTROLA PRETEČENIA ČASU (Bez rizika Integer Overflow)
                            if (Environment.TickCount64 - startTime > timeoutMs)
                                throw new CDeviceException(
                                    $"WaitForTargetReached Node:{NodeId}. Timeout:{timeoutMs}ms.", 0);

                            // 6. UVOĽNENIE CPU PRE OS (Makro-čakanie)
                            // 10 ms je ideálny kompromis medzi reaktivitou a nulovou záťažou CPU.
                            Thread.Sleep(5);
                        }
                    }


                    public void WaitForHomingAttained(uint timeoutMs)
                    {
                        long startTime = Environment.TickCount64;

                        while (true)
                        {
                            // 1. ATOMICKÉ NAČÍTANIE STAVU
                            ushort sw = Data.Statusword;
                            bool wpdoError = Data.WpdoError;

                            // 2. BITOVÁ EXTRAKCIA
                            bool enableState = (sw & 0x007F) == 0x0037;
                            bool faultState = (sw & 0x0008) == 0x0008;
                            bool targetReached = (sw & 0x0400) == 0x0400; // Bit 10
                            bool homingAttained = (sw & 0x1000) == 0x1000; // Bit 12
                            bool homingError = (sw & 0x2000) == 0x2000; // Bit 13

                            // 3. CHYBOVÉ STAVY
                            if (wpdoError)
                                throw new CDeviceException(
                                    $"WaitForHomingAttained Node:{NodeId}. Async WPDO Error on PDO {Data.WpdoErrorPdoNumber}",
                                    0);

                            if (!enableState)
                                throw new CDeviceException(
                                    $"WaitForHomingAttained Node:{NodeId}. Device lost Enable state.", 0);

                            if (faultState)
                                throw new CDeviceException(
                                    $"WaitForHomingAttained Node:{NodeId}. Device is in Fault state.", 0);

                            if (homingError)
                                throw new CDeviceException(
                                    $"WaitForHomingAttained Node:{NodeId}. Homing Error occurred (Bit 13).", 0);

                            // 4. ÚSPEŠNÝ STAV
                            // EPOS4 Homing: Bit 12 (Homing Attained) = 1 AND Bit 10 (Target Reached) = 1
                            if (homingAttained && targetReached)
                                return;

                            // 5. TIMEOUT
                            if (Environment.TickCount64 - startTime > timeoutMs)
                                throw new CDeviceException(
                                    $"WaitForHomingAttained Node:{NodeId}. Timeout:{timeoutMs}ms.", 0);

                            // 6. UVOĽNENIE CPU
                            Thread.Sleep(5);
                        }
                    }

/*
                    public void WaitForHomingAttained(uint timeout)
                    {
                        bool homingSuccessfullyCompleted = false;
                        bool homingErrorOccurred = false;

                        bool conditionMet = SpinWait.SpinUntil(() =>
                        {
                            // Kontrola straty stavu Enable, asynchrónnej chyby zápisu alebo globálneho Faultu
                            if (!Data.EnableState || Data.WpdoError || Data.FaultState)
                            {
                                return true;
                            }

                            // Vetva B: Homing Error (Bit 13 == 1)
                            if (Data.HomingError)
                            {
                                homingErrorOccurred = true;
                                return true;
                            }

                            // Vetva A: Homing úspešne ukončený (Bit 13 == 0 && Bit 12 == 1 && Bit 10 == 1)
                            if (!Data.HomingError && Data.HomingAttained && Data.TargetReached)
                            {
                                homingSuccessfullyCompleted = true;
                                return true;
                            }

                            return false;
                        }, (int)timeout);

                        // Vyhodnotenie dôvodu ukončenia SpinWait
                        if (!conditionMet)
                        {
                            throw new CDeviceException($"WaitForHomingAttained Node:{NodeId}. Timeout:{timeout}ms.", 0);
                        }

                        if (Data.WpdoError)
                        {
                            throw new CDeviceException(
                                $"WaitForHomingAttained Node:{NodeId}. Async WPDO Error on PDO {Data.WpdoErrorPdoNumber}",
                                0);
                        }

                        if (!Data.EnableState)
                        {
                            throw new CDeviceException(
                                $"WaitForHomingAttained Node:{NodeId}. Device lost Enable state.", 0);
                        }

                        if (Data.FaultState)
                        {
                            throw new CDeviceException(
                                $"WaitForHomingAttained Node:{NodeId}. Device is in Fault state.", 0);
                        }

                        if (homingErrorOccurred)
                        {
                            throw new CDeviceException(
                                $"WaitForHomingAttained Node:{NodeId}. Homing Error occurred (Bit 13).", 0);
                        }

                        if (!homingSuccessfullyCompleted)
                        {
                            throw new CDeviceException($"WaitForHomingAttained Node:{NodeId}. Unknown exit condition.",
                                0);
                        }
                    }
*/
                    public int PositionActualValueSensor2()
                    {
                        return (int)ReadSdo(0x60E4, 0x02, 4);
                    }
                }
            }
        }
    }
}