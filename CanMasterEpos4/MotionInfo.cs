using EposCmd.Net.DeviceCmdSet.DataRecorder;
using System.Diagnostics;
using System.Threading;

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

                    // OPRAVA 1: Použitie SpinWait pre bezpečný timeout
                    public void WaitForTargetReached(uint timeout)
                    {
                        // Čakáme kým nenastane jedna z podmienok:
                        // 1. Target Reached (Bit 10) je true
                        // 2. Motor stratil Enable (napr. spadol do QuickStop alebo Disable)
                        // 3. Motor hlási Fault (Bit 3)
                        bool conditionMet = SpinWait.SpinUntil(() => 
                            Data.TargetReached || !Data.EnableState || Data.FaultState, (int)timeout);

                        if (!conditionMet)
                        {
                            var Message = $"WaitForTargetReached Node:{NodeId}. Timeout:{timeout}ms. TargetReached:{Data.TargetReached} Ack:{Data.Ack}";
                            throw new CDeviceException(Message, 0);
                        }

                        if (Data.FaultState)
                        {
                            var Message = $"WaitForTargetReached Node:{NodeId}. Device is in Fault state.";
                            throw new CDeviceException(Message, 0);
                        }
                    }

                    // OPRAVA 2: Pridané odpočítavanie timeoutu a oprava logických operátorov
                    public void WaitForPositionReachedGear(double waitPosition, bool bigger, uint timeout)
                    {
                        if (bigger && waitPosition < Data.PositionActualGear)
                        {
                            Debug.WriteLine($"Node:{NodeId} WaitForPositionReachedGear waitPosition:{waitPosition} is less to actualPosition:{Data.PositionActualGear} bigger:{bigger}");
                        }
                        else if (!bigger && waitPosition > Data.PositionActualGear)
                        {
                            Debug.WriteLine($"Node:{NodeId} WaitForPositionReachedGear waitPosition:{waitPosition} is bigger to actualPosition:{Data.PositionActualGear} bigger:{bigger}");
                        }

                        bool conditionMet = SpinWait.SpinUntil(() =>
                        {
                            bool resultEnable = !Data.EnableState;
                            bool resultTargetReached = Data.TargetReached;
                            bool resultPosition = bigger ? (Data.PositionActualGear > waitPosition) : (Data.PositionActualGear < waitPosition);
                            
                            return resultEnable || resultTargetReached || resultPosition;
                        }, (int)timeout);

                        if (!conditionMet)
                        {
                            throw new CDeviceException($"Node:{NodeId} WaitForPositionReachedGear waitPosition:{waitPosition} actualPosition:{Data.PositionActualGear} bigger:{bigger}. Timeout:{timeout}ms", 0);
                        }
                        
                        if (!Data.EnableState)
                        {
                            throw new CDeviceException($"Node:{NodeId} WaitForPositionReachedGear waitPosition:{waitPosition} actualPosition:{Data.PositionActualGear} bigger:{bigger}. Is not Enable state", 0);
                        }
                        
                        if (Data.TargetReached)
                        {
                            Debug.WriteLine($"Node:{NodeId} WaitForPositionReachedGear waitPosition:{waitPosition} actualPosition:{Data.PositionActualGear} bigger:{bigger}. Target reached was earlier");
                        }
                    }

                    public void WaitForHomingAttained(uint timeout)
                    {
                        bool conditionMet = SpinWait.SpinUntil(() => Data.Ack || Data.FaultState, (int)timeout);

                        if (!conditionMet)
                        {
                            var message = $"Wait for homing attained Node:{NodeId:d}. Timeout";
                            throw new CDeviceException(message, 0);
                        }
                        if (Data.FaultState)
                        {
                            throw new CDeviceException($"WaitFor homing attained Node:{NodeId}. Fault", 0);
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