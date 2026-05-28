using System;

namespace EposCmd.Net
{
    public class CDataEpos4 : CDataBaseCO
    {
        private readonly object LockingStatusword = new object();
        private readonly object LockingIpmStatus = new object();
        private readonly object LockingInput = new object();
        private readonly object LockingDigitalAllInput = new object();
        private readonly object LockingDigitalAllOutput = new object();
        private readonly object LockingAnalogInput1 = new object();
        private readonly object LockingAnalogInput2 = new object();
        private readonly object LockingModeOfOperationDisplayStatusword = new object();
        private readonly object LockingModeOfOperation = new object();
        private readonly object LockingPositionActual = new object();
        private readonly object LockingPositionActualSensor2 = new object();
        private readonly object LockingPositionTarget = new object();
        private readonly object LockingTorqueTarget = new object();
        private readonly object LockingVelocityActual = new object();
        private readonly object LockingCurrentActual = new object();
        private readonly object LockingCurrentActualPercentage = new object();
        private readonly object LockingCurrentMax = new object();

        private short _analogInput1;
        private short _analogInput2;
        private int _currentActualAverage;
        private double _currentActualAveragePercentage;
        private int _currentMax;
        private ushort _digitalAllInput;
        private ushort _digitalAllOutput;
        private ushort _ipmStatu;
        private EOperationMode _modeOfOperationDisplay;
        private EOperationMode _modeOfOperation;
        private int _positionActual;
        private int _positionActualSensor2;
        private int _positionTarget;
        private int _torqueTarget;
        private int _velocityActual;
        private ushort _statusword;
        private ushort _input;
        private bool polaritySen2;

        public double Gear;
        public double Pulse;

        public CDataEpos4(byte nodeId, string name, double gear)
        {
            NodeId = nodeId;
            Name = name;
            Gear = gear;
        }

        public CDataEpos4(byte nodeId, string name, double gear, double pulse) : this(nodeId, name, gear)
        {
            Pulse = pulse;
        }

        public ushort Statusword
        {
            get { lock (LockingStatusword) { return _statusword; } }
            set { lock (LockingStatusword) { _statusword = value; } }
        }

        public ushort Input
        {
            get { lock (LockingInput) { return _input; } }
            set { lock (LockingInput) { _input = value; } }
        }

        // EPOS4 špecifické bity zo Statuswordu
        public bool TargetReached => (GetStatusWord() & 0x0400) == 0x0400;
        public bool Ack => (GetStatusWord() & 0x1000) == 0x1000;
        public bool RemoteStatus => (Statusword & 0x0200) == 0x0200;
        public bool DisableState => (GetStatusWord() & 0x0040) == 0x0040;
        public bool EnableState => (GetStatusWord() & 0x007F) == 0x0037;
        public bool FaultState => (GetStatusWord() & 0x0008) == 0x0008;
        public bool QuickStopState => (GetStatusWord() & 0x007F) == 0x0017;
        public bool ReadyToSwitchOn => (GetStatusWord() & 0x007F) == 0x0021;
        public bool FollowingError => (GetStatusWord() & 0x2000) == 0x2000;
        public bool HomingAttained => (GetStatusWord() & 0x1000) == 0x1000;
        public bool HomingError => (GetStatusWord() & 0x2000) == 0x2000;

        // Override pre PDO zápis - EPOS4 vyžaduje RemoteStatus = true
        public override bool IsPdoCommunicationAllowed => RemoteStatus;

