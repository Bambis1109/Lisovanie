namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class ProfileVelocityMode : CCommandGroupCO
                {
                    public ProfileVelocityMode(ushort keyHandle, byte nodeId, CDataCO Data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        this.Data = Data;
                    }

                    public ProfileVelocityModeAdvanced Advanced { get; }

                    public void ActivateProfileVelocityMode()
                    {
                    }

                    public int GetTargetVelocity()
                    {
                        return 0;
                        ;
                    }

                    public void GetVelocityProfile(ref uint profileAcceleration, ref uint profileDeceleration)
                    {
                    }

                    public void HaltVelocityMovement()
                    {
                    }

                    public void MoveWithVelocity(int targetVelocity)
                    {
                    }

                    public void SetVelocityProfile(uint profileAcceleration, uint profileDeceleration)
                    {
                    }
                }
            }
        }
    }
}