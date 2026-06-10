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
        public ScaleVibro Vibro { get; }

        public ScaleOperation(ushort keyHandle, byte nodeId, CDataScale data)
        {
            Master = new ScaleMasterPlc(keyHandle, nodeId, data);
            Doser = new ScaleDoser(keyHandle, nodeId, data);
            Boom = new ScaleBoom(keyHandle, nodeId, data);
            Lock = new ScaleLock(keyHandle, nodeId, data);
            Weigher = new ScaleWeigher(keyHandle, nodeId, data);
            System = new ScaleSystem(keyHandle, nodeId, data);
            Vibro = new ScaleVibro(keyHandle, nodeId, data);
        }
    }

    // --- Jednotlivé moduly ---

    public class ScaleMasterPlc : CScaleCommandGroupCO
    {
        public ScaleMasterPlc(ushort keyHandle, byte nodeId, CDataScale data) { KeyHandle = keyHandle; NodeId = nodeId; BaseData = data; }
        
        public void SendCommand(EMasterCommand cmd) => SendCommandFireAndForget(0x6206, (uint)cmd);
    }

    public class ScaleDoser : CScaleCommandGroupCO
    {
        public ScaleDoser(ushort keyHandle, byte nodeId, CDataScale data) { KeyHandle = keyHandle; NodeId = nodeId; BaseData = data; }
        
        public void SendCommand(EDoserCommand cmd) => SendCommandFireAndForget(0x6205, (uint)cmd);
    }

    public class ScaleBoom : CScaleCommandGroupCO
    {
        public ScaleBoom(ushort keyHandle, byte nodeId, CDataScale data) { KeyHandle = keyHandle; NodeId = nodeId; BaseData = data; }
        
        public void SendCommand(EBoomCommand cmd) => SendCommandFireAndForget(0x6203, (uint)cmd);
    }

    public class ScaleLock : CScaleCommandGroupCO
    {
        public ScaleLock(ushort keyHandle, byte nodeId, CDataScale data) { KeyHandle = keyHandle; NodeId = nodeId; BaseData = data; }
        
        public void SendCommand(ELockCommand cmd) => SendCommandFireAndForget(0x6201, (uint)cmd);
        
        // Čítanie statusu cez SDO (pre ladenie, neblokuje UI ak sa volá správne)
        public uint GetStatus() => (uint)ReadSdo(0x6201, 0x02, 4);
    }

    public class ScaleWeigher : CScaleCommandGroupCO
    {
        public ScaleWeigher(ushort keyHandle, byte nodeId, CDataScale data) { KeyHandle = keyHandle; NodeId = nodeId; BaseData = data; }
        
        public void SendCommand(EScaleCommand cmd) => SendCommandFireAndForget(0x6202, (uint)cmd);
        
        public uint GetStatus() => (uint)ReadSdo(0x6202, 0x02, 4);
    }

    public class ScaleSystem : CScaleCommandGroupCO
    {
        public ScaleSystem(ushort keyHandle, byte nodeId, CDataScale data) { KeyHandle = keyHandle; NodeId = nodeId; BaseData = data; }
        
        public void SendCommand(ESystemCommand cmd) => SendCommandFireAndForget(0x6100, (uint)cmd);
    }
    public class ScaleVibro : CScaleCommandGroupCO
    {
        public ScaleVibro(ushort keyHandle, byte nodeId, CDataScale data) { KeyHandle = keyHandle; NodeId = nodeId; BaseData = data; }
        
        public void SendCommand(EVibroCommand cmd) => SendCommandFireAndForget(0x6204, (uint)cmd);
        
        public uint GetStatus() => (uint)ReadSdo(0x6204, 0x02, 4);
    }
}