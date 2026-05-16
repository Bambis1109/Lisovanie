using System;
using static IXXAT.CANopenMasterAPI6;


namespace EposCmd
{
    namespace Net
    {
        public class CDataCO
        {
            public readonly object NodePdoLock = new object();
            public readonly object NodeSdoLock = new object();
            private readonly object LockingStatusword = new object();
            private readonly object LockingIpmStatus = new object();
            private readonly object LockingInput = new object();

            private readonly object LockingDigitalAllInput = new object();
            private readonly object LockingDigitalAllOutput = new object();
            private readonly object LockingAnalogInput1 = new object();
            private readonly object LockingAnalogInput2 = new object();
            private readonly object LockingModeOfOperationDisplayStatusword = new object();
            private readonly object LockingModeOfOperation = new object();
            private readonly object LockingNmtStatus = new object();
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
            private ENmtStatus _nmtStatus;
            private int _positionActual;
            private int _positionActualSensor2;
            private int _positionTarget;
            private int _torqueTarget;
            private int _velocityActual;


            public byte[] TxdataPDO1 { get; set; } = new byte[2];
            public byte[] TxdataPDO2 { get; set; } = new byte[2];
            public byte[] TxdataPDO3 { get; set; } = new byte[7];
            public byte[] TxdataPDO4 { get; set; } = new byte[4];

            public double Gear;
            public double Pulse;
            private ushort _statusword;
            
            //++++++++++++++++++++++++++++++++ Uprava krok 2 ++++++++++++++++++++++++++++++++++
            
            private readonly object LockingWpdoError = new object();
            private bool _wpdoError;
            private byte _wpdoErrorPdoNumber;

            public bool WpdoError
            {
                get { lock (LockingWpdoError) { return _wpdoError; } }
                set { lock (LockingWpdoError) { _wpdoError = value; } }
            }

            public byte WpdoErrorPdoNumber
            {
                get { lock (LockingWpdoError) { return _wpdoErrorPdoNumber; } }
                set { lock (LockingWpdoError) { _wpdoErrorPdoNumber = value; } }
            }

            public void ResetWpdoError()
            {
                lock (LockingWpdoError)
                {
                    _wpdoError = false;
                    _wpdoErrorPdoNumber = 0;
                }
            }
            
            //--------------------------- Uprava krok 2 --------------------------------
            
            public CDataCO(byte nodeId, string name, double gear)
            {
                _statusword = 0;
                _modeOfOperationDisplay = 0;
                _ipmStatu = 0;
                NodeId = nodeId;
                Name = name;
                Gear = gear;
            }
            public CDataCO(byte nodeId, string name, double gear, double pulse)
            {
                _statusword = 0;
                _modeOfOperationDisplay = 0;
                _ipmStatu = 0;
                NodeId = nodeId;
                Name = name;
                Gear = gear;
                Pulse = pulse;
            }
            public COP_t_EMERGENCY_OBJ LastEmergency { get; set; }
            public COP_t_EVENT_OBJ LastEvent { get; set; }
            public byte NodeId { get; set; }
            public string Name { get; set; }
            public ushort Statusword
            {
                get { lock (LockingStatusword) { return _statusword; } }
                set { lock (LockingStatusword) { _statusword = value; } }
            }
            private ushort _input;
            public ushort Input
            {
                get { lock (LockingInput) { return _input; } }
                set { lock (LockingInput) { _input = value; } }
            }
          

            //************************************************************** VAHA *******************************************************


            private static readonly object LockingVaStatus = new object();
            private EVaStatus _vaStatus;
            public EVaStatus VaStatus
            {
                get { lock (LockingVaStatus) { return _vaStatus; } }
                set { lock (LockingVaStatus) { _vaStatus = value; } }
            }

            private static readonly object LockingVaStatus2 = new object();
            private EVaStatus2 _vaStatus2;
            public EVaStatus2 VaStatus2
            {
                get { lock (LockingVaStatus2) { return _vaStatus2; } }
                set { lock (LockingVaStatus2) { _vaStatus2 = value; } }
            }

