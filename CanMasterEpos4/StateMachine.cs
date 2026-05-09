using System.Threading;

namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class StateMachine : CCommandGroupCO
                {
                    public StateMachine(ushort keyHandle, byte nodeId, CDataCO Data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        this.Data = Data;
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