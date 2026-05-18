using System.Threading;

namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class StateMachine : CEpos4CommandGroupCO
                {
                    public StateMachine(ushort keyHandle, byte nodeId, CDataEpos4 data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        BaseData = data;
                    }
                 
                    public bool GetDisableState() { return Data.DisableState; }
                    public bool GetEnableState() { return Data.EnableState; }
                    public bool GetFaultState() { return Data.FaultState; }
                    public bool GetQuickStopState() { return Data.QuickStopState; }
                    public bool GetSwitchOnState() { return Data.ReadyToSwitchOn; }
                 
                    public ushort Statuswordx() { return Data.Statusword; }
                    public void ResetDevice() { }
             
                }
            }
        }
    }
}