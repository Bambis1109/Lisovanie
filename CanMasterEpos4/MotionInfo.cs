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
                            // 1. Atomické načítanie stavu
                            ushort sw = Data.Statusword;
                            bool wpdoError = Data.WpdoError;

                            // 2. Bitová extrakcia z jedného snapshotu
                            bool enableState = (sw & 0x007F) == 0x0037;
                            bool faultState = (sw & 0x0008) == 0x0008;
                            bool targetReached = (sw & 0x0400) == 0x0400;
                            bool ack = (sw & 0x1000) == 0x1000;
                            bool followingError = (sw & 0x2000) == 0x2000;

                            // 3. Exaktné vyhodnotenie chybových stavov
                            if (wpdoError)
                                throw new CDeviceException($"WaitForTargetReached Node:{NodeId}. Async WPDO Error on PDO {Data.WpdoErrorPdoNumber}", 0);
        
                            if (!enableState)
                                throw new CDeviceException($"WaitForTargetReached Node:{NodeId}. Device lost Enable state.", 0);
        
                            if (faultState)
                                throw new CDeviceException($"WaitForTargetReached Node:{NodeId}. Device is in Fault state.", 0);
        
                            if (followingError)
                                throw new CDeviceException($"WaitForTargetReached Node:{NodeId}. Following Error occurred.", 0);

                            // 4. Vyhodnotenie úspešného stavu
                            if (targetReached && !ack)
                                return;

                            // 5. Kontrola pretečenia času (bez rizika Integer Overflow)
                            if (Environment.TickCount64 - startTime > timeoutMs)
                                throw new CDeviceException($"WaitForTargetReached Node:{NodeId}. Timeout:{timeoutMs}ms.", 0);

                            // 6. Uvoľnenie CPU (Context Switch)
                            Thread.Sleep(1);
                        }
                    }

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

                    public int PositionActualValueSensor2()
                    {
                        return (int)ReadSdo(0x60E4, 0x02, 4);
                    }
                }
            }
        }
    }
}