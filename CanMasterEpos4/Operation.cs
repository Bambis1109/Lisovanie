namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class Operation
                {
                    public Operation(ushort keyHandle, byte nodeId, CDataCO data)
                    {
                        CurrentMode = new CyclicSynTorqueMode(keyHandle, nodeId, data);
                        DeviceErrorHandling = new DeviceErrorHandling(keyHandle, nodeId, data);
                        HomingMode = new HomingMode(keyHandle, nodeId, data);
                        Io = new InputOutput(keyHandle, nodeId, data);
                        MotionInfo = new MotionInfo(keyHandle, nodeId, data);
                        OperationMode = new OperationMode(keyHandle, nodeId, data);
                        CyclicSynPositionMode = new CyclicSynPositionMode(keyHandle, nodeId, data);
                        ProfilePositionMode = new ProfilePositionMode(keyHandle, nodeId, data);
                        ProfileVelocityMode = new ProfileVelocityMode(keyHandle, nodeId, data);
                        StateMachine = new StateMachine(keyHandle, nodeId, data);
                    CyclicSynVelocityMode = new CyclicSynVelocityMode(keyHandle, nodeId, data);
                    }

                    public CyclicSynTorqueMode CurrentMode { get; }
                    public DeviceErrorHandling DeviceErrorHandling { get; }
                    public HomingMode HomingMode { get; }
                    public InputOutput Io { get; }
                    public MotionInfo MotionInfo { get; }
                    public OperationMode OperationMode { get; }
                    public CyclicSynPositionMode CyclicSynPositionMode { get; }
                    public ProfilePositionMode ProfilePositionMode { get; }
                    public ProfileVelocityMode ProfileVelocityMode { get; }
                    public StateMachine StateMachine { get; }
                
                    public CyclicSynVelocityMode CyclicSynVelocityMode { get; }
                }
            }
        }
    }
}