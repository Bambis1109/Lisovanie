namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class ProfileVelocityModeAdvanced : CCommandGroupCO
                {
                    public ProfileVelocityModeAdvanced(ushort keyHandle, byte nodeId, CDataCO Data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        this.Data = Data;
                    }

                    public void DisableVelocityWindow()
                    {
                    }

                    public void EnableVelocityWindow(uint velocityWindow, ushort velocityWindowTime)
                    {
                    }
                }
            }
        }
    }
}