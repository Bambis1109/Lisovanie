using System;
using EposCmd.Net.DeviceScaleSet;

namespace EposCmd.Net
{
    public class CDataScale : CDataBaseCO
    {
        private readonly object LockingWeight = new object();
        private readonly object LockingStatus = new object();

        // Váhy
        private int _weightRaw;
        private int _weight32Inter;
        private int _weight32Tare;
        private int _weightFinal;
        private int _weightDuration;
        private EVaResult _weightResult;

        // Statusy (používame silné typy)
        private EProcStatus _statusMainProc;
        private EMatStatus _statusMainMat;
        private EZoneStatus _statusMainZone;
        
        private EProcStatus _statusDoserProc;
        private EMatStatus _statusDoserMat;
        
        private EProcStatus _statusVyloznikProc;
        private EMatStatus _statusVyloznikMat;

        public CDataScale(byte nodeId, string name)
        {
            NodeId = nodeId;
            Name = name;
        }

        public override bool IsPdoCommunicationAllowed => NmtStatus == ENmtStatus.NcsOPERATIONAL;

        // --- Vlastnosti pre Váhu ---
        public int WeightRaw { get { lock (LockingWeight) return _weightRaw; } set { lock (LockingWeight) _weightRaw = value; } }
        public int Weight32Inter { get { lock (LockingWeight) return _weight32Inter; } set { lock (LockingWeight) _weight32Inter = value; } }
        public int Weight32Tare { get { lock (LockingWeight) return _weight32Tare; } set { lock (LockingWeight) _weight32Tare = value; } }
        public int Weight32Actual { get { lock (LockingWeight) return _weight32Inter - _weight32Tare; } }
        public int WeightFinal { get { lock (LockingWeight) return _weightFinal; } set { lock (LockingWeight) _weightFinal = value; } }
        public int WeightDuration { get { lock (LockingWeight) return _weightDuration; } set { lock (LockingWeight) _weightDuration = value; } }
        public EVaResult WeightResult { get { lock (LockingStatus) return _weightResult; } set { lock (LockingStatus) _weightResult = value; } }

        // --- Vlastnosti pre Statusy ---
        public EProcStatus StatusMainProc { get { lock (LockingStatus) return _statusMainProc; } set { lock (LockingStatus) _statusMainProc = value; } }
        public EMatStatus StatusMainMat { get { lock (LockingStatus) return _statusMainMat; } set { lock (LockingStatus) _statusMainMat = value; } }
        public EZoneStatus StatusMainZone { get { lock (LockingStatus) return _statusMainZone; } set { lock (LockingStatus) _statusMainZone = value; } }

        public EProcStatus StatusDoserProc { get { lock (LockingStatus) return _statusDoserProc; } set { lock (LockingStatus) _statusDoserProc = value; } }
        public EMatStatus StatusDoserMat { get { lock (LockingStatus) return _statusDoserMat; } set { lock (LockingStatus) _statusDoserMat = value; } }

        public EProcStatus StatusVyloznikProc { get { lock (LockingStatus) return _statusVyloznikProc; } set { lock (LockingStatus) _statusVyloznikProc = value; } }
        public EMatStatus StatusVyloznikMat { get { lock (LockingStatus) return _statusVyloznikMat; } set { lock (LockingStatus) _statusVyloznikMat = value; } }
    }
}