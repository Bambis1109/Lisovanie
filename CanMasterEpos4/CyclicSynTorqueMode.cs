using System;
using System.Threading;

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

                    public CyclicSynTorqueModeAdvanced Advanced { get; }

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

                    //public void SetCurrentMust(double currentMust)
                    //{
                    //    short value = (short)(currentMust * 10);
                    //    //  WritedSDO(0x6071, 0x00, (ushort)value, 2);
                    //    WritePDO4TargetTorque(value);
                    //}
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

                    //public void WaitToTorqueStopMove(int timeout, double torque)
                    //{
                    //    SetCurrentMustPercentage(torque);

                    //    int currentActual = 0;
                    //    int velocityActual = 0;
                    //    bool repeat = false;
                    //    bool exit = false;

                    //    DateTime timeStart = DateTime.Now;
                    //    DateTime timeEnd = DateTime.Now.AddMilliseconds(timeout);
                    //    do
                    //    {
                    //        currentActual = Data.CurrentActualAverage;
                    //        Thread.Sleep(10);
                    //        velocityActual = Data.VelocityActual;
                    //        exit = (currentActual != 0) & (velocityActual == 0);
                    //        if (currentActual == 0 & velocityActual == 0)
                    //        {
                    //            throw new CDeviceException($"Node:{NodeId}, Prud=0 aj rychlost=0  pozadovana sila je {torque}% - nedosiahnuty doraz");
                    //        }

                    //        if (DateTime.Now > timeEnd) throw new CDeviceException($"Node:{NodeId}, WaitToCurrent timeout {timeout}");
                    //        repeat = !exit;
                    //    } while (repeat);
                    //}
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
                            if ((currentActual == torque) & (velocityActual < 5)) counter++; else counter = 0;
                            if (currentActual == 0 & velocityActual == 0) zero++; else zero = 0;
                            if (zero > 5)
                            {
                                throw new CDeviceException($"Node:{NodeId}, Prud={currentActual} , rychlost={velocityActual}Target TorLocal{torque}TargetTorgue{GetCurrentPercentage()} TorgueActua{GetTorqueActual()}  Mode:{Data.ModeOfOperationDisplay} State: {GetStateCommand()}  ZERo ZERO");
                            }
                            if (DateTime.Now > timeEnd)
                            {
                                throw new CDeviceException($"Node:{NodeId}, Prud={currentActual} rychlost={velocityActual},Target TorLocal{torque}  Mode:{Data.ModeOfOperationDisplay} State: {GetStateCommand()}  nedosiahnuty doraz, WaitToCurrent timeout {timeout}");
                            }
                        } while (counter <= 2);
                    }
                }
            }
        }
    }
}