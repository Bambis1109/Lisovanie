using System.Text;

namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Initialization
            {
                public class Initialization : CCommandGroupCO
                {
                    public Initialization(ushort keyHandle, byte nodeId)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                    }

                    public InitializationAdvanced Advanced { get; }

                    public void GetDriverInfo(ref string libraryName, ref string libraryVersion)
                    {
                    }

                    public void GetErrorInfo(uint errorCodeValue, ref string errofInfo, ushort maxStrSize)
                    {
                    }

                    public void GetVersion(ref ushort hardwareVersion, ref ushort softwareVersion,
                        ref ushort applicationNumber, ref ushort applicationVersion)
                    {
                    }
                }

                public class InitializationAdvanced : CCommandGroupCO
                {
                    public InitializationAdvanced(ushort keyHandle, byte nodeId)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                    }

                    public uint GetBaudrateSelection(string deviceName, string protocolStackName, string interfaceName,
                        string portName, int startOfSelection, ref int endOfSelection)
                    {
                        return 0;
                    }

                    public void GetDeviceName(StringBuilder deviceName, ushort maxStrSize)
                    {
                    }

                    public void GetDeviceNameSelection(int startOfSelection, ref string deviceNameSel,
                        ref int endOfSelection)
                    {
                    }

                    public void GetInterfaceName(StringBuilder interfaceName, ushort maxStrSize)
                    {
                    }

                    public void GetInterfaceNameSelection(string deviceName, string protocolStackName,
                        int startOfSelection, ref string interfaceNameSel, ref int endOfSelection)
                    {
                    }

                    public void GetKeyHandle(string deviceName, string protocolStackName, string interfaceName,
                        string portName, ref int keyHandle)
                    {
                    }

                    public void GetPortName(StringBuilder portName, ushort maxStrSize)
                    {
                    }

                    public void GetPortNameSelection(string deviceName, string protocolStackName, string interfaceName,
                        int startOfSelection, ref string portNameSel, ref int endOfSelection)
                    {
                    }

                    public void GetProtocolStackName(ref string protocolStackName, ushort maxStrSize)
                    {
                    }

                    public void GetProtocolStackNameSelection(string deviceName, int startOfSelection,
                        ref string protocolStackNameSel, ref int endOfSelection)
                    {
                    }
                }
            }
        }
    }
}