namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class PositionMarker : CCommandGroupCO
                {
                    public PositionMarker(ushort keyHandle, byte nodeId, CDataCO Data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        this.Data = Data;
                    }

                    public void ActivatePositionMarker(ushort digitalInputNumber, int polarity)
                    {

                    }

                    public void DeactivatePositionMarker(ushort digitalInputNumber)
                    {
                    }

                    public void GetPositionMarkerParameter(ref EPositionMarkerEdgeType positionMarkerEdgeType, ref EPositionMarkerMode positionMarkerMode)
                    {
                    }

                    public int GetPositionMarkerCapturedPosition()
                    {
                        return (int)ReadSdo(0x2074, 0x01, 4);
                    }

                    public void ReadPositionMarkerCounter(ref ushort count)
                    {

                    }
                    public int GetPositionMarkerCounter()
                    {
                        return (int)ReadSdo(0x2074, 0x04, 2);
                    }

                    public void ResetPositionMarkerCounter()
                    {
                        WritedSDO(0x2074, 0x04, (ushort)0, 2);
                    }

                    public void SetPositionMarkerParameter(EPositionMarkerEdgeType positionMarkerEdgeType, EPositionMarkerMode positionMarkerMode)
                    {
                        WritedSDO(0x2074, 0x02, (byte)EPositionMarkerEdgeType.PetRisingEdge, 1);
                        WritedSDO(0x2074, 0x03, (byte)EPositionMarkerMode.PmSingle, 1);
                    }
                }
            }
        }
    }
}