        public ushort IpmStatus
        {
            get { lock (LockingIpmStatus) { return _ipmStatu; } }
            set { lock (LockingIpmStatus) { _ipmStatu = value; } }
        }
        public ushort DigitalAllInput
        {
            get { lock (LockingDigitalAllInput) { return _digitalAllInput; } }
            set { lock (LockingDigitalAllInput) { _digitalAllInput = value; } }
        }
        public ushort DigitalAllOutput
        {
            get { lock (LockingDigitalAllOutput) { return _digitalAllOutput; } }
            set { lock (LockingDigitalAllOutput) { _digitalAllOutput = value; } }
        }
        public short AnalogInput1
        {
            get { lock (LockingAnalogInput1) { return _analogInput1; } }
            set { lock (LockingAnalogInput1) { _analogInput1 = value; } }
        }
      public short AnalogInput2
        {
            get { lock (LockingAnalogInput2) { return _analogInput2; } }
            set { lock (LockingAnalogInput2) { _analogInput2 = value; } }
        }
        
        public EOperationMode ModeOfOperationDisplay
        {
            get { lock (LockingModeOfOperationDisplayStatusword) { return _modeOfOperationDisplay; } }
            set { lock (LockingModeOfOperationDisplayStatusword) { _modeOfOperationDisplay = value; } }
        }
        public EOperationMode ModeOfOperation
        {
            get { lock (LockingModeOfOperation) { return _modeOfOperation; } }
            set { lock (LockingModeOfOperation) { _modeOfOperation = value; } }
        }
        public int PositionActual
        {
            get { lock (LockingPositionActual) { return _positionActual; } }
            set { lock (LockingPositionActual) { _positionActual = value; } }
        }
        public int PositionActualSensor2
        {
            get { lock (LockingPositionActualSensor2) { return _positionActualSensor2; } }
            set { lock (LockingPositionActualSensor2) { _positionActualSensor2 = value; } }
        }
        public double PositionActualSensor2Float => (double)PositionActualSensor2 / 1000;
        
        public int PositionTarget
        {
            get { lock (LockingPositionTarget) { return _positionTarget; } }
            set { lock (LockingPositionTarget) { _positionTarget = value; } }
        }
        public int TorqueTarget
        {
            get { lock (LockingTorqueTarget) { return _torqueTarget; } }
            set { lock (LockingTorqueTarget) { _torqueTarget = value; } }
        }
        public double PositionActualGear => (double)PositionActual / Gear;
        
        public int VelocityActual
        {
            get { lock (LockingVelocityActual) { return _velocityActual; } }
            set { lock (LockingVelocityActual) { _velocityActual = value; } }
        }
        public int CurrentActualAverage
        {
            get { lock (LockingCurrentActual) { return _currentActualAverage; } }
            set { lock (LockingCurrentActual) { _currentActualAverage = value; } }
        }
        public double CurrentActualAveragePercentage
        {
            get { lock (LockingCurrentActualPercentage) { return _currentActualAveragePercentage; } }
            set { lock (LockingCurrentActualPercentage) { _currentActualAveragePercentage = value; } }
        }
        public int CurrentMax
        {
            get { lock (LockingCurrentMax) { return _currentMax; } }
            set { lock (LockingCurrentMax) { _currentMax = value; } }
        }

        public bool PolaritySen2
        {
            get => polaritySen2;
            set { if (value != polaritySen2) polaritySen2 = value; }
        }

        private ushort GetStatusWord()
        {
            if (!RemoteStatus)
                throw new CDeviceException($" SetStatusWord Node {NodeId:d}: Remote Node Off [0x{Statusword:X02}] ", 0);
            return _statusword;
        }

        public string OperationMode()
        {
            switch (_modeOfOperationDisplay)
            {
                case EOperationMode.OmdProfilePositionMode: return "Profile position mode";
                case EOperationMode.OmdProfileVelocityMode: return "Profile velocity mode";
                case EOperationMode.OmdHomingMode: return "Homing mode";
                case EOperationMode.OmdCyclicSynchronousPositionMode: return "Cyclic syn position mode";
                case EOperationMode.OmdCyclicSynchronousVelocityMode: return "Cyclic syn velocity mode";
                case EOperationMode.OmdCyclicSyncronicTorqueMode: return "Cyclic syn torque mode";
                default: return "Unknow mode";
            }
        }
    }
}