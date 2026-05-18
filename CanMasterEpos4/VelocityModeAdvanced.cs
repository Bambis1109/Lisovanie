namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class VelocityModeAdvanced : CEpos4CommandGroupCO
                {
                    public VelocityModeAdvanced(ushort keyHandle, byte nodeId, CDataEpos4 data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        BaseData = Data;
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