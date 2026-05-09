namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class CyclicSynPositionModeAdvanced : CCommandGroupCO
                {
                    public CyclicSynPositionModeAdvanced(ushort keyHandle, byte nodeId, CDataCO Data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        this.Data = Data;
                    }

                    public void ActivateAnalogPositionSetpoint(ushort analogInputNumber, float scaling, int offset)
                    {
                    }

                    public void DeactivateAnalogPositionSetpoint(ushort analogInputNumber)
                    {
                    }

                    public void DisableAnalogPositionSetpoint()
                    {
                    }

                    public void EnableAnalogPositionSetpoint()
                    {
                    }
                }
            }
        }
    }
}