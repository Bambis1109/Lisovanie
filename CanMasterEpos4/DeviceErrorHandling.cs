namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class DeviceErrorHandling : CCommandGroupCO
                {
                    public DeviceErrorHandling(ushort keyHandle, byte nodeId, CDataCO Data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        this.Data = Data;
                    }

                    public void GetDeviceErrorCode(byte deviceErrorNumber, ref uint deviceErrorCode)
                    {
                    }

                    public void GetNbOfDeviceError(ref byte deviceError)
                    {
                    }
                }
            }
        }
    }
}