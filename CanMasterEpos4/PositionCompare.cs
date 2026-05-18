using System;

namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class PositionCompare : CEpos4CommandGroupCO
                {
                    public PositionCompare(ushort keyHandle, byte nodeId, CDataEpos4 data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        BaseData = data;
                    }

                    public ushort ModificationBit(ushort value, ushort bitNumber, bool bit)
                    {
                        if (!bit)
                        {
                            return (ushort)(value & ~(1 << bitNumber));
                        }
                        else
                        {
                            return (ushort)(value | (1 << bitNumber));
                        }
                    }
                    public void DisablePositionCompare()
                    {
                        ushort val = (ushort)ReadSdo(0x207A, 0x01, 2);
                        WritedSDO(0x207A, 0x01, ModificationBit(val, 0, false), 2);

                    }
                    public void EnablePositionCompare()
                    {
                        ushort val = (ushort)ReadSdo(0x207A, 0x01, 2);
                        WritedSDO(0x207A, 0x01, ModificationBit(val, 0, true), 2);
                    }
                    public void GetPositionCompareParameter(
                        ref EPositionCompareOperationalMode operationalMode,
                        ref EPositionCompareIntervalMode intervalMode,
                        ref EPositionCompareDirectionDependency directionDependency,
                        ref ushort intervalWidth,
                        ref ushort intervalRepetitions,
                        ref ushort pulseWidth)
                    {
                        ushort value = (ushort)ReadSdo(0x207A, 0x01, 2);
                        operationalMode = (EPositionCompareOperationalMode)(SelectBits(value, 0b0000000000000110, 1));
                        intervalMode = (EPositionCompareIntervalMode)(SelectBits(value, 0b0000000000011000, 3));
                        directionDependency = (EPositionCompareDirectionDependency)(SelectBits(value, 0b0000000001100000, 5));
                        intervalWidth = (ushort)ReadSdo(0x207A, 0x03, 4);
                        intervalRepetitions = (ushort)ReadSdo(0x207A, 0x04, 2);
                        pulseWidth = (ushort)ReadSdo(0x207A, 0x05, 2);
                    }
                    private ushort SelectBits(ushort c, ushort m, ushort n)
                    {
                        ushort a = (ushort)(c & m);
                        ushort b = (ushort)(a >> n);
                        return b;
                    }

                    public void SetPositionCompareParameter(EPositionCompareOperationalMode operationalMode,
                        EPositionCompareIntervalMode intervalMode,
                        EPositionCompareDirectionDependency directionDependency, ushort intervalWidth,
                        ushort intervalRepetitions, ushort pulseWidth)
                    {

                        ushort value = (ushort)ReadSdo(0x207A, 0x01, 2);
                        ushort oM = (ushort)(((ushort)(operationalMode)) << 1);
                        ushort iM = (ushort)(((ushort)(intervalMode)) << 3);
                        ushort dD = (ushort)(((ushort)(directionDependency)) << 5);

                        ushort v = (ushort)(oM | iM | dD);
                        WritedSDO(0x207A, 0x01, v, 2);
                        WritedSDO(0x207A, 0x03, intervalWidth, 4);
                        WritedSDO(0x207A, 0x04, intervalRepetitions, 2);
                        WritedSDO(0x207A, 0x05, pulseWidth, 2);
                    }


                    public void SetPositionCompareReferencePosition(int referencePosition)
                    {
                        WritedSDO(0x207A, 0x02, (ulong)referencePosition, 4);
                    }
                    public void SetProbeFunction(UInt32 referencePosition)
                    {
                        WritedSDO(0x60B8, 0x00, referencePosition, 2);
                    }

                    public int GetToucheProbePositiveEdgeCounter()
                    {
                       return (int)ReadSdo(0x60D5, 0x00, 2);
                    }
                    public int GetToucheProbeNegativeEdgeCounter()
                    {
                        return (int)ReadSdo(0x60D6, 0x00, 2);
                    }
                }
            }
        }
    }
}