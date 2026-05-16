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

                    public void WaitForTargetReached(uint timeout)
                    {
                        bool targetReachedSuccessfully = false;
                        bool faultOccurred = false;

                        bool conditionMet = SpinWait.SpinUntil(() =>
                        {
                            // Kontrola straty stavu Enable alebo asynchrónnej chyby zápisu
                            if (!Data.EnableState || Data.WpdoError)
                            {
                                return true;
                            }

                            // Vetva B: Neúspešné ukončenie (Following Error / Fault)
                            // Podmienka: Bit 3 == 1 && Bit 13 == 1
                            if (Data.FaultState && Data.FollowingError)
                            {
                                faultOccurred = true;
                                return true;
                            }

                            // Vetva A: Úspešné ukončenie (Target Reached)
                            // Podmienka: Bit 3 == 0 && Bit 13 == 0 && Bit 10 == 1 && Bit 12 == 0
                            if (!Data.FaultState && !Data.FollowingError && Data.TargetReached && !Data.Ack)
                            {
                                targetReachedSuccessfully = true;
                                return true;
                            }

                            return false;
                        }, (int)timeout);

                        // Vyhodnotenie dôvodu ukončenia SpinWait
                        if (!conditionMet)
                        {
                            throw new CDeviceException($"WaitForTargetReached Node:{NodeId}. Timeout:{timeout}ms.", 0);
                        }

                        if (Data.WpdoError)
                        {
                            throw new CDeviceException(
                                $"WaitForTargetReached Node:{NodeId}. Async WPDO Error on PDO {Data.WpdoErrorPdoNumber}",
                                0);
                        }

                        if (!Data.EnableState)
                        {
                            throw new CDeviceException($"WaitForTargetReached Node:{NodeId}. Device lost Enable state.",
                                0);
                        }

                        if (faultOccurred)
                        {
                            throw new CDeviceException(
                                $"WaitForTargetReached Node:{NodeId}. Following Error / Fault occurred.", 0);
                        }

                        if (!targetReachedSuccessfully)
                        {
                            // Fallback pre neočakávané stavy
                            throw new CDeviceException($"WaitForTargetReached Node:{NodeId}. Unknown exit condition.",
                                0);
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