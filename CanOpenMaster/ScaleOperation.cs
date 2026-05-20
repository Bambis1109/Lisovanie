using System;

namespace EposCmd.Net.DeviceScaleSet
{
    public class ScaleOperation
    {
        public ScaleMasterPlc Master { get; }
        public ScaleDoser Doser { get; }
        public ScaleBoom Boom { get; }
        public ScaleLock Lock { get; }
        public ScaleWeigher Weigher { get; }
        public ScaleSystem System { get; }

        public ScaleOperation(ushort keyHandle, byte nodeId, CDataScale data)
        {
            Master = new ScaleMasterPlc(keyHandle, nodeId, data);
            Doser = new ScaleDoser(keyHandle, nodeId, data);
            Boom = new ScaleBoom(keyHandle, nodeId, data);
            Lock = new ScaleLock(keyHandle, nodeId, data);
            Weigher = new ScaleWeigher(keyHandle, nodeId, data);
            System = new ScaleSystem(keyHandle, nodeId, data);
        }
    }

    // --- Jednotlivé moduly ---

    public class ScaleMasterPlc : CScaleCommandGroupCO
    {
        public ScaleMasterPlc(ushort keyHandle, byte nodeId, CDataScale data) { KeyHandle = keyHandle; NodeId = nodeId; BaseData = data; }
        
        public void SendCommand(EMasterCommand cmd) => ExecuteCommandWithAck(0x6206, (uint)cmd);
    }

    public class ScaleDoser : CScaleCommandGroupCO
    {
        public ScaleDoser(ushort keyHandle, byte nodeId, CDataScale data) { KeyHandle = keyHandle; NodeId = nodeId; BaseData = data; }
        
        public void SendCommand(EDoserCommand cmd) => ExecuteCommandWithAck(0x6205, (uint)cmd);
    }

    public class ScaleBoom : CScaleCommandGroupCO
    {
        public ScaleBoom(ushort keyHandle, byte nodeId, CDataScale data) { KeyHandle = keyHandle; NodeId = nodeId; BaseData = data; }
        
        public void SendCommand(EBoomCommand cmd) => ExecuteCommandWithAck(0x6203, (uint)cmd);
    }

    public class ScaleLock : CScaleCommandGroupCO
    {
        public ScaleLock(ushort keyHandle, byte nodeId, CDataScale data) { KeyHandle = keyHandle; NodeId = nodeId; BaseData = data; }
        
        public void SendCommand(ELockCommand cmd) => ExecuteCommandWithAck(0x6201, (uint)cmd);
        
        // Čítanie statusu cez SDO (pre ladenie)
        public uint GetStatus() => (uint)ReadSdo(0x6201, 0x02, 4);
    }

    public class ScaleWeigher : CScaleCommandGroupCO
    {
        public ScaleWeigher(ushort keyHandle, byte nodeId, CDataScale data) { KeyHandle = keyHandle; NodeId = nodeId; BaseData = data; }
        
        public void SendCommand(EScaleCommand cmd) => ExecuteCommandWithAck(0x6202, (uint)cmd);
        
        public uint GetStatus() => (uint)ReadSdo(0x6202, 0x02, 4);
    }

    public class ScaleSystem : CScaleCommandGroupCO
    {
        public ScaleSystem(ushort keyHandle, byte nodeId, CDataScale data) { KeyHandle = keyHandle; NodeId = nodeId; BaseData = data; }
        
        public void SendCommand(ESystemCommand cmd) => ExecuteCommandWithAck(0x6100, (uint)cmd);
    }
}