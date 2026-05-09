using System;

namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class CyclicSynVelocityMode : CCommandGroupCO
                {
                    public CyclicSynVelocityMode(ushort keyHandle, byte nodeId, CDataCO Data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        this.Data = Data;
                    }

                    public VelocityModeAdvanced Advanced { get; }
                    public void SetMaxProfileVelocity(uint velocity)
                    {
                        WritedSDO(0x607F, 0x00, velocity, 4);
                    }
                    public void SetMaxMotorVelocity(uint velocity)
                    {
                        
                        WritedSDO(0x6080, 0x00, velocity, 4);
                    }
                    public void SetProfileVelocity(uint velocity)
                    {
                        WritedSDO(0x6081, 0x00, velocity, 4);
                    }
                    public void SetMaxVelocity(uint velocity)
                    {
                        SetMaxMotorVelocity(velocity);
                        SetMaxProfileVelocity(velocity);
                    }
                    public void ActivateVelocityMode()
                    {
                    }

                    public void GetVelocityMust(ref int velocityMust)
                    {
                    }

                    public void SetVelocityMust(int velocityMust)
                    {
                    }
                }
            }
        }
    }
}