            private static readonly object LockingVaResult = new object();
            private EVaResult _vaResult;
            public EVaResult VaResult
            {
                get { lock (LockingVaResult) { return _vaResult; } }
                set { lock (LockingVaResult) { _vaResult = value; } }
            }

            private static readonly object LockingWeight = new object();
            private double _vaWeightInter;
            public double VaWeightInter
            {
                get { lock (LockingWeight) { return _vaWeightInter; } }
                set { lock (LockingWeight) { _vaWeightInter = value; } }
            }


            private double _vaWeightTare;
            public double VaWeightTare
            {
                get { lock (LockingWeight) { return _vaWeightTare; } }
                set { lock (LockingWeight) { _vaWeightTare = value; } }
            }

            public double VaWeightActual
            {
                get { lock (LockingWeight) { return _vaWeightInter - _vaWeightTare; } }

            }

            private static readonly object LockingFinal = new object();

            private double _vaWeightFinal;
            public double VaWeightFinal
            {
                get { lock (LockingFinal) { return _vaWeightFinal; } }
                set { lock (LockingFinal) { _vaWeightFinal = value; } }
            }

            //*************************************************************************************************************************
            public bool TargetReached => (GetStatusWord() & 0x0400) == 0x0400;
            public bool Ack => (GetStatusWord() & 0x1000) == 0x1000;
            public bool RemoteStatus => (Statusword & 0x0200) == 0x0200;
            public bool DisableState => (GetStatusWord() & 0x0040) == 0x0040;
            public bool EnableState => (GetStatusWord() & 0x007F) == 0x0037;
            public bool FaultState => (GetStatusWord() & 0x0008) == 0x0008;
            public bool QuickStopState => (GetStatusWord() & 0x007F) == 0x0017;
            public bool ReadyToSwitchOn => (GetStatusWord() & 0x007F) == 0x0021;
            public bool FollowingError => (GetStatusWord() & 0x2000) == 0x2000;


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

            public int AnalogInput1Weight
            {
                get { lock (LockingAnalogInput1) { return (int)(((double) _analogInput1-2000)*1.25); } }
               
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
            public ENmtStatus NmtStatus
            {
                get { lock (LockingNmtStatus) { return _nmtStatus; } }
                set { lock (LockingNmtStatus) { _nmtStatus = value; } }
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

            public double PositionActualSensor2Float
            {
                get { lock (LockingPositionActualSensor2) { return (double)_positionActualSensor2/1000; } }
            }
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
            public double PositionActualGear { get => (double)PositionActual / Gear; }
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

            private bool polaritySen2;
            public bool PolaritySen2
            {
                get => polaritySen2;
                set
                {
                    if (value == polaritySen2) return;
                    polaritySen2 = value;
                }
            }

            private void WritePDO4()
            {
                //var Txdata = new byte[4];
                //Txdata = BitConverter.GetBytes(targetPosition);
                //WritePDO(4, Txdata);
            }
            private ushort GetStatusWord()
            {
                if (!RemoteStatus)
                    throw new CDeviceException($" SetStatusWord Node {NodeId:d}: Remote Node Off [0x{Statusword:X02}] ",
                        0);
                return _statusword;
            }
            public string OperationMode()
            {
                string mode;
                var op = _modeOfOperationDisplay;
                switch (op)
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
            public string NmtStatusString()
            {
                string description;
                switch (NmtStatus)
                {
                    case ENmtStatus.NcsBOOTUP:
                        description = "Bootup";
                        break;
                    case ENmtStatus.NcsDISCONNECTED:
                        description = "Disconected";
                        break;
                    case ENmtStatus.NcsSTOPPED:
                        description = "Stopped";
                        break;
                    case ENmtStatus.NcsOPERATIONAL:
                        description = "Operational";
                        break;
                    case ENmtStatus.NcsPREOPERATIONAL:
                        description = "Preoperational";
                        break;
                    case ENmtStatus.NcsUNKNOWN:
                        description = "Unknown";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                return description;
            }
        }
    }
}