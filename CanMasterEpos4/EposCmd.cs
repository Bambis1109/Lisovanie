using System.Xml.Linq;
using IXXAT;


namespace EposCmd
{
    namespace Net
    {
        public enum ECanMessageType
        {
            Pdo,
            Emergency,
            Event
        }

        public struct CanMessage
        {
            public ECanMessageType Type;
            public CANopenMasterAPI6.COP_t_RX_PDO Pdo;
            public CANopenMasterAPI6.COP_t_EMERGENCY_OBJ Emergency;
            public CANopenMasterAPI6.COP_t_EVENT_OBJ Event;
        }
        
        public enum EAnalogInputConfiguration
        {
            AicAnalogCurrentSetpoint = 0,
            AicAnalogVelocitySetpoint = 1,
            AicAnalogPositionSetpoint = 2,
            AicAnalogGeneralPurposeA = 16
        }

        public enum ECommandSpecifier
        {
            NcsBootUPRemoteNode = 0,
            NcsStartRemoteNode = 1,
            NcsStopRemoteNode = 2,
            NcsEnterPreOperational = 128,
            NcsResetNode = 129,
            NcsResetCommunication = 130,
            NcsResetAllNode = 131
        }

        public enum ESync
        {
            NcsEnable = 0,
            NcsDisable = 1,
            NcsOneSync = 3
        }

        public enum ENmtStatus
        {
            NcsBOOTUP = 0,
            NcsDISCONNECTED = 1,
            NcsSTOPPED = 4,
            NcsOPERATIONAL = 5,
            NcsPREOPERATIONAL = 127,
            NcsUNKNOWN = 255
        }

        public enum EDataRecorderTriggerType
        {
            DrMovementTrigger = 1,
            DrErrorTrigger = 2,
            DrDigitalInputTrigger = 4,
            DrMovementEndTrigger = 8
        }

        public enum EDialogMode
        {
            DmProgressDlg = 0,
            DmProgressAndConfirmDlg = 1,
            DmConfirmDlg = 2,
            DmNoDlg = 3
        }

        public enum EDigitalInputConfiguration
        {
            DicNegativeLimitSwitch = 0,
            DicPositiveLimitSwitch = 1,
            DicHomeSwitch = 2,
            DicPositionMarker = 3,
            DicDriveEnable = 4,
            DicQuickStop = 5,
            DicGeneralPurposeJ = 6,
            DicGeneralPurposeI = 7,
            DicGeneralPurposeH = 8,
            DicGeneralPurposeG = 9,
            DicGeneralPurposeF = 10,
            DicGeneralPurposeE = 11,
            DicGeneralPurposeD = 12,
            DicGeneralPurposeC = 13,
            DicGeneralPurposeB = 14,
            DicGeneralPurposeA = 15,
            DicTouchProbe1 = 26,
            DicNoFunctionality = 255
        }

        public enum EDigitalOutputConfiguration
        {
            DocReadyFault = 0,
            DocPositionCompare = 1,
            DocGeneralPurposeH = 8,
            DocGeneralPurposeG = 9,
            DocGeneralPurposeF = 10,
            DocGeneralPurposeE = 11,
            DocGeneralPurposeD = 12,
            DocGeneralPurposeC = 13,
            DocGeneralPurposeB = 14,
            DocGeneralPurposeA = 15,
            DocNoFunctionality = 255
        }

        public enum EHomingMethod
        {
            HmCurrentThresholdNegativeSpeed = -4,//
            HmCurrentThresholdPositiveSpeed = -3,//
            HmCurrentThresholdNegativeSpeedAndIndex = -2,//
            HmCurrentThresholdPositiveSpeedAndIndex = -1,//
            HmNegativeLimitSwitchAndIndex = 1,//
            HmPositiveLimitSwitchAndIndex = 2,//
            HmHomeSwitchPositiveSpeedAndIndex = 7,//
            HmHomeSwitchNegativeSpeedAndIndex = 11, //
            HmNegativeLimitSwitch = 17,//
            HmPositiveLimitSwitch = 18,//
            HmHomeSwitchPositiveSpeed = 23,//
            HmHomeSwitchNegativeSpeed = 27,//
            HmIndexNegativeSpeed = 33,//
            HmIndexPositiveSpeed = 34,//
            HmActualPosition = 37//
        }

        public enum EMotorType
        {
            MtDcMotor = 1,
            MtEcSinusCommutatedMotor = 10,
            MtEcBlockCommutatedMotor = 11
        }

        public enum EOperationMode
        {


            OmdProfilePositionMode = 1,
            OmdProfileVelocityMode = 3,
            OmdHomingMode = 6,
            OmdCyclicSynchronousPositionMode = 8,
            OmdCyclicSynchronousVelocityMode = 9,
            OmdCyclicSyncronicTorqueMode = 10
        }

        public enum EPositionCompareDirectionDependency
        {
            PcdMotorDirectionNegative = 0,
            PcdMotorDirectionPositive = 1,
            PcdMotorDirectionBoth = 2
        }

        public enum EPositionCompareIntervalMode
        {
            PciNegativeDirToRefpos = 0,
            PciPositiveDirToRefpos = 1,
            PciBothDirToRefpos = 2
        }

        public enum EPositionCompareOperationalMode
        {
            PcoSinglePositionMode = 0,
            PcoPositionSequenceMode = 1
        }

        public enum EPositionMarkerEdgeType
        {
            PetBothEdges = 0,
            PetRisingEdge = 1,
            PetFallingEdge = 2
        }

        public enum EPositionMarkerMode
        {
            PmContinuous = 0,
            PmSingle = 1,
            PmMultiple = 2
        }

        public enum ESensorType
        {
            StUnknown = 0,
            StNone = 0,
            StIncEncoder3Channel = 1,
            StIncEncoder2Channel = 2,
            StHallSensors = 3,
            StSsiAbsEncoderBinary = 4,
            StSsiAbsEncoderGrey = 5
        }

        public enum EStates
        {
            StDisabled,
            StReadyToSwitchOn,
            StSwitchedOn,
            StEnabled,
            StQuickStop,
            StReactionFaul,
            StFault
        }

        public enum EVelocityDimension
        {
            VdRpm = 164
        }

        public enum EVelocityNotation
        {
            VnMilli = -3,
            VnCenti = -2,
            VnDeci = -1,
            VnStandard = 0
        }

        public enum EErrorCode
        {
        }
        public enum enmResult { Ok, Nok }
        public enum enmError { Error, NoError }

        public enum EVaStatus
        {
            NoInit = 0,
            Ready = 1,
            Busy = 2,
            Error = 3,
            NoDef = 4
        }
        public enum EVaStatus2
        {
            NoSt = 0,
            Full = 1,
            Empty = 2,

        }

        public enum EVaResult
        {
            NOK = 0,
            OK = 1,
            Unknow = 3

        }
    }
}