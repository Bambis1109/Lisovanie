namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class OperationMode : CEpos4CommandGroupCO
                {
                    public OperationMode(ushort keyHandle, byte nodeId, CDataEpos4 data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        BaseData = data;
                    }

                    public EOperationMode GetOperationMode()
                    {
                        return GetModeOfOperation();
                    }

                    public string GetOperationModeAsString()
                    {
                        string mode;
                        switch (GetModeOfOperation())
                        {
                          
                            case EOperationMode.OmdProfilePositionMode:
                                mode = "Profile position mode";
                                break;
                            case EOperationMode.OmdProfileVelocityMode:
                                mode = "Profile velocity mode";
                                break;
                            case EOperationMode.OmdHomingMode:
                                mode = "Homing mode";
                                break;
                            case EOperationMode.OmdCyclicSynchronousPositionMode:
                                mode = "Cyclic syn position mode";
                                break;
                            case EOperationMode.OmdCyclicSynchronousVelocityMode:
                                mode = "Cyclic syn velocity mode";
                                break;
                            case EOperationMode.OmdCyclicSyncronicTorqueMode:
                                mode = "Cyclic syn torque mode";
                                break;
                            default:
                                mode = "Unknow mode";
                                break;
                        }

                        return mode;
                    }

                    public void SetOperationMode(EOperationMode operationMode)
                    {

                    }
                }
            }
        }
    }
}