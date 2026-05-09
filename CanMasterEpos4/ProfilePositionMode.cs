using System;

namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class ProfilePositionMode : CCommandGroupCO
                {
                    public ProfilePositionMode(ushort keyHandle, byte nodeId, CDataCO Data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        this.Data = Data;
                    }
                    public ProfilePositionModeAdvanced Advanced { get; }
                    public void ActivateProfilePositionMode()
                    {
                        SetModeOfOperation(EOperationMode.OmdProfilePositionMode);
                        SetEnableState();
                        WaitForResetACK(100);
                    }

                    public void GetPositionProfile(ref uint profileVelocity, ref uint profileAcceleration, ref uint profileDeceleration)
                    {
                        profileVelocity = (uint)ReadSdo(0x6081, 0x00, 4);
                        profileAcceleration = (uint)ReadSdo(0x6083, 0x00, 4);
                        profileDeceleration = (uint)ReadSdo(0x6084, 0x00, 4);
                    }
                    public int GetTargetPosition() => (int)ReadSdo(0x607a, 0x00, 4);
                    public void SetTargetPosition(int targetPosition)
                    {
                        try
                        {
                            //   WritedSDO(0x607a, 0x00, (ulong)targetPosition, 4);
                            WritePDO4TargetPosition(targetPosition);
                        }
                        catch (CDeviceException e)
                        {
                            throw new CDeviceException($"SetTargetPositiion: {e.ErrorMessage}", 0);
                        }
                    }
                    public void HaltPositionMovement() => throw new CDeviceException($"Not implementation!", 0);
                    public void MoveToPosition(int targetPosition, bool absolute, bool immediately)
                    {
                        try
                        {
                            TestBeforSetPointPpm();
                            SetTargetPosition(targetPosition);
                            if (absolute)
                            {
                                if (immediately) SetControlword(0x3f);
                                else SetControlword(0x1f);
                            }
                            else
                            {
                                if (immediately) SetControlword(0x7f);
                                else SetControlword(0x5f);
                            }

                            WaitForACK();
                        }
                        catch (CDeviceException e)
                        {
                            throw new CDeviceException($"MoveToPosition:{e.ErrorMessage}", 0);
                        }
                    }
                    public void MoveToPositionSync(int targetPosition, bool absolute, bool immediately)
                    {
                        try
                        {
                            TestBeforSetPointPpm();
                            SetTargetPosition(targetPosition);
                            if (absolute)
                            {
                                if (immediately) SetControlword(0x3f);
                                else SetControlword(0x1f);
                            }
                            else
                            {
                                if (immediately) SetControlword(0x7f);
                                else SetControlword(0x5f);
                            }
                        }
                        catch (CDeviceException e)
                        {
                            throw new CDeviceException($"MoveToPosition:{e.ErrorMessage}", 0);
                        }
                    }
                    public void MoveToPositionGear(double targetPosition, bool absolute, bool immediately)
                    {
                        try
                        {
                            TestBeforSetPointPpm();
                            SetTargetPosition((int)(targetPosition * Data.Gear));
                            if (absolute)
                            {
                                if (immediately) SetControlword(0x23f);
                                else SetControlword(0x21f);
                            }
                            else
                            {
                                if (immediately) SetControlword(0x27f);
                                else SetControlword(0x25f);
                            }
                            WaitForACK();
                        }
                        catch (CDeviceException e)
                        {
                            throw new CDeviceException($"MoveToPositionGear:{e.ErrorMessage}", 0);
                        }
                    }
                    public void MoveToPositionGearSync(double targetPosition, bool absolute, bool immediately)
                    {
                        try
                        {
                            TestBeforSetPointPpm();
                            SetTargetPosition((int)(targetPosition * Data.Gear));
                            if (absolute)
                            {
                                if (immediately) SetControlwordSync(0x23f);
                                else SetControlwordSync(0x21f);
                            }
                            else
                            {
                                if (immediately) SetControlwordSync(0x27f);
                                else SetControlwordSync(0x25f);
                            }
                        }
                        catch (CDeviceException e)
                        {
                            throw new CDeviceException($"MoveToPosition:{e.ErrorMessage}", 0);
                        }
                    }
                    public void SetPositionProfile(uint profileVelocity, uint profileAcceleration, uint profileDeceleration)
                    {
                        WritedSDO(0x6081, 0x00, profileVelocity, 4);
                        WritedSDO(0x6083, 0x00, profileAcceleration, 4);
                        WritedSDO(0x6084, 0x00, profileDeceleration, 4);
                    }
                    public void SetPositionProfileGear(double profileVelocity, double profileAcceleration, double profileDeceleration)
                    {
                        double om = Data.Gear / Data.Pulse;

                        WritedSDO(0x6081, 0x00, (ulong)Math.Round(profileVelocity * 60 * om), 4);
                        WritedSDO(0x6083, 0x00, (ulong)Math.Round(profileAcceleration * om), 4);
                        WritedSDO(0x6084, 0x00, (ulong)Math.Round(profileDeceleration * om), 4);
                    }
                    private void TestBeforSetPointPpm()
                    {
                        if (Data.ModeOfOperationDisplay != EOperationMode.OmdProfilePositionMode)
                            throw new CDeviceException($"Node {NodeId:d}: Operation mode is not PPM [{Data.ModeOfOperationDisplay}]", 0);
                        var stateDevice = GetStateCommand();
                        if (stateDevice != EStates.StEnabled) throw new CDeviceException($"Node {NodeId:d}: State of device is not Enable [{stateDevice}]", 0);
                        if (Data.Ack) throw new CDeviceException($"Ack is not reset.  Node:{NodeId:d}", 0);
                    }

                }
            }
        }
    }
}