namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class VelocityModeAdvanced : CCommandGroupCO
                {
                    public VelocityModeAdvanced(ushort keyHandle, byte nodeId, CDataCO Data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        this.Data = Data;
                    }

                    public void ActivateAnalogVelocitySetpoint(ushort analogInputNumber, float scaling, int offset)
                    {
                    }

                    public void DeactivateAnalogVelocitySetpoint(ushort analogInputNumber)
                    {
                    }

                    public void DisableAnalogVelocitySetpoint()
                    {
                    }

                    public void EnableAnalogVelocitySetpoint()
                    {
                    }
                }
            }
        }
    }
}