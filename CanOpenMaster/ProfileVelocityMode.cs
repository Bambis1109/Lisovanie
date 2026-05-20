namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class ProfileVelocityMode : CEpos4CommandGroupCO
                {
                    public ProfileVelocityMode(ushort keyHandle, byte nodeId, CDataEpos4 data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        BaseData = data;
                    }

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