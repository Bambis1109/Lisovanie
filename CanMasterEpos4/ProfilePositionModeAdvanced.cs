namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class ProfilePositionModeAdvanced : CCommandGroupCO
                {
                    public ProfilePositionModeAdvanced(ushort keyHandle, byte nodeId, CDataCO Data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        this.Data = Data;
                    }

                    public void DisablePositionWindow()
                    {
                    }

                    public void EnablePositionWindow(uint positionWindow, ushort positionWindowTime)
                    {
                    }
                }
            }
        }
    }
}