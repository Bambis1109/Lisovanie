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
                    public void WaitForTargetReached(uint timeout)
                    {
                        do
                        {
                            timeout -= 1;
                            Thread.Sleep(1);
                        } while (!Data.TargetReached  & (timeout >= 0) & (Data.EnableState));

                        if (timeout < 1)
                        {
                            var Message = string.Format($"WaitForTargetReached  Node:{NodeId}. Timeout:{timeout} TargetReached:{Data.TargetReached} Ack:{Data.Ack}");
                            throw new CDeviceException(Message, 0);
                        }
                        if (Data.FaultState)
                        {
                            var Message = string.Format($"WaitForTargetReached  Node:{NodeId}. Fault");
                            throw new CDeviceException(Message, 0);
                        }
                    }
                    public void WaitForPositionReachedGear(double waitPosition, bool bigger, uint timeout)
                    {
                        if (bigger)
                        {
                            if (waitPosition < Data.PositionActualGear)
                            {
                                Debug.WriteLine($"Node:{NodeId} WaitForPositionReachedGear waitPosition:{waitPosition} is less to actualPosition:{Data.PositionActualGear} bigger:{bigger}");
                            }
                        }
                        else
                        {
                            if (waitPosition > Data.PositionActualGear)
                            {
                                Debug.WriteLine($"Node:{NodeId} WaitForPositionReachedGear waitPosition:{waitPosition} is bigger to actualPosition:{Data.PositionActualGear} bigger:{bigger}");
                            }
                        }
                        bool resultTimeout = false; bool resultEnable = false; bool resultPosition = false; bool resultTargetReached = false;
                        do
                        {
                            resultTimeout = !(timeout > 0); // Ak vyprsi cas = true;
                            resultEnable = !Data.EnableState; //Ak nie je Enable = true
                            resultTargetReached = Data.TargetReached; //Ak dosiahne targetReachet = true;
                            resultPosition = bigger ? (Data.PositionActualGear > waitPosition) : (Data.PositionActualGear < waitPosition); // ak prebehol poziciu = true
                            Thread.Sleep(1);
                        } while (!resultTimeout & !resultTargetReached & !resultPosition & !resultEnable);
                        if (resultTimeout)
                        {
                            throw new CDeviceException($"Node:{NodeId} WaitForPositionReachedGear waitPosition:{waitPosition} actualPosition:{Data.PositionActualGear} bigger:{bigger}. Timeout:{timeout}", 0);
                        }
                        if (resultEnable)
                        {
                            throw new CDeviceException($"Node:{NodeId} WaitForPositionReachedGear waitPosition:{waitPosition} actualPosition:{Data.PositionActualGear} bigger:{bigger}. Is not Enable state", 0);
                        }
                        if (resultTargetReached)
                        {
                            Debug.WriteLine($"Node:{NodeId} WaitForPositionReachedGear waitPosition:{waitPosition} actualPosition:{Data.PositionActualGear} bigger:{bigger}. Target reachet was earlier");
                        }
                    }
                    //public void WaitForTargetReached(uint timeout)
                    //{
                    //  //  WaitForACKSync();
                    //    do
                    //    {
                    //        timeout -= 1;
                    //        Thread.Sleep(1);
                    //    } while (!Data.TargetReached & (timeout > 0) & (Data.EnableState));

                    //    if (timeout < 1)
                    //    {
                    //        throw new CDeviceException($"WaitForTargetReachedSync  Node:{NodeId}. Timeout:{timeout}", 0);
                    //    }
                    //    if (Data.FaultState)
                    //    {
                    //        throw new CDeviceException($"WaitForTargetReachedSync  Node:{NodeId}. Fault", 0);
                    //    }
                    //}
                    public void WaitForHomingAttained(uint timeout)
                    {
                        do
                        {
                            timeout -= 1;
                            Thread.Sleep(1);
                        } while (!Data.Ack & (timeout > 0) );

                        if (timeout < 1)
                        {
                            var message = $"Wait for homing attained   Node:{NodeId:d}. Timeout";
                            throw new CDeviceException(message, 0);
                        }
                        if (Data.FaultState)
                        {
                            throw new CDeviceException($"WaitFor homing attained  Node:{NodeId}. Fault", 0);
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