using System.Threading;

namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class HomingMode : CCommandGroupCO
                {
                    public HomingMode(ushort keyHandle, byte nodeId, CDataCO Data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        this.Data = Data;
                    }
                    public void ActivateHomingMode()
                    {
                        SetModeOfOperation(EOperationMode.OmdHomingMode);
                        SetEnableState();
                        WaitForResetACK(100);
                    }
                    public void DefinePosition(int homePosition)
                    {
                        WritedSDO(0x2081, 0x00, (uint)homePosition, 4); // home position
                        FindHome(EHomingMethod.HmActualPosition);
                    }
                    public void FindHome(EHomingMethod homingMethod)
                    {
                        WritedSDO(0x6098, 0x00, (byte)homingMethod, 1);
                        SetControlword(0x1F); // Set start home
                    }
                    private void TestStateDeviceForFindHome()
                    {
                        var operationMode = GetModeOfOperation();
                        if (operationMode != EOperationMode.OmdHomingMode)
                        {
                            var Message = string.Format("Find home Node {0:d}:  Operation mode is [{1}]", NodeId,
                                operationMode);
                            throw new CDeviceException(Message, 0);
                        }

                        var StateDevice = GetStateCommand();
                        if (StateDevice != EStates.StEnabled)
                        {
                            var Message =
                                string.Format(
                                    "Find home Node {0:d}: State of device is {1}. Device need  Enable state] ", NodeId,
                                    StateDevice);
                            throw new CDeviceException(Message, 0);
                        }
                    }

                    public void GetHomingParameter(ref uint homingAcceleration, ref uint speedSwitch,
                        ref uint speedIndex, ref int homeOffset, ref ushort currentTreshold, ref int homePosition)
                    {
                        homingAcceleration = (uint)ReadSdo(0x609a, 0x00, 4); // homing acceleratioin
                        speedSwitch = (uint)ReadSdo(0x6099, 0x01, 4); //speed switch
                        speedIndex = (uint)ReadSdo(0x6099, 0x02, 4); //speed index
                        homeOffset = (int)ReadSdo(0x607c, 0x00, 4); // home ofset
                        currentTreshold = (ushort)ReadSdo(0x6080, 0x00, 2); //curent treshold
                        homePosition = (int)ReadSdo(0x6081, 0x00, 4); // home position
                    }
                    public void FindHome()
                    {
                        TestStateDeviceForFindHome();
                        //  WritedSDO(0x6098, 0x00, (byte)homingMethod, 1);
                        SetControlword(0x1F); // Set start home
                        Thread.Sleep(10);
                    }
                    public void GetHomingState(ref bool homingAttained, ref bool homingError)
                    {
                    }
                    public int GetSSiEncoderActualPositionA()
                    {
                        return (int)ReadSdo(0x3012, 0x09, 4);
                    }
                    public int GetSSiEncoderActualPosition()
                    {
                        return (int)ReadSdo(0x2211, 0x03, 4);
                    }
                    public void SetHomingParameter(uint homingAcceleration, uint speedSwitch, uint speedIndex,
                        int homeOffset, ushort currentTreshold, int homePosition, EHomingMethod homingMethod)
                    {
                        var y = Data.Statusword;
                        WritedSDO(0x609a, 0x00, homingAcceleration, 4); // homing acceleratioin
                        WritedSDO(0x6099, 0x01, speedSwitch, 4); //speed switch
                        WritedSDO(0x6099, 0x02, speedIndex, 4); //speed index
                        WritedSDO(0x30B1, 0x00, (ulong)homeOffset, 4); // home ofset
                        WritedSDO(0x30B2, 0x00, currentTreshold, 2); //curent treshold
                        WritedSDO(0x30B0, 0x00, (ulong)homePosition, 4); // home position
                        WritedSDO(0x6098, 0x00, (byte)homingMethod, 1); //Homing metod
                        var x = Data.Statusword;
                    }
                    public void StopHoming()
                    {
                        SetEnableStateAndWait();
                    }
                }
            }
        }
    }
}