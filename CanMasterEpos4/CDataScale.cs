using System;

namespace EposCmd.Net
{
    public class CDataScale : CDataBaseCO
    {
        // Zámky pre thread-safe prístup
        private readonly object LockingWeight = new object();
        private readonly object LockingIo = new object();
        private readonly object LockingStatus = new object();

        // --- IO Premenné ---
        private uint _digitalInputs;
        private uint _digitalOutputs;

        // --- Váhové premenné ---
        private double _vaWeightInter;
        private double _vaWeightTare;
        private double _vaWeightFinal;
        
        private EVaStatus _vaStatus;
        private EVaStatus2 _vaStatus2;
        private EVaResult _vaResult;

        public CDataScale(byte nodeId, string name)
        {
            NodeId = nodeId;
            Name = name;
        }

        // Podľa štandardu CiA 301 je PDO komunikácia povolená len v stave OPERATIONAL
        public override bool IsPdoCommunicationAllowed => NmtStatus == ENmtStatus.NcsOPERATIONAL;

        // --- Vlastnosti (Properties) ---
        public uint DigitalInputs
        {
            get { lock (LockingIo) { return _digitalInputs; } }
            set { lock (LockingIo) { _digitalInputs = value; } }
        }

        public uint DigitalOutputs
        {
            get { lock (LockingIo) { return _digitalOutputs; } }
            set { lock (LockingIo) { _digitalOutputs = value; } }
        }

        public double VaWeightInter
        {
            get { lock (LockingWeight) { return _vaWeightInter; } }
            set { lock (LockingWeight) { _vaWeightInter = value; } }
        }

        public double VaWeightTare
        {
            get { lock (LockingWeight) { return _vaWeightTare; } }
            set { lock (LockingWeight) { _vaWeightTare = value; } }
        }

        public double VaWeightActual
        {
            get { lock (LockingWeight) { return _vaWeightInter - _vaWeightTare; } }
        }

        public double VaWeightFinal
        {
            get { lock (LockingWeight) { return _vaWeightFinal; } }
            set { lock (LockingWeight) { _vaWeightFinal = value; } }
        }

        public EVaStatus VaStatus
        {
            get { lock (LockingStatus) { return _vaStatus; } }
            set { lock (LockingStatus) { _vaStatus = value; } }
        }

        public EVaStatus2 VaStatus2
        {
            get { lock (LockingStatus) { return _vaStatus2; } }
            set { lock (LockingStatus) { _vaStatus2 = value; } }
        }

        public EVaResult VaResult
        {
            get { lock (LockingStatus) { return _vaResult; } }
            set { lock (LockingStatus) { _vaResult = value; } }
        }
    }
}