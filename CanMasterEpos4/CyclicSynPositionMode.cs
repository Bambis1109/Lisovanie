namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class CyclicSynPositionMode : CCommandGroupCO
                {
                    public CyclicSynPositionMode(ushort keyHandle, byte nodeId, CDataCO Data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        this.Data = Data;
                    }

                    public CyclicSynPositionModeAdvanced Advanced { get; }

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
                        //var Txdata = new byte[4];
                        //Txdata = BitConverter.GetBytes(positionMust);
                        //WritePDO(4, Txdata);
                    }
                }
            }
        }
    }
}