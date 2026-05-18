namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class CyclicSynPositionMode : CEpos4CommandGroupCO
                {
                    public CyclicSynPositionMode(ushort keyHandle, byte nodeId, CDataEpos4 data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        BaseData = data;
                    }
                  
                    public void ActivateCyclicSynPositionMode()
                    {
                        SetModeOfOperation(EOperationMode.OmdCyclicSynchronousPositionMode);
                    }

                    public int GetPositionMust(int positionMust)
                    {
                        return (int)ReadSdo(0x2062, 0x00, 4);
                    }

                    public void SetPositionMust(int positionMust)
                    {
                        WritedSDO(0x2062, 0x00, (ulong)positionMust, 4);
                      
                    }
                }
            }
        }
    }
}