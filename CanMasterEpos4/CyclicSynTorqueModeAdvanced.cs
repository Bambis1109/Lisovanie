namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class CyclicSynTorqueModeAdvanced : CCommandGroupCO
                {
                    public CyclicSynTorqueModeAdvanced(ushort keyHandle, byte nodeId, CDataCO Data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        this.Data = Data;
                    }

                    public void ActivateAnalogCurrentSetpoint(ushort analogInputNumber, float scaling, short offset)
                    {
                    }

                    public void DeactivateAnalogCurrentSetpoint(ushort analogInputNumber)
                    {
                    }

                    public void DisableAnalogCurrentSetpoint()
                    {
                    }

                    public void EnableAnalogCurrentSetpoint()
                    {
                    }
                }
            }
        }
    }
}