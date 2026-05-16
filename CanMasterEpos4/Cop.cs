using System.Runtime.InteropServices;

namespace IXXAT
{
    /*##########################################################################
     Name:
      CANopenMasterAPI6
     Description :
      Defines and Structures for the command queue between firmware and API-DLL
    ##########################################################################*/
    public class CANopenMasterAPI6
    {
        // Name of the DLL that provides the CANopen Master API
        //  Here: 32bit platform target
        //public const string CANopenMasterAPIDll = "XatCOP60.dll";

        #region cop Structures

        //************************************************************************
        //  Board information
        //************************************************************************
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct COP_BOARD_INFO
        {
            public ushort hw_version;
            public ushort fw_version;
            public ushort sw_version;
            public uint board_seg;
            public ushort irq_num;
            public ushort canlines;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string serial_num;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
            public string str_hw_type;
        }

        #endregion

        public const string CANopenMasterAPIDll = "XatCOP60-64.dll";

        #region cop Constants

        //************************************************************************
        //    Errorcodes (returnvalues)
        //************************************************************************
        public const short BER_k_OK = 0; //  success
        public const short BER_k_ERR = 1; //  general error
        public const short COP_k_NO_OBJECTS = 9; //  compatibility entry for COP_k_QUEUE_EMPTY
        public const short BER_k_DATA_CORRUPT = -41; //  corrupt data detected �C to PC
        public const short BER_k_NOT_SENT = -40; //  msg not sent; try again
        public const short BER_k_TIMEOUT = -38; //  timeout in communication PC to �C
        public const short BER_k_BOARD_ALREADY_USED = -37; //  board is used by another instance
        public const short BER_k_ALL_BOARDS_USED = -36; //  no free board slots inside DLL
        public const short BER_k_BOARD_NOT_SUPP = -35; //  the given board is not supported by CANopen Master API
        public const short BER_k_BOARD_NOT_FOUND = -34; //  the board wasn't found
        public const short BER_k_CANNOT_SEARCH_BOARD = -33; //  Hardware selection Dialog cancelled by user
        public const short BER_k_WRONG_FW = -32; //  wrong firmware version
        public const short BER_k_USED_FROM_OTHER_PROCESS = -31; //  board is used by another application
        public const short BER_k_PC_MC_COMM_ERR = -30; //  communication error PC to �C
        public const short BER_k_BOARD_DLD_ERR = -29; //  an error occured while firmware download
        public const short BER_k_BADCALLBACK_PTR = -28; //  a callbackpointer is invalid
        public const short BER_k_NO_SUCH_CANLINE = -27; //  given CANline is not available or not supported
        public const short BER_k_CANLINE_USED = -26; //  CANline is already in use
        public const short BER_k_VCI_INST_ERR = -25; //  IXXAT VCI driver missing
        public const short BER_k_BOARD_ERR = -24; //  unknown board type or can't locate board type

        public const short
            BER_k_MEM_ALLOC_ERR = -23; //  memory allocation error (Internal) data or OS element couldn't be created

        public const short BER_k_CCI_INST_ERR = -22; //  CCI installation error (Internal)
        public const short BER_k_SDO_INST_ERR = -21; //  SDO handler installation error (Internal)

        public const short
            BER_k_SDO_THREAD_ERR = -20; //  SDO thread execution cancelled while waiting for SDO response from master

        //*************************************************************************
        //  Constants for COP_InitBoard()
        //*************************************************************************
        public static readonly Guid COP_DEFAULTBOARD =
            new Guid("{0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}}");

        public static readonly Guid COP_BOARDDIALOG =
            new Guid("{0xFFFFFFFF,0xFFFF,0xFFFF,{0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF}}");

        public static readonly Guid COP_1stBOARD =
            new Guid("{0x00000000,0xFFFF,0xFFFF,{0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF}}");

        public static readonly Guid COP_2ndBOARD =
            new Guid("{0x00000001,0xFFFF,0xFFFF,{0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF}}");

        public static readonly Guid COP_3rdBOARD =
            new Guid("{0x00000002,0xFFFF,0xFFFF,{0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF}}");

        public static readonly Guid COP_4thBOARD =
            new Guid("{0x00000003,0xFFFF,0xFFFF,{0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF}}");

        public static readonly Guid COP_5thBOARD =
            new Guid("{0x00000004,0xFFFF,0xFFFF,{0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF}}");

        public static readonly Guid COP_6thBOARD =
            new Guid("{0x00000005,0xFFFF,0xFFFF,{0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF}}");

        public static readonly Guid COP_7thBOARD =
            new Guid("{0x00000006,0xFFFF,0xFFFF,{0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF}}");

        public static readonly Guid COP_8thBOARD =
            new Guid("{0x00000007,0xFFFF,0xFFFF,{0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF}}");

        public static readonly Guid COP_9thBOARD =
            new Guid("{0x00000008,0xFFFF,0xFFFF,{0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF}}");

        public const uint COP_FIRSTLINE = 0x00;
        public const uint COP_SECONDLINE = 0x01;
        public const uint COP_THIRDLINE = 0x02;
        public const uint COP_FOURTHLINE = 0x03;
        public const uint COP_SINGLELINE = 0xFF;
        public const uint COP_VCI3GENERIC = 0x100;

        //*************************************************************************
        //  Definitions for LSS-Configuration-Modes
        //*************************************************************************
        public const byte LSS_k_CLR_MODE_ALL = 0;
        public const byte LSS_k_SET_MODE_SWITCH_MODE_GLOBAL = 1;
        public const byte LSS_k_SET_MODE_STORE_CONFIGURATION = 2;
        public const byte LSS_k_SET_MODE_ACTIVATE_NEW_BAUDRATE = 4;

        //*************************************************************************
        //  LSS-function return values
        //*************************************************************************
        public const short LSS_k_MEDIA_ACCESS_ERROR = 4; //  CAN bus access failed
        public const short LSS_k_IV_PARAMETER = 5; //  invalid parameter
        public const short LSS_k_PROTOCOL_ERR = 7; //  invalid device response
        public const short LSS_k_BSY = 11; //  currently processing a LSS command sequence
        public const short LSS_k_FS_NO_NONCONFIGURED_SLAVE = 16; //  No non-configured slave responded
        public const short LSS_k_FS_NF_NONCONFIGURED_SLAVE = 18; //  Not Found the non-configured slave

        //************************************************************************
        //    Definitions of PDO/SDO base identifiers
        //************************************************************************
        public const ushort COP_k_ID_EMCY = 0x080;
        public const ushort COP_k_ID_GUARDING = 0x700;

        //  slave view
        public const ushort COP_k_S_ID_TxPDO1 = 0x180;
        public const ushort COP_k_S_ID_RxPDO1 = 0x200;
        public const ushort COP_k_S_ID_TxPDO2 = 0x280;
        public const ushort COP_k_S_ID_RxPDO2 = 0x300;
        public const ushort COP_k_S_ID_TxPDO3 = 0x380;
        public const ushort COP_k_S_ID_RxPDO3 = 0x400;
        public const ushort COP_k_S_ID_TxPDO4 = 0x480;
        public const ushort COP_k_S_ID_RxPDO4 = 0x500;

        public const ushort COP_k_S_ID_TxSDO = 0x580;
        public const ushort COP_k_S_ID_RxSDO = 0x600;

        //  master view
        public const ushort COP_k_M_ID_TxPDO1 = 0x200;
        public const ushort COP_k_M_ID_RxPDO1 = 0x180;
        public const ushort COP_k_M_ID_TxPDO2 = 0x300;
        public const ushort COP_k_M_ID_RxPDO2 = 0x280;
        public const ushort COP_k_M_ID_TxPDO3 = 0x400;
        public const ushort COP_k_M_ID_RxPDO3 = 0x380;
        public const ushort COP_k_M_ID_TxPDO4 = 0x500;
        public const ushort COP_k_M_ID_RxPDO4 = 0x480;

        public const ushort COP_k_M_ID_TxSDO = 0x600;
        public const ushort COP_k_M_ID_RxSDO = 0x580;

        #endregion

        #region copcmd Constants

        //************************************************************************
        // CMS-errorcodes
        //************************************************************************
        public const byte COP_k_OK = 0x00; // Success
        public const byte COP_k_NO = 0x01; // Common failure
        public const byte COP_k_CAL_ERR = 0x02; // Failure occured in CAL
        public const byte COP_k_IV = 0x03; // Invalid parameter
        public const byte COP_k_ABORT = 0x04; // Transfer aborted
        public const byte COP_k_NOT_FOUND = 0x05; // Node not found
        public const byte COP_k_NOT_INIT = 0x06; // CANopen-Master not initialised
        public const byte COP_k_INIT = 0x07; // CANopen-Master initialised

        public const byte COP_k_QUEUE_EMPTY = 0x09; // No Objects in Queue
        public const byte COP_k_TIMEOUT = 0x0a; // Timeout in CAN communication

        public const byte COP_k_SDO_RUNNING = 0x10; // SDO transfer in progress, retry later
        public const byte COP_k_BSY = 0x11; // Generic process still running (not finished so far)

        public const byte COP_k_NO_OBJECT = 0x12; // Object does not exist
        public const byte COP_k_NO_SUBINDEX = 0x13; // Subindex does not exist
        public const byte COP_k_WRITE_ONLY = 0x14; // Object is write only
        public const byte COP_k_PRESENT_DEVICE_STATE = 0x15; // Access actual not possible
        public const byte COP_k_RANGE_EXCEEDED = 0x16; // Parameter out of range
        public const byte COP_k_UNKNOWN = 0x20; // Unknown command
        public const byte COP_k_NO_FLY_MASTER_PRESENT = 0x21; // API/hardware version does not support flying master
        public const byte COP_k_NO_LOWSPEED = 0x22; // No LowSpeed bus-coupling present or supported

        //************************************************************************
        // Errortypes for COP_GetEvent
        //************************************************************************
        public const byte COP_k_NMT_EVT = 1;
        public const byte COP_k_DLL_EVT = 2;
        public const byte COP_k_WPDO_EVT = 3;
        public const byte COP_k_RPDO_EVT = 4;
        public const byte COP_k_QUEUE_OVRUN_EVT = 5;
        public const byte COP_k_FLY_EVT = 6;

        //************************************************************************
        //  Errorcodes for COP_GetEvent (E) of type COP_k_DLL_EVT
        //************************************************************************
        public const byte COP_k_DLL_NOERR = 0; // no error
        public const byte COP_k_DLL_RXOVR = 1; // software overrun (rx-queue)
        public const byte COP_k_DLL_COVR = 2; // CAN: overrun
        public const byte COP_k_DLL_BOFF = 4; // CAN: bus off
        public const byte COP_k_DLL_ESET = 8; // CAN: error-status-bit set
        public const byte COP_k_DLL_ERESET = 16; // CAN: error-status-bit reset
        public const byte COP_k_DLL_TXOVR = 32; // tx-queue full

        //************************************************************************
        //  Errorcodes for COP_GetEvent (E) of type COP_k_NMT_EVT
        //************************************************************************
        public const byte COP_k_NMT_GUARDERR = 1;
        public const byte COP_k_NMT_BOOTIND = 2;
        public const byte COP_k_NMT_HEARTBEATERR = 3;

        //************************************************************************
        // Errorcodes for COP_GetEvent (E) of type COP_k_FLY_EVT
        // Returncodes for Flying Master status (F) in COP_GetStatusFlyMasterNeg()
        //************************************************************************
        public const byte COP_k_FLY_MASTER = 4; //  E,F received mastership
        public const byte COP_k_FLY_NOT_MASTER = 5; //  E,F lost master negotiation
        public const byte COP_k_FLY_LOST_MASTERSHIP = 6; //  E   high prior node kicked master
        public const byte COP_k_FLY_LOST_ACTIVE_MASTER = 7; //  E   lost active master
        public const byte COP_k_FLY_UNKNOWN = 8; //  E   unknown event
        public const byte COP_k_FLY_WAIT_BUSCONNECTION = 9; //  F   waiting for busconnection
        public const byte COP_k_FLY_NEGOTIATION_RUNNING = 10; //  F   negotiation in progress

        //*************************************************************************
        // Errorcodes for COP_GetEvent of type COP_k_WPDO_EVT, COP_k_TPDO_EVT
        //*************************************************************************
        public const byte COP_k_ERR_PDO_IV = 1;
        public const byte COP_k_ERR_PDO_OVR = 2;

        //************************************************************************
        //  Definitions for CAN-baudrate settings
        //************************************************************************
        public const byte COP_k_BAUD_CIA = 0;
        public const byte COP_k_BAUD_USER = 0x80;

        public const byte COP_k_1000_KB = 0;
        public const byte COP_k_800_KB = 1;
        public const byte COP_k_500_KB = 2;
        public const byte COP_k_250_KB = 3;
        public const byte COP_k_125_KB = 4;
        public const byte COP_k_100_KB = 5;
        public const byte COP_k_50_KB = 6;
        public const byte COP_k_20_KB = 7;
        public const byte COP_k_10_KB = 8;

        //************************************************************************
        // Definitions for additional features
        // Not every feature is supported by every hardware
        // Used for COP_InitInterface() API command parameter "AddFeatures"
        // Bit set     => use feature
        // Bit cleared => do not use feature
        //************************************************************************
        public const byte COP_k_NO_FEATURES = 0;

        /* Bit 1 reserved for compatibility reasons */
        public const byte COP_k_FEATURE_FLYING_MASTER = 2;
        public const byte COP_k_FEATURE_LOWSPEED = 17;

        //************************************************************************
        // Definitions for Guarding or Heartbeat
        //************************************************************************
        public const byte COP_k_NODE_GUARDING = 0;
        public const byte COP_k_HEARTBEAT = 1;

        //************************************************************************
        // Defines of SDO numbers
        //************************************************************************
        // Use default Server SDO according
        public const byte COP_k_DEFAULT_SDO = 0x01; // to Predefined Connection Set

        // Use Server SDO that has been
        public const byte COP_k_USERDEFINED_SDO = 0x02; // declared in COP_CreateSDO()

        //************************************************************************
        // Defines of SDO modes
        //************************************************************************
        public const byte COP_k_NO_BLOCKTRANSFER = 0x00; // Do not use block transfer for SDO
        public const byte COP_k_BLOCKTRANSFER = 0x01; // Use block transfer for SDO

        //************************************************************************
        //  Defines of SDO access directions (download or upload)
        //************************************************************************
        public const byte COP_k_SDO_DOWNLOAD = 0x00; // Download SDO data
        public const byte COP_k_SDO_UPLOAD = 0x01; // Upload SDO data

        //************************************************************************
        // Defines for Firmware SDO segmentation of COP_k_MAX_SDO_SIZE
        //************************************************************************
        public const byte COP_k_NOMORE_SDO = 0x00; // Last data packet

        public const byte COP_k_MORE_SDO = 0x01; // More data packets to come

        // Error in block download, repeat transmission
        public const byte COP_k_REPEAT_SDO_SEGMENT = 0x02; // starting with following segment

        //************************************************************************
        // Definitions of PDO types
        //************************************************************************
        public const byte COP_k_PDO_TYP_RX = 0;
        public const byte COP_k_PDO_TYP_TX = 1;

        //************************************************************************
        // Definitions of PDO modes (Transmission Type)
        //************************************************************************
        public const byte COP_k_PDO_MODE_SYNC = 1;
        public const byte COP_k_PDO_MODE_ASYNC = 254;

        //************************************************************************
        // Definitions of node state return codes
        //************************************************************************
        public const byte COP_k_NS_BOOTUP = 0;
        public const byte COP_k_NS_DISCONNECTED = 1;
        public const byte COP_k_NS_STOPPED = 4;
        public const byte COP_k_NS_OPERATIONAL = 5;
        public const byte COP_k_NS_PREOPERATIONAL = 127;

        public const byte COP_k_NS_UNKNOWN = 255;

        //************************************************************************
        // Defines for TimeStamp control
        //************************************************************************
        public const byte COP_k_TS_START = 0;
        public const byte COP_k_TS_STOP = 1;

        //************************************************************************
        // Definitions of modes for synchronisation object
        //************************************************************************
        public const byte COP_k_BOTH_LINES = 2; // Command is applied to both lines (compatibility entry)
        public const byte COP_k_ALL_LINES = 2; // Command is applied to all lines
        public const byte COP_k_SINGLE_LINE = 3; // Command is applied to line addressed by boardhandle

        //************************************************************************
        //  Definitions of CCI queue numbers
        //************************************************************************
        // PC to microcontroller CAN0
        public const byte COP_P2M_QUEUE_COMMAND0 = 0;
        public const byte COP_P2M_QUEUE_SDO0 = 1;
        public const byte COP_P2M_QUEUE_PDO0 = 2;
        public const byte COP_P2M_QUEUE_SETTIME0 = 3;

        // PC to microcontroller CAN1
        public const byte COP_P2M_QUEUE_COMMAND1 = 4;
        public const byte COP_P2M_QUEUE_SDO1 = 5;
        public const byte COP_P2M_QUEUE_PDO1 = 6;
        public const byte COP_P2M_QUEUE_SETTIME1 = 3; // same value as COP_P2M_QUEUE_SETTIME0

        // PC to microcontroller CAN2
        public const byte COP_P2M_QUEUE_COMMAND2 = 7;
        public const byte COP_P2M_QUEUE_SDO2 = 8;
        public const byte COP_P2M_QUEUE_PDO2 = 9;
        public const byte COP_P2M_QUEUE_SETTIME2 = 3; //  same value as COP_P2M_QUEUE_SETTIME0

        // PC to microcontroller CAN3
        public const byte COP_P2M_QUEUE_COMMAND3 = 10;
        public const byte COP_P2M_QUEUE_SDO3 = 11;
        public const byte COP_P2M_QUEUE_PDO3 = 12;
        public const byte COP_P2M_QUEUE_SETTIME3 = 3; //  same value as COP_P2M_QUEUE_SETTIME0

        // microcontroller to PC CAN0
        public const byte COP_M2P_QUEUE_COMMAND0 = 0;
        public const byte COP_M2P_QUEUE_SDO0 = 1;
        public const byte COP_M2P_QUEUE_PDO0 = 2;
        public const byte COP_M2P_QUEUE_EMERGENCY0 = 3;
        public const byte COP_M2P_QUEUE_EVENT0 = 4;
        public const byte COP_M2P_QUEUE_SYNC0 = 5;

        // microcontroller to PC CAN1
        public const byte COP_M2P_QUEUE_COMMAND1 = 6;
        public const byte COP_M2P_QUEUE_SDO1 = 7;
        public const byte COP_M2P_QUEUE_PDO1 = 8;
        public const byte COP_M2P_QUEUE_EMERGENCY1 = 9;
        public const byte COP_M2P_QUEUE_EVENT1 = 10;
        public const byte COP_M2P_QUEUE_SYNC1 = 11;

        // microcontroller to PC CAN2
        public const byte COP_M2P_QUEUE_COMMAND2 = 12;
        public const byte COP_M2P_QUEUE_SDO2 = 13;
        public const byte COP_M2P_QUEUE_PDO2 = 14;
        public const byte COP_M2P_QUEUE_EMERGENCY2 = 15;
        public const byte COP_M2P_QUEUE_EVENT2 = 16;
        public const byte COP_M2P_QUEUE_SYNC2 = 17;

        // microcontroller to PC CAN3 */
        public const byte COP_M2P_QUEUE_COMMAND3 = 18;
        public const byte COP_M2P_QUEUE_SDO3 = 19;
        public const byte COP_M2P_QUEUE_PDO3 = 20;
        public const byte COP_M2P_QUEUE_EMERGENCY3 = 21;
        public const byte COP_M2P_QUEUE_EVENT3 = 22;
        public const byte COP_M2P_QUEUE_SYNC3 = 23;

        //************************************************************************
        //  Definitions of command opcodes
        //************************************************************************
        //************************************************************************
        //                            +-+---+
        //  Assembly of opcodes:      |f|fff|
        //                            +-+---+
        //                             |  |
        //                     +-------+  +------+
        //                     |                 |
        //                     V                 V
        //            module identifier | service opcode
        //                                       |
        //                                       V
        //                      client           |      server
        //                      -----------------+----------------
        //                      request      ----|---> indication
        //                      confirmation <---|---- response
        //
        //                      base no + 0 -> request
        //                      base no + 1 -> indication
        //                      base no + 2 -> response
        //                      base no + 3 -> confirmation
        //
        //************************************************************************

        // basic interface opcodes
        public const ushort COP_k_TESTCMD_REQ = 0x0000;
        public const ushort COP_k_TESTCMD_CON = 0x0003;

        public const ushort COP_k_STATUS_REQ = 0x0004;
        public const ushort COP_k_STATUS_CON = 0x0007;

        public const ushort COP_k_INIT_INTERFACE_REQ = 0x0008;
        public const ushort COP_k_INIT_INTERFACE_CON = 0x000b;

        public const ushort COP_k_FW_INFO_REQ = 0x000c;
        public const ushort COP_k_FW_INFO_CON = 0x000f;

        public const ushort COP_k_SHUTDOWN_REQ = 0x0010;
        public const ushort COP_k_SHUTDOWN_CON = 0x0013;

        public const ushort COP_k_SET_USERBITTIMING_REQ = 0x0014;
        public const ushort COP_k_SET_USERBITTIMING_CON = 0x0017;

        // network management opcodes
        public const ushort COP_k_ADD_NODE_REQ = 0x1000;
        public const ushort COP_k_ADD_NODE_CON = 0x1003;

        public const ushort COP_k_SEARCH_NODE_REQ = 0x1004;
        public const ushort COP_k_SEARCH_NODE_CON = 0x1007;

        public const ushort COP_k_DELETE_NODE_REQ = 0x1008;
        public const ushort COP_k_DELETE_NODE_CON = 0x100b;

        public const ushort COP_k_SET_OPERATIONAL_REQ = 0x100c;
        public const ushort COP_k_SET_OPERATIONAL_CON = 0x100f;

        public const ushort COP_k_SET_PREOPERTNL_REQ = 0x1010;
        public const ushort COP_k_SET_PREOPERTNL_CON = 0x1013;

        public const ushort COP_k_SET_PREPARED_REQ = 0x1018;
        public const ushort COP_k_SET_PREPARED_CON = 0x101b;

        public const ushort COP_k_RESET_COMM_REQ = 0x101c;
        public const ushort COP_k_RESET_COMM_CON = 0x101f;

        public const ushort COP_k_RESET_NODE_REQ = 0x1020;
        public const ushort COP_k_RESET_NODE_CON = 0x1023;

        public const ushort COP_k_GET_NODE_STATE_REQ = 0x1024;
        public const ushort COP_k_GET_NODE_STATE_CON = 0x1027;

        public const ushort COP_k_CHANGE_NODE_PARAM_REQ = 0x102c;
        public const ushort COP_k_CHANGE_NODE_PARAM_CON = 0x102f;

        public const ushort COP_k_EVENT_IND = 0x1031;

        public const ushort COP_k_GET_NODE_INFO_REQ = 0x1034; //  NEW6
        public const ushort COP_k_GET_NODE_INFO_CON = 0x1037; //  NEW6

        public const ushort COP_k_CONFIG_FLY_MASTER_REQ = 0x1050;
        public const ushort COP_k_CONFIG_FLY_MASTER_CON = 0x1053;

        public const ushort COP_k_START_MASTER_NEG_REQ = 0x1054;
        public const ushort COP_k_START_MASTER_NEG_CON = 0x1057;

        public const ushort COP_k_GET_STATUS_MASTER_NEG_REQ = 0x1058;
        public const ushort COP_k_GET_STATUS_MASTER_NEG_CON = 0x105b;

        public const ushort COP_k_CONFIG_SDM_REQ = 0x105c;
        public const ushort COP_k_CONFIG_SDM_CON = 0x105f;

        public const ushort COP_k_START_SDM_REQ = 0x1060;
        public const ushort COP_k_START_SDM_CON = 0x1063;

        // data object management
        public const ushort COP_k_CREATE_PDO_REQ = 0x3000;
        public const ushort COP_k_CREATE_PDO_CON = 0x3003;

        public const ushort COP_k_DELETE_PDO_REQ = 0x3004; //  NEW6
        public const ushort COP_k_DELETE_PDO_CON = 0x3007; //  NEW6

        public const ushort COP_k_DEF_SYNCOBJ_REQ = 0x3008;
        public const ushort COP_k_DEF_SYNCOBJ_CON = 0x300b;

        public const ushort COP_k_GET_SYNC_INFO_REQ = 0x300c; //  NEW6
        public const ushort COP_k_GET_SYNC_INFO_CON = 0x300f; //  NEW6

        public const ushort COP_k_ENABLE_SYNC_REQ = 0x3014;
        public const ushort COP_k_ENABLE_SYNC_CON = 0x3017;

        public const ushort COP_k_DISABLE_SYNC_REQ = 0x3018;
        public const ushort COP_k_DISABLE_SYNC_CON = 0x301b;

        public const ushort COP_k_CREATE_SPDTMOBJ_REQ = 0x302c;
        public const ushort COP_k_CREATE_SPDTMOBJ_CON = 0x302f;

        public const ushort COP_k_SET_SPEEDTIME_REQ = 0x3030;
        public const ushort COP_k_SET_SPEEDTIME_CON = 0x3033;

        public const ushort COP_k_EN_DIS_SPDTMOBJ_REQ = 0x3034;
        public const ushort COP_k_EN_DIS_SPDTMOBJ_CON = 0x3037;

        public const ushort COP_k_EN_DIS_TS_OBJ_REQ = 0x303c;
        public const ushort COP_k_EN_DIS_TS_OBJ_CON = 0x303f;

        public const ushort COP_k_SET_SDO_TMOUT_REQ = 0x3040;
        public const ushort COP_k_SET_SDO_TMOUT_CON = 0x3043;

        public const ushort COP_k_CREATE_SDO_REQ = 0x3044;
        public const ushort COP_k_CREATE_SDO_CON = 0x3047;

        public const ushort COP_k_SET_SYNCDIVISOR_REQ = 0x3048;
        public const ushort COP_k_SET_SYNCDIVISOR_CON = 0x304b;

        public const ushort COP_k_GET_TS_OBJ_REQ = 0x3050; //  NEW6
        public const ushort COP_k_GET_TS_OBJ_CON = 0x3053; //  NEW6

        public const ushort COP_k_GET_PDO_INFO_REQ = 0x3054; //  NEW6
        public const ushort COP_k_GET_PDO_INFO_CON = 0x3057; //  NEW6

        public const ushort COP_k_GET_SDO_INFO_REQ = 0x3058; //  NEW6
        public const ushort COP_k_GET_SDO_INFO_CON = 0x305b; //  NEW6

        public const ushort COP_k_SET_EMCY_ID_REQ = 0x3060; //  NEW6
        public const ushort COP_k_SET_EMCY_ID_CON = 0x3063; //  NEW6

        // basic data communication opcodes
        public const ushort COP_k_READ_SDO_REQ = 0x2000;
        public const ushort COP_k_READ_SDO_CON = 0x2003;

        public const ushort COP_k_WRITE_SDO_REQ = 0x2004;
        public const ushort COP_k_WRITE_SDO_CON = 0x2007;

        public const ushort COP_k_BLOCKREAD_SDO_REQ = 0x2020;
        public const ushort COP_k_BLOCKREAD_SDO_CON = 0x2023;

        public const ushort COP_k_BLOCKWRITE_SDO_REQ = 0x2024;
        public const ushort COP_k_BLOCKWRITE_SDO_CON = 0x2027;

        public const ushort COP_k_CANCEL_SDO_REQ = 0x2028;
        public const ushort COP_k_CANCEL_SDO_CON = 0x202B;

        public const ushort COP_k_RX_PDO_IND = 0x2005;

        public const ushort COP_k_WRITE_PDO_REQ = 0x0000; //  Dummy

        public const ushort COP_k_EMERGENCY_OBJ_IND = 0x2011;

        public const ushort COP_k_REQUEST_PDO_REQ = 0x2014;
        public const ushort COP_k_REQUEST_PDO_CON = 0x2017;

        // layer management opcodes
        public const ushort COP_k_REQ_LMT_INQUIRE_ADDRESS_MACRO = 0x4000;
        public const ushort COP_k_CON_LMT_INQUIRE_ADDRESS_MACRO = 0x4003;

        public const ushort COP_k_REQ_LMT_CONFIG_NODE_ID_MACRO = 0x4004;
        public const ushort COP_k_CON_LMT_CONFIG_NODE_ID_MACRO = 0x4007;

        public const ushort COP_k_REQ_LMT_CONFIG_BIT_TIMING_MACRO = 0x4008;
        public const ushort COP_k_CON_LMT_CONFIG_BIT_TIMING_MACRO = 0x400b;

        public const ushort COP_k_REQ_LMT_IDENTIFY_SLAVE_MACRO = 0x400c;
        public const ushort COP_k_CON_LMT_IDENTIFY_SLAVE_MACRO = 0x400f;

        // layer setting sevices opcodes (LSS)
        public const ushort COP_k_REQ_LSS_CONFIG_NODE_ID_MACRO = 0x4020;
        public const ushort COP_k_CON_LSS_CONFIG_NODE_ID_MACRO = 0x4023;

        public const ushort COP_k_REQ_LSS_CONFIG_BIT_TIMING_MACRO = 0x4024;
        public const ushort COP_k_CON_LSS_CONFIG_BIT_TIMING_MACRO = 0x4027;

        public const ushort COP_k_REQ_LSS_ACTIVATE_BIT_TIMING_MACRO = 0x4028;
        public const ushort COP_k_CON_LSS_ACTIVATE_BIT_TIMING_MACRO = 0x402b;

        public const ushort COP_k_REQ_LSS_IDENTIFY_SLAVE_MACRO = 0x402c;
        public const ushort COP_k_CON_LSS_IDENTIFY_SLAVE_MACRO = 0x402f;

        public const ushort COP_k_REQ_LSS_INQUIRE_ADDRESS_MACRO = 0x4030;
        public const ushort COP_k_CON_LSS_INQUIRE_ADDRESS_MACRO = 0x4033;

        public const ushort COP_k_REQ_LSS_INQUIRE_NODE_ID_MACRO = 0x4034;
        public const ushort COP_k_CON_LSS_INQUIRE_NODE_ID_MACRO = 0x4037;

        public const ushort COP_k_REQ_LSS_IDENTIFY_NON_CONFIG_SLAVE_MACRO = 0x4038;
        public const ushort COP_k_CON_LSS_IDENTIFY_NON_CONFIG_SLAVE_MACRO = 0x403b;

        public const ushort COP_k_REQ_LSS_SET_TIMEOUT = 0x403c;
        public const ushort COP_k_CON_LSS_SET_TIMEOUT = 0x403f;

        public const ushort COP_k_REQ_LSS_FASTSCAN = 0x4040; //  NEW6
        public const ushort COP_k_CON_LSS_FASTSCAN = 0x4043; //  NEW6

        #endregion

        #region copcmd Structures

        /// <summary>
        ///     Indication parameter block for COP_k_RX_PDO_IND
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct COP_t_RX_PDO
        {
            public byte node_no;
            public byte pdo_no;
            public byte length;
            public byte SyncCounter;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] a_data;
        }


        //************************************************************************
        //  Request parameter block for COP_k_WRITE_PDO_REQ
        //************************************************************************
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct COP_t_TX_PDO
        {
            public byte node_no;
            public byte pdo_no;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] a_data;
        }


        //************************************************************************
        //  Indication parameter block for COP_t_EMERGENCY_OBJ_IND
        //************************************************************************
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct COP_t_EMERGENCY_OBJ
        {
            public ushort err_value;
            public byte err_reg;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public byte[] err_data;

            public byte node_no;
        }


        //************************************************************************
        //  Indication parameter block for COP_k_EVENT_IND
        //************************************************************************
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct COP_t_EVENT_OBJ
        {
            public byte evt_type;
            public byte evt_data1;
            public byte evt_data2;
            public byte evt_data3;
            public byte evt_data4;
        }


        //************************************************************************
        //  Request parameter block for TimeStamp queue
        //************************************************************************
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct COP_t_TIMESTAMP_OBJ
        {
            public uint ms;
            public ushort days;
        }

        #endregion

        #region cop Export Function Delegates

        //************************************************************************
        //
        //    Function      : COP_t_EventCallback
        //
        //    Description   : Prototype for user callbackfunctions for receive
        //                    event signalization from the receive queues:
        //                    RxPDO           : que_num = COP_M2P_QUEUE_PDO0 or
        //                                                COP_M2P_QUEUE_PDO1
        //                    Emergency       : que_num = COP_M2P_QUEUE_EMERGENCY0 or
        //                                                COP_M2P_QUEUE_EMERGENCY1
        //                    Network/Status Event: que_num = COP_M2P_QUEUE_EVENT0 or
        //                                                    COP_M2P_QUEUE_EVENT1
        //                    Sync message    : que_num = COP_M2P_QUEUE_SYNC0 or
        //                                                COP_M2P_QUEUE_SYNC1
        //
        //    Parameter     : boardhdl        : handle of CAN board
        //                    que_num         : queue identifier
        //                    canline         : absolute CAN line number (0 or 1)
        //
        //    Returnvalues  : none
        //
        //************************************************************************
        public delegate void COP_t_EventCallback([In] ushort boardhdl
            , [In] byte que_num
            , [In] byte canline);

        //************************************************************************
        //
        //    Function      : COP_DefineCallbacks
        //
        //    Description   : Assign event callback functions to the different
        //                    receive queues.
        //                    If a function shouldn't be called, use NULL as
        //                    parameter.
        //
        //    Parameters    : boardhdl   (in) : handle of CAN board
        //                    fp_rx_pdo  (in) : this function will be called when a
        //                                      RxPDO was received
        //                                      (que_num = COP_M2P_QUEUE_PDO0 or
        //                                                 COP_M2P_QUEUE_PDO1)
        //                    fp_emergency (in) : this function will be called when a
        //                                      emergency message was received
        //                                      (que_num = COP_M2P_QUEUE_EMERGENCY0 or
        //                                                 COP_M2P_QUEUE_EMERGENCY1)
        //                    fp_net_event (in) : this function will be called when a
        //                                      network event occurs
        //                                      (que_num = COP_M2P_QUEUE_EVENT0 or
        //                                                 COP_M2P_QUEUE_EVENT1)
        //                    fp_sync    (in) : this function will be called when a
        //                                      synchronisation message was received
        //                                      (que_num = COP_M2P_QUEUE_SYNC0 or
        //                                                 COP_M2P_QUEUE_SYNC1)
        //
        //    Returnvalues  : BER_k_OK        : success
        //                    BER_k_ERR       : error
        //                    BER_k_BADCALLBACK_PTR:
        //                                      Callback function incorrect
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_DefineCallbacks([In] ushort boardhdl
            , [In] COP_t_EventCallback fp_rx_pdo
            , [In] COP_t_EventCallback fp_emergency
            , [In] COP_t_EventCallback fp_net_event
            , [In] COP_t_EventCallback fp_sync);

        //************************************************************************
        //
        //    Function      : COP_DefineMsgXXXX
        //
        //    Description   : Assign user defined windows messages or thread messages
        //                    to the different receive queues.
        //                    When an object is received by a particular queue, you
        //                    have the option to get a windows message, a thread
        //                    message or both.
        //                    If a receive queue shouldn't be handled (no more), use 0
        //                    as hWnd or idThread argument value.
        //                    There's a separate function for each receive queue.
        //                    When an change is detected, WINAPI function PostMessage()
        //                    resp. PostThreadMessage() will be called by MasterAPI DLL.
        //                    The eventmessage carries the boardhandle as wParam.
        //                    The eventmessage carries the queuenumber as lParam
        //                    (COP_M2P_QUEUE_PDO0 or COP_M2P_QUEUE_PDO1)
        //                    (COP_M2P_QUEUE_EVENT0 or COP_M2P_QUEUE_EVENT1)
        //                    (COP_M2P_QUEUE_EMERGENCY0 or COP_M2P_QUEUE_EMERGENCY1)
        //                    (COP_M2P_QUEUE_SYNC0 or COP_M2P_QUEUE_SYNC1)
        //                    The messagevalues must be above WM_USER.
        //
        //    Parameters    : boardhdl   (in) : handle of CAN board
        //                    hWnd       (in) : window handle of client application
        //                    idThread   (in) : thread id of sink thread
        //                    Msg        (in) : message identifier
        //                                      COP_DefineMsgRPDO - this message will
        //                                          be posted when a RxPDO was received
        //                                      COP_DefineMsgEvent - this message will
        //                                          be posted when a network event occurs
        //                                      COP_DefineMsgEmergency - this message
        //                                          will be posted when an emergency
        //                                          message was received
        //                                      COP_DefineMsgSync - this message will
        //                                          be posted when a synchronisation
        //                                          message was transmitted
        //
        //    Returnvalues  : COP_k_IV        : invalid parameter
        //                    BER_k_OK        : success
        //                    BER_k_ERR       : error
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_DefineMsgRPDO([In] ushort boardhdl
            , [In] IntPtr hWnd
            , [In] uint idThread
            , [In] uint Msg);

        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_DefineMsgEvent([In] ushort boardhdl
            , [In] IntPtr hWnd
            , [In] uint idThread
            , [In] uint Msg);

        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_DefineMsgEmergency([In] ushort boardhdl
            , [In] IntPtr hWnd
            , [In] uint idThread
            , [In] uint Msg);

        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_DefineMsgSync([In] ushort boardhdl
            , [In] IntPtr hWnd
            , [In] uint idThread
            , [In] uint Msg);

        //************************************************************************
        //
        //    Function      : COP_GetThreadIds
        //
        //    Description   : Return the Thread Identifiers of the internal queue
        //                    poll threads. This identifier can be used by the
        //                    application to gain access to the thread using Windows
        //                    API function OpenThread()
        //
        //    Parameters    : boardhdl   (in) : handle of CAN board
        //                    pPdoThreadId (out): identifier of the PDO queues poller
        //                                      (COP_M2P_QUEUE_PDO0 and
        //                                      COP_M2P_QUEUE_PDO1)
        //                    pEmcyThreadId (out): identifier of the Emergency queues
        //                                      poller (COP_M2P_QUEUE_EMERGENCY0 and
        //                                      COP_M2P_QUEUE_EMERGENCY1)
        //                    pEventThreadId (out): identifier of the network event
        //                                      queues poller (COP_M2P_QUEUE_EVENT0 and
        //                                      COP_M2P_QUEUE_EVENT1)
        //                    pSyncThreadId (out): identifier of the synchronisation
        //                                      message queues poller
        //                                      (COP_M2P_QUEUE_SYNC0 and
        //                                      COP_M2P_QUEUE_SYNC1)
        //
        //    Returnvalues  : BER_k_OK        : success
        //                    BER_k_ERR       : invalid boardhandle
        //                    BER_k_CCI_INST_ERR:
        //                                      board hasn't been initialised correctly
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_GetThreadIds([In] ushort boardhdl
            , [Out] out uint pPdoThreadId
            , [Out] out uint pEmcyThreadId
            , [Out] out uint pEventThreadId
            , [Out] out uint pSyncThreadId);

        //************************************************************************
        //
        //    Function      : COP_Reset_DLL
        //
        //    Description   : Completely reset the DLL.
        //                    This is useful for programming with interpreters
        //                    such as Visual Basic. If you stop debugging inside
        //                    interpreted code, the automatic cleanup won't be
        //                    called. So you have to use COP_Reset_DLL to perform
        //                    a post clean up.
        //                    Internally, COP_ReleaseBoard() is being called
        //
        //    Parameter     : none
        //    Returnvalues  : none
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern void COP_Reset_DLL();

        //************************************************************************
        //
        //    Function      : COP_InitBoard
        //
        //    Description   : Initialize CAN board, COP firmware and select CAN line
        //                    to use.
        //                    When you feed COP_DEFAULTBOARD to pBoardtype, the
        //                    default board according to IXXAT control panel applet
        //                    will be used.
        //                    When you feed COP_BOARDDIALOG to pBoardtype, the IXXAT
        //                    hardware selection dialog will open up.
        //                    You may feed COP_1stBOARD, COP_2ndBOARD... to pBoardID
        //                    in order to select the n-th instance of a specific board
        //                    type.
        //
        //    Parameter     : pBoardhdl  (out): pointer to handle of CAN board/line
        //                    pBoardtype (in/out) : Type of CAN board
        //                                      VCI3  DeviceClass acc.to vciguid.h
        //                                      VCI2  The type acc.to XatBrds.h shall
        //                                            be in pBoardtype->Data1
        //                    pBoardID   (in/out) : Unique global identifier of board
        //                                      VCI3  UniqueHardwareId.AsGuid
        //                                      VCI2  Former Regkey shall be in
        //                                            pBoardID->Data1
        //                    canLine    (in) : number of the CAN line to use:
        //                                      COP_FIRSTLINE - default (first CAN line)
        //                                      COP_SECONDLINE - second CAN line
        //                                      COP_THIRDLINE - third CAN line
        //                                      COP_FOURTHLINE - fourth CAN line
        //                                      COP_SINGLELINE - default (first CAN
        //                                            line), and no need for further CAN
        //                                            lines. Utilising faster alternative
        //                                            firmware.
        //
        //    Returnvalues  : BER_k_OK        : Success
        //                    BER_k_ERR       : General error
        //                    BER_k_BOARD_ALREADY_USED:
        //                                      Board is already in use
        //                    BER_k_ALL_BOARDS_USED:
        //                                      No more free board slot in DLL
        //                    BER_k_CANNOT_SEARCH_BOARD:
        //                                      IXXAT Hardware selection Dialog
        //                                      cancelled by user
        //                    BER_k_BOARD_NOT_FOUND:
        //                                      Given pBoardtype and pRegkey didn't
        //                                      match any local CAN board
        //                    BER_k_BOARD_NOT_SUPP:
        //                                      Local Boardtype isn't capable of
        //                                      running CANopen firmware
        //                    BER_k_WRONG_FW  : Wrong firmware version or initial
        //                                      communication with firmware failed
        //                    BER_k_USED_FROM_OTHER_PROCESS:
        //                                      Board is in use by another CAN
        //                                      application
        //                    BER_k_PC_MC_COMM_ERR:
        //                                      Communication between PC and CAN board
        //                                      failed
        //                    BER_k_BOARD_DLD_ERR:
        //                                      An error occured during firmware download
        //                    BER_k_NO_SUCH_CANLINE:
        //                                      CANline is not available or not
        //                                      supported
        //                    BER_k_CANLINE_USED:
        //                                      CANline is already in use
        //                    BER_k_VCI_INST_ERR:
        //                                      IXXAT VCI driver missing
        //                    BER_k_BOARD_ERR : Unknown board type or can't
        //                                      locate board type
        //                    BER_k_CCI_INST_ERR:
        //                                      CCI installation error (internal)
        //                    BER_k_SDO_INST_ERR:
        //                                      SDO handler installation error
        //                                      (internal)
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_InitBoard([Out] out ushort pBoardhdl
            , [In] [Out] ref Guid pBoardtype
            , [In] [Out] ref Guid pBoardID
            , [In] int canLine);

        //************************************************************************
        //
        //    Function      : COP_ReleaseBoard
        //
        //    Description   : Free resources for a board inside the DLL
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //
        //    Returnvalues  : none
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern void COP_ReleaseBoard([In] ushort boardhdl);

        //************************************************************************
        //
        //    Function      : COP_SendMsg
        //
        //    Description   : Place request message in transmit command queue
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    sp_message (in) : msg to transmit
        //
        //    Returnvalues  : BER_k_OK        : message sent
        //                    BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : message couldn't be handed over
        //
        //************************************************************************
        /* Todo

        [DllImport(CANopenMasterAPIDll)]
        public static extern Int16 COP_SendMsg( [In] UInt16          boardhdl
                                              , [In] COP_t_Message*  sp_message );
        */

        //************************************************************************
        //
        //    Function      : COP_GetMsg
        //
        //    Description   : Get response message from receive command queue
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    sp_message (out): buffer for msg
        //
        //    Returnvalues  : BER_k_OK        : message retrieved
        //                    BER_k_ERR       : boardhandle not valid
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    BER_k_PC_MC_COMM_ERR:
        //                                      Communication error PC to �C
        //                    BER_k_DATA_CORRUPT:
        //                                      Sequence number incorrect
        //
        //************************************************************************
        /* Todo

        [DllImport(CANopenMasterAPIDll)]
        public static extern Int16 COP_GetMsg( [In]      UInt16          boardhdl
                                             , [Out] out COP_t_Message*  sp_message);
        */

        //************************************************************************
        //
        //    Function      : COP_InitInterface
        //
        //    Description   : Intialize the CANopen-Master firmware
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    baudtable  (in) : COP_k_BAUD_CIA (standard)
        //                                      COP_k_BAUD_USER
        //                    baudrate   (in) : COP_k_10_KB
        //                                      COP_k_20_KB
        //                                      COP_k_50_KB
        //                                      COP_k_100_KB
        //                                      COP_k_125_KB
        //                                      COP_k_250_KB
        //                                      COP_k_500_KB
        //                                      COP_k_800_KB
        //                                      COP_k_1000_KB
        //                    node_no    (in) : node number of the master
        //                                      (0: feature not used)
        //                    hbTime     (in) : heartbeat time for the master
        //                    addFeatures(in) : Flagfield to switch several additional
        //                                      features in firmware,
        //                                      default value is COP_k_NO_FEATURES
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    BER_k_CANLINE_USED:
        //                                      CANline is already initialised
        //                    COP_k_OK        : success
        //                    COP_k_CAL_ERR   : CAL-Error
        //                    COP_k_IV        : Invalid parameter
        //                    COP_k_NO_FLY_MASTER_PRESENT:
        //                                      Flying master not supported
        //                    COP_k_NO_LOWSPEED:
        //                                      No LowSpeed bus-coupling present or
        //                                      supported
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_InitInterface([In] ushort boardhdl
            , [In] byte baudtable
            , [In] byte baudrate
            , [In] byte node_no
            , [In] ushort hbTime
            , [In] ushort addFeatures);

        //************************************************************************
        //
        //    Function      : COP_TestCommand
        //
        //    Description   : Function to test the communication between Master API
        //                    DLL and Masterkernel
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    BER_k_DATA_CORRUPT:
        //                                      corrupt data received from firmware
        //                    COP_k_OK        : communication ok
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_TestCommand([In] ushort boardhdl);

        //************************************************************************
        //
        //    Function      : COP_GetStatus
        //
        //    Description   : Returns the state of the CANopen Master API firmware
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    state_master (out): state of master
        //                    state_err_dll(out): state of master firmware
        //                                        data link layer
        //
        //    Returnvalues  : BER_k_OK        : success
        //                    BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_GetStatus([In] ushort boardhdl
            , [Out] out byte state_master
            , [Out] out byte state_err_dll);

        //************************************************************************
        //
        //    Function      : COP_SetCommTimeOut
        //
        //    Description   : Change the timeout for response messages from command
        //                    queue in milliseconds.
        //                    When attempting to retrieve a message using COP_GetMsg()
        //                    this timeout value determines how long to wait.
        //                    COP_GetMsg() is used internally in nearly all CANopen
        //                    Master API functions.
        //
        //    Parameters    : boardhdl   (in) : handle of CAN board
        //                    w_timeout  (in) : new timeoutvalue in milliseconds
        //                                      (lowest possible value 55 ms)
        //
        //    Returnvalue   : BER_k_OK        : success
        //                    BER_k_ERR       : wrong board handle
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_SetCommTimeOut([In] ushort boardhdl
            , [In] ushort w_timeout);

        //************************************************************************
        //
        //    Function      : COP_GetBoardInfo
        //
        //    Description   : Returns information about HW and SW
        //                    COP_BOARD_INFO:
        //                    - hardware version of board
        //                    - version of board firmware
        //                    - PC software version
        //                    - memory segment of board (legacy)
        //                      A value of 0x100 signals usage of VCI3 generic firmware
        //                    - IRQ of board (legacy)
        //                    - number of CAN controllers
        //                    - serial number of board e.g.: "HW123456"
        //                      (16 characters).
        //                    - HW identification e.g. "USB-to-CAN compact"
        //                      (40 characters).
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    sp_info    (out): pointer to information data
        //
        //    Returnvalues  : BER_k_OK        : success
        //                    BER_k_ERR       : error
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_GetBoardInfo([In] ushort boardhdl
            , [Out] out COP_BOARD_INFO sp_info);

        //************************************************************************
        //
        //    Function      : COP_CreatePDO
        //
        //    Description   : Create a new PDO
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (in) : number of the node
        //                                      1 <= node_no <= 127
        //                    pdo_no     (in) : number of the pdo
        //                    type       (in) : type of pdo (direction)
        //                                      (COP_k_PDO_TYP_RX, .._TX)
        //                    mode       (in) : transmission mode of pdo
        //                                      (COP_k_PDO_MODE_SYNC, .._ASYNC)
        //                                      0 <= mode <= 254
        //                                      0..240  synchronous
        //                                      254     asynchronous
        //                    length     (in) : datalength of pdo
        //                                      0 <= length <= 8
        //                    canid      (in) : CANID of pdo
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_CAL_ERR   : CAL-Error
        //                    COP_k_IV        : invalid parameter
        //                    COP_k_NOT_FOUND : node not present in network
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_CreatePDO([In] ushort boardhdl
            , [In] byte node_no
            , [In] byte pdo_no
            , [In] byte type
            , [In] byte mode
            , [In] byte length
            , [In] ushort canid);

        //************************************************************************
        //
        //    Function      : COP_DeletePDO
        //
        //    Description   : Delete a PDO
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (in) : number of the node
        //                                      1 <= node_no <= 127
        //                    pdo_no     (in) : number of the pdo
        //                    type       (in) : type of pdo (direction)
        //                                      (COP_k_PDO_TYP_RX, .._TX)
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_IV        : invalid parameter
        //                    COP_k_NOT_FOUND : node not present in network
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_DeletePDO([In] ushort boardhdl
            , [In] byte node_no
            , [In] byte pdo_no
            , [In] byte type);

        //************************************************************************
        //
        //    Function      : COP_GetPDOInfo
        //
        //    Description   : Deliver attributes of a PDO
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (in) : number of the node
        //                                      1 <= node_no <= 127
        //                    pdo_no     (in) : number of the pdo
        //                    type       (in) : type of pdo (direction)
        //                                      (COP_k_PDO_TYP_RX, .._TX)
        //                    mode       (out): transmission mode of pdo
        //                                      (COP_k_PDO_MODE_SYNC, .._ASYNC)
        //                                      0 <= mode <= 254
        //                                      0..240  synchronous
        //                                      254     asynchronous
        //                    length     (out): datalength of pdo
        //                                      0 <= length <= 8
        //                    canid      (out): CANID of pdo
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_IV        : invalid parameter
        //                    COP_k_NOT_FOUND : node not present in network
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_GetPDOInfo([In] ushort boardhdl
            , [In] byte node_no
            , [In] byte pdo_no
            , [In] byte type
            , [Out] out byte mode
            , [Out] out byte length
            , [Out] out ushort canid);

        //************************************************************************
        //
        //    Function      : COP_CreateSDO
        //
        //    Description   : Create a new SDO
        //                    The Server-SDOs of the Predefined Connection Set exist
        //                    by default, so only additional SDOs must be created.
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (in) : number of the node
        //                    sdo_no     (in) : number of the sdo,
        //                                      always = COP_k_USERDEFINED_SDO
        //                    clientcanid(in) : CANID for SDO request
        //                                      (Master is client)
        //                    servercanid(in) : CANID for SDO response
        //                                      (Node is server)
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_CAL_ERR   : CAL-Error
        //                    COP_k_IV        : invalid parameter
        //                    COP_k_NOT_FOUND : node not present in network
        //                    COP_k_SDO_RUNNING:
        //                                      SDO transfer in progress, retry later
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_CreateSDO([In] ushort boardhdl
            , [In] byte node_no
            , [In] byte sdo_no
            , [In] ushort clientcanid
            , [In] ushort servercanid);

        //************************************************************************
        //
        //    Function      : COP_GetSDOInfo
        //
        //    Description   : Deliver attributes of a SDO
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (in) : number of the node
        //                    sdo_no     (in) : number of the sdo:
        //                                      COP_k_DEFAULT_SDO or
        //                                      COP_k_USERDEFINED_SDO
        //                    clientcanid(out): CANID for SDO request
        //                                      (Master is client)
        //                    servercanid(out): CANID for SDO response
        //                                      (Node is server)
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_IV        : invalid parameter
        //                    COP_k_NOT_FOUND : node not present in network
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_GetSDOInfo([In] ushort boardhdl
            , [In] byte node_no
            , [In] byte sdo_no
            , [Out] out ushort clientcanid
            , [Out] out ushort servercanid);

        //************************************************************************
        //
        //    Function      : COP_DefSyncObj
        //
        //    Description   : Initialize the synchronisation object of the CAN board
        //                    --++-------------+---------++-------------+--->t
        //                      || sync window |         || sync window |
        //                      ||-------------|         ||-------------|
        //                      || divisor * sync period ||
        //                      ||-----------------------||
        //                    Please note that all CAN lines of a board share the same
        //                    sync_period.  Thus, for different sync intervals on the
        //                    lines, a so-called divisor must be set for each line (see
        //                    also COP_SetSyncDivisor).  Hence, the sync_period given
        //                    here is the greatest common divisor gcd of all lines'
        //                    sync intervals.
        //                    Contrary to the sync_period, the CounterOverflow value
        //                    is individual to each CAN line.
        //                    Calling this function turns OFF a possibly already
        //                    running sync object of the CAN line.
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    sync_period(in) : base interval of synchronisation
        //                                      message (in ms)
        //                                      2 <= sync_period <= 65280
        //                                      default value is 1000
        //                    sync_window(in) : width of the synchronisation window
        //                                      (in ms)
        //                                      2 <= sync_window <= sync_period
        //                                      Reserved for future use
        //                    counteroverflow    (in) : sync counter overflow value
        //                                              of the CAN line,
        //                                              acc.to. [1019sub0]
        //                                              0; 2 <= CounterOverflow <= 240
        //                                              default value is 0
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_CAL_ERR   : CAL-Error
        //                    COP_k_IV        : invalid parameter
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_DefSyncObj([In] ushort boardhdl
            , [In] ushort sync_period
            , [In] ushort sync_window
            , [In] byte counteroverflow);

        //************************************************************************
        //
        //    Function      : COP_GetSyncInfo
        //
        //    Description   : Deliver attributes of the synchronisation object of
        //                    the CAN line
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    sync_period(out): base interval of synchronisation
        //                                      message (in ms)
        //                    sync_window(out): width of the synchronisation window
        //                                      (in ms)
        //                    counteroverflow    (out): sync counter overflow value,
        //                                              acc.to. [1019sub0]
        //                    divisor    (out): Factor of common sync_period for
        //                                      the individual CAN line (see also
        //                                      COP_SetSyncDivisor)
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_GetSyncInfo([In] ushort boardhdl
            , [Out] out ushort sync_period
            , [Out] out ushort sync_window
            , [Out] out byte counteroverflow
            , [Out] out byte divisor);

        //************************************************************************
        //
        //    Function      : COP_SetSyncDivisor
        //
        //    Description   : Define a divisor for the synchronisation objects'
        //                    frequency.
        //                    Because all CAN lines on one board share the same
        //                    sync_period as defined in COP_DefSyncObj(), the sync
        //                    divisor is useful to generate several sync intervals
        //                    on the different CAN lines.
        //                    Given a sync_period of e.g. 10ms, a divisor 10 on
        //                    CAN line 0 would trigger the sync object every 100 ms,
        //                    whereas as divisor of 3 on CAN line 1 would trigger
        //                    the sync object in a 30 ms interval on CAN line 1.
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    divisor    (in) : Factor of common sync_period for
        //                                      the individual CAN line
        //                                      default value is 1
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_CAL_ERR   : CAL-Error
        //                    COP_k_IV        : invalid parameter
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_SetSyncDivisor([In] ushort boardhdl
            , [In] byte divisor);

        //************************************************************************
        //
        //    Function      : COP_EnableSync
        //
        //    Description   : Enable cyclic transmission of synchronization objects
        //                    of the CAN board
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    mode       (in) : operating modes for non-single CAN
        //                                      configurations:
        //                                      COP_k_SINGLE_LINE / COP_k_ALL_LINES
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_CAL_ERR   : CAL-Error
        //                    COP_k_IV        : invalid parameter
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_EnableSync([In] ushort boardhdl
            , [In] byte mode);

        //************************************************************************
        //
        //    Function      : COP_DisableSync
        //
        //    Description   : Disable cyclic transmission of synchronization objects
        //                    of the CAN board
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    mode       (in) : operating modes for non-single CAN
        //                                      configurations:
        //                                      COP_k_SINGLE_LINE / COP_k_ALL_LINES
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_CAL_ERR   : CAL-Error
        //                    COP_k_IV        : invalid parameter
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_DisableSync([In] ushort boardhdl
            , [In] byte mode);

        //************************************************************************
        //
        //    Function      : COP_InitTimeStampObj
        //
        //    Description   : Initialize the timestamp object of the CAN board
        //                    (The time basis is the same for all CAN lines)
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    ms         (in) : ms after midnight
        //                    days       (in) : days from 1. January  1984
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_IV        : invalid time
        //                    COP_k_BSY       : Queue is full
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_InitTimeStampObj([In] ushort boardhdl
            , [In] uint ms
            , [In] ushort days);

        //************************************************************************
        //
        //    Function      : COP_GetTimeStampObj
        //
        //    Description   : Deliver attributes and current value of the timestamp
        //                    object of the CAN board
        //                    (The time basis is the same for all CAN lines)
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    startstop  (out): COP_k_TS_START or COP_k_TS_STOP
        //                    cycle      (out): cycletime of transmission in ms
        //                    ms         (out): ms after midnight
        //                                      Value is 0 if COP_InitTimeStampObj()
        //                                      had not been called yet
        //                    days       (out): days from 1. January 1984
        //                                      Value is 0 if COP_InitTimeStampObj()
        //                                      had not been called yet
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_GetTimeStampObj([In] ushort boardhdl
            , [Out] out byte startstop
            , [Out] out ushort cycle
            , [Out] out uint ms
            , [Out] out ushort days);

        //************************************************************************
        //
        //    Function      : COP_StartStopTSObj
        //
        //    Description   : Start or stop cyclic transmission of timestamp objects
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    startstop  (in) : COP_k_TS_START or COP_k_TS_STOP
        //                    cycle      (in) : cycletime of transmission in ms
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_CAL_ERR   : CAL-Error
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_StartStopTSObj([In] ushort boardhdl
            , [In] byte startstop
            , [In] ushort cycle);

        //************************************************************************
        //
        //    Function      : COP_SetSDOTimeOut
        //
        //    Description   : Change the SDO timeout value. Value in milliseconds.
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    w_timeout  (in) : new timeout in ms
        //                                      default value is 200
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_CAL_ERR   : CAL-Error
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_SetSDOTimeOut([In] ushort boardhdl
            , [In] ushort w_timeout);

        //************************************************************************
        //
        //    Function      : COP_AddNode
        //
        //    Description   : Declare a new node
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (in) : number of new node
        //                    NgOrHb     (in) : node guarding or heartbeat:
        //                                      COP_k_NODE_GUARDING
        //                                      COP_k_HEARTBEAT
        //                    GuardHeartbeatTime (in) : time between two guard
        //                                              requests in ms  resp.
        //                                              time between two heartbeat
        //                                              transmissions in ms
        //                    lifetimefactor     (in) : only for node guarding:
        //                                              how many guard reqests may
        //                                              remain unanswered without
        //                                              error indication
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_CAL_ERR   : CAL-Error
        //                    COP_k_IV        : invalid parameter
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_AddNode([In] ushort boardhdl
            , [In] byte node_no
            , [In] byte NgOrHb
            , [In] ushort GuardHeartbeatTime
            , [In] byte lifetimefactor);

        //************************************************************************
        //
        //    Function      : COP_DeleteNode
        //
        //    Description   : Removes a node from the masters network management
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (in) : number of the node to be removed
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_NOT_FOUND : unknown node
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_DeleteNode([In] ushort boardhdl
            , [In] byte node_no);

        //************************************************************************
        //
        //    Function      : COP_SearchNode
        //
        //    Description   : Check whether a declared node is present in network
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (in) : number of the node to be searched
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_CAL_ERR   : CAL-Error
        //                    COP_k_IV        : invalid parameter
        //                    COP_k_NOT_FOUND : node not present in network
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_SearchNode([In] ushort boardhdl
            , [In] byte node_no);

        //************************************************************************
        //
        //    Function      : COP_ChangeNodeParameter
        //
        //    Description   : Change attributes of a node already registered with
        //                    COP_AddNode() resp.
        //                    Change heartbeat time of the Master
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (in) : number of the node (1..127)
        //                    NgOrHb     (in) : node guarding or heartbeat:
        //                                      COP_k_NODE_GUARDING
        //                                      COP_k_HEARTBEAT
        //                    GuardHeartbeatTime (in) : time between two guard
        //                                              requests in ms  resp.
        //                                              time between two heartbeat
        //                                              transmissions in ms
        //                    lifetimefactor     (in) : only for node guarding:
        //                                              how many guard reqests may
        //                                              remain unanswered without
        //                                              error indication
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_IV        : invalid parameter
        //                    COP_k_NOT_FOUND : node not present in network
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_ChangeNodeParameter([In] ushort boardhdl
            , [In] byte node_no
            , [In] byte NgOrHb
            , [In] ushort GuardHeartbeatTime
            , [In] byte lifetimefactor);

        //************************************************************************
        //
        //    Function      : COP_GetNodeInfo
        //
        //    Description   : Deliver attributes of a node already registered with
        //                    COP_AddNode()
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (in) : number of the node (1..127)
        //                    NgOrHb     (out): node guarding or heartbeat:
        //                                      COP_k_NODE_GUARDING
        //                                      COP_k_HEARTBEAT
        //                    GuardHeartbeatTime (out): time between two guard
        //                                              requests in ms  resp.
        //                                              time between two heartbeat
        //                                              transmissions in ms
        //                    lifetimefactor     (out): only for node guarding:
        //                                              how many guard requests may
        //                                              remain unanswered without
        //                                              error indication
        //                    EmcyIdentifier     (out): CAN Identifier of Emergency
        //                                              object
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_IV        : invalid parameter
        //                    COP_k_NOT_FOUND : node not present in network
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_GetNodeInfo([In] ushort boardhdl
            , [In] byte node_no
            , [Out] out byte NgOrHb
            , [Out] out ushort GuardHeartbeatTime
            , [Out] out byte lifetimefactor
            , [Out] out ushort EmcyIdentifier);

        //************************************************************************
        //
        //    Function      : COP_SetEmcyIdentifier
        //
        //    Description   : Configure the CAN identifier used by a node for
        //                    its Emergency object
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (in) : number of the node (1..127)
        //                    EmcyIdentifier     (in) : CAN Identifier of Emergency
        //                                              object
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_IV        : invalid parameter
        //                    COP_k_CAL_ERR   : CAL-Error
        //                    COP_k_NOT_FOUND : node not present in network
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_SetEmcyIdentifier([In] ushort boardhdl
            , [In] byte node_no
            , [In] ushort EmcyIdentifier);

        //************************************************************************
        //
        //    Function      : COP_StartNode
        //
        //    Description   : Start one or all nodes
        //                    NMT command 'Start Remote Node'
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (in) : number of the target node
        //                                      (0 = entire network)
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_NOT_FOUND : unknown node
        //                    COP_k_IV        : invalid nodeID
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_StartNode([In] ushort boardhdl
            , [In] byte node_no);

        //************************************************************************
        //
        //    Function      : COP_StopNode
        //
        //    Description   : Stop one or all nodes
        //                    NMT command 'Stop Remote Node'
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (in) : number of the target node
        //                                      (0 = entire network)
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_NOT_FOUND : unknown node
        //                    COP_k_IV        : invalid nodeID
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_StopNode([In] ushort boardhdl
            , [In] byte node_no);

        //************************************************************************
        //
        //    Function      : COP_EnterPreOperational
        //
        //    Description   : Change the state of the node(s) to Pre-Operational
        //                    NMT command 'Enter Pre-Operational'
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (in) : number of the target node
        //                                      (0 = entire network)
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_NOT_FOUND : unknown node
        //                    COP_k_IV        : invalid nodeID
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_EnterPreOperational([In] ushort boardhdl
            , [In] byte node_no);

        //************************************************************************
        //
        //    Function      : COP_ResetComm
        //
        //    Description   : Reset communication profile of a node
        //                    NMT command 'Reset Communication'
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (in) : number of the target node
        //                                      (0 = entire network)
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_NOT_FOUND : unknown node
        //                    COP_k_IV        : invalid nodeID
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_ResetComm([In] ushort boardhdl
            , [In] byte node_no);

        //************************************************************************
        //
        //    Function      : COP_ResetNode
        //
        //    Description   : Reset application and communication profile of a node
        //                    NMT command 'Reset Node'
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (in) : number of the target node
        //                                      (0 = entire network)
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_NOT_FOUND : unknown node
        //                    COP_k_IV        : invalid nodeID
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_ResetNode([In] ushort boardhdl
            , [In] byte node_no);

        //************************************************************************
        //
        //    Function      : COP_GetNodeState
        //
        //    Description   : Return the state of a node
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (in) : number of the node
        //                    node_state (out): address of node state:
        //                                      COP_k_NS_BOOTUP
        //                                      COP_k_NS_DISCONNECTED
        //                                      COP_k_NS_STOPPED
        //                                      COP_k_NS_OPERATIONAL
        //                                      COP_k_NS_PREOPERATIONAL
        //                                      COP_k_NS_UNKNOWN
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_CAL_ERR   : CAL-Error
        //                    COP_k_IV        : invalid nodeID
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_GetNodeState([In] ushort boardhdl
            , [In] byte node_no
            , [Out] out ushort node_state);

        //************************************************************************
        //
        //    Function      : COP_GetEvent
        //
        //    Description   : Fetch an event from the event queue
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    evt_type   (out): kind of event
        //                                      COP_k_NMT_EVT -> network event
        //                                      COP_k_DLL_EVT -> data link layer event
        //                                      COP_k_WPDO_EVT -> COP_WritePDO() event
        //                                      COP_k_RPDO_EVT -> RxPDO-queue event
        //                                      COP_k_QUEUE_OVRUN_EVT -> queue overrun
        //                                      COP_k_FLY_EVT -> Flying Master event
        //                    evt_data1  (out): COP_k_NMT_EVT -> event cause;
        //                                        one of COP_k_NMT_aaaa
        //                                      COP_k_DLL_EVT -> current status;
        //                                        set of COP_k_DLL_aaaa
        //                                      COP_k_WPDO_EVT,
        //                                      COP_k_RPDO_EVT -> event cause;
        //                                        one of COP_k_ERR_PDO_aaaa
        //                                      COP_k_QUEUE_OVRUN_EVT -> EMCY overrun count
        //                                      COP_k_FLY_EVENT -> event cause;
        //                                        one of COP_k_FLY_aaaa
        //                    evt_data2  (out): COP_k_NMT_EVT -> node id
        //                                      COP_k_WPDO_EVT,
        //                                      COP_k_RPDO_EVT -> node id of request
        //                                      COP_k_QUEUE_OVRUN_EVT -> RxPDO overrun count
        //                    evt_data3  (out): COP_k_WPDO_EVT,
        //                                      COP_k_RPDO_EVT -> pdo number of request
        //                                      COP_k_QUEUE_OVRUN_EVT -> Event overrun count
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_OK        : success
        //                    COP_k_QUEUE_EMPTY:no entry in queue
        //                    COP_k_CAL_ERR   : General failure on CCI access
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_GetEvent([In] ushort boardhdl
            , [Out] out byte evt_type
            , [Out] out byte evt_data1
            , [Out] out byte evt_data2
            , [Out] out byte evt_data3);

        //************************************************************************
        //
        //    Function      : COP_RequestPDO
        //
        //    Description   : Request a PDO transmission from a node.
        //                    The PDO must be read with COP_ReadPDO.
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (in) : number of the node
        //                    pdo_no     (in) : number of the pdo
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_BSY       : Transmit queue for CAN is full
        //                    COP_k_CAL_ERR   : CAL-Error
        //                    COP_k_IV        : invalid parameter
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_RequestPDO([In] ushort boardhdl
            , [In] byte node_no
            , [In] byte pdo_no);

        //************************************************************************
        //
        //    Function      : COP_ReadPDO
        //
        //    Description   : Get a PDO entry from the PDO receive queue in
        //                    separate parameters
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (out): number of the node
        //                    pdo_no     (out): number of the pdo
        //                    rxlen      (out): length of received data
        //                    rxdata     (out): buffer for received data, must be
        //                                      8 bytes size
        //                    SyncCounter(out): sync counter value upon reception
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_OK        : success
        //                    COP_k_QUEUE_EMPTY:No Objects in queue
        //                    COP_k_IV        : invalid parameter
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_ReadPDO([In] ushort boardhdl
            , [Out] out byte node_no
            , [Out] out byte pdo_no
            , [Out] out byte rxlen
            , [Out] byte[] rxdata
            , [Out] out byte SyncCounter);

        //************************************************************************
        //
        //    Function      : COP_ReadPDO_S
        //
        //    Description   : Get a PDO entry from the PDO receive queue in
        //                    a single structure
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    sp_pdo     (out): received PDO
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_OK        : success
        //                    COP_k_QUEUE_EMPTY:No Objects in queue
        //                    COP_k_IV        : invalid parameter
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_ReadPDO_S([In] ushort boardhdl
            , [Out] out COP_t_RX_PDO sp_pdo);

        //************************************************************************
        //
        //    Function      : COP_GetEmergencyObj
        //
        //    Description   : Fetch an emergency object from the emergency queue
        //                    in separate parameters
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (out): number of the node
        //                    err_value  (out): error code of emergency object
        //                    err_register (out) : error register of emergency object
        //                    err_data   (out): error data of emergency object
        //                                      (5 bytes)
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_OK        : success
        //                    COP_k_QUEUE_EMPTY:No emergency objects in queue
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_GetEmergencyObj([In] ushort boardhdl
            , [Out] out byte node_no
            , [Out] out ushort err_value
            , [Out] out byte err_register
            , [Out] byte[] err_data);

        //************************************************************************
        //
        //    Function      : COP_GetEmergencyObj_S
        //
        //    Description   : Fetch an emergency object from the EMCY queue in
        //                    a single structure
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    sp_emergency (out): received emergency object
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_OK        : success
        //                    COP_k_QUEUE_EMPTY:No emergency objects in queue
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_GetEmergencyObj_S([In] ushort boardhdl
            , [Out] out COP_t_EMERGENCY_OBJ sp_emergency);

        //************************************************************************
        //
        //    Function      : COP_CheckSync
        //
        //    Description   : Check whether firmware has signaled a sync
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    SyncCounter(out): sync counter value
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_PC_MC_COMM_ERR:
        //                                      Communication error PC to �C
        //                    COP_k_OK        : sync signaled
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_CheckSync([In] ushort boardhdl
            , [Out] out byte SyncCounter);

        //************************************************************************
        //
        //    Function      : COP_WritePDO
        //
        //    Description   : Place a PDO entry in the PDO transmit queue in
        //                    separate parameters
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (in) : number of the node
        //                    pdo_no     (in) : number of the pdo
        //                    txdata     (in) : data to transmit, must be 8 bytes
        //                                      size
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_OK        : success
        //                    COP_k_IV        : invalid txdata
        //                    COP_k_BSY       : Queue is full
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_WritePDO([In] ushort boardhdl
            , [In] byte node_no
            , [In] byte pdo_no
            , [MarshalAs(UnmanagedType.LPArray, SizeConst = 8)] [In]
            byte[] txdata);

        //************************************************************************
        //
        //    Function      : COP_WritePDO_S
        //
        //    Description   : Place a PDO entry in the PDO transmit queue in a
        //                    single structure
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    sp_pdo     (in) : PDO to transmit
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_OK        : success
        //                    COP_k_IV        : invalid txdata buffer
        //                    COP_k_BSY       : Queue is full
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_WritePDO_S([In] ushort boardhdl
            , [In] COP_t_TX_PDO sp_pdo);

        //************************************************************************
        //
        //    Function      : COP_ReadSDO
        //
        //    Description   : Initiate and execute a SDO upload from a node.
        //                    When given buffer size rxlen is insufficient, the buffer
        //                    will be filled up to it's capacity limit and the total
        //                    number of necessary bytes will be returned in rxlen.
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (in) : number of the node
        //                    sdo_no     (in) : number of the sdo:
        //                                      COP_k_DEFAULT_SDO or
        //                                      COP_k_USERDEFINED_SDO
        //                                      Shall the user defined SDO be used, it
        //                                      must already have been created with
        //                                      COP_CreateSDO
        //                    mode       (in) : COP_k_NO_BLOCKTRANSFER
        //                                      COP_k_BLOCKTRANSFER
        //                    idx        (in) : index in OV
        //                    subidx     (in) : subindex in OV
        //                    rxlen   (in/out): size of buffer for received data
        //                                      / number of received data bytes
        //                    rxdata     (out): received data (max 2^32 bytes)
        //                    abortcode  (out): abort code of SDO-transfer
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_MEM_ALLOC_ERR:
        //                                      internal data couldn't be created
        //                    BER_k_SDO_THREAD_ERR:
        //                                      Thread execution cancelled
        //                    BER_k_OK        : success
        //                    BER_k_PC_MC_COMM_ERR:
        //                                      Communication error PC to �C
        //                    COP_k_IV        : invalid parameter
        //                    COP_k_NOT_FOUND : node not present in network
        //                    COP_k_TIMEOUT   : SDO response timeout expired
        //                    COP_k_ABORT     : SDO transfer aborted
        //                    COP_k_BSY       : SDO transfer discarded (Transmit
        //                                      queue of the CAN is full)
        //                    COP_k_QUEUE_EMPTY:
        //                                      No SDO response from master
        //                    COP_k_SDO_RUNNING:
        //                                      Thread is still busy, retry later
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_ReadSDO([In] ushort boardhdl
            , [In] byte node_no
            , [In] byte sdo_no
            , [In] byte mode
            , [In] ushort idx
            , [In] byte subidx
            , [In] [Out] ref uint rxlen
            , [Out] byte[] rxdata
            , [Out] out uint abortcode);

        //************************************************************************
        //
        //    Function      : COP_WriteSDO
        //
        //    Description   : Initiate and execute a SDO download to a node.
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (in) : number of the node
        //                    sdo_no     (in) : number of the sdo:
        //                                      COP_k_DEFAULT_SDO or
        //                                      COP_k_USERDEFINED_SDO
        //                                      Shall the user defined SDO be used, it
        //                                      must already have been created with
        //                                      COP_CreateSDO
        //                    mode       (in) : COP_k_NO_BLOCKTRANSFER
        //                                      COP_k_BLOCKTRANSFER
        //                    idx        (in) : index in OV
        //                    subidx     (in) : subindex in OV
        //                    txlen      (in) : length of data to be transmitted
        //                    txdata     (in) : transmit data (max 2^32 bytes)
        //                    abortcode  (out): abort code of SDO-transfer
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_MEM_ALLOC_ERR:
        //                                      internal data couldn't be created
        //                    BER_k_SDO_THREAD_ERR:
        //                                      Thread execution cancelled
        //                    BER_k_OK        : success
        //                    BER_k_PC_MC_COMM_ERR:
        //                                      Communication error PC to �C
        //                    COP_k_IV        : invalid parameter
        //                    COP_k_NOT_FOUND : node not present in network
        //                    COP_k_TIMEOUT   : SDO response timeout expired
        //                    COP_k_ABORT     : SDO transfer aborted
        //                    COP_k_BSY       : SDO transfer discarded (Transmit
        //                                      queue of the CAN is full)
        //                    COP_k_QUEUE_EMPTY:
        //                                      No SDO response from master
        //                    COP_k_SDO_RUNNING:
        //                                      Thread is still busy, retry later
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_WriteSDO([In] ushort boardhdl
            , [In] byte node_no
            , [In] byte sdo_no
            , [In] byte mode
            , [In] ushort idx
            , [In] byte subidx
            , [In] uint txlen
            , [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 6)] [In]
            byte[] txdata
            , [Out] out uint abortcode);

        //************************************************************************
        //
        //    Function      : COP_PutSDO
        //
        //    Description   : Initiate and execute a SDO-transfer (download or upload)
        //                    with a node
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (in) : number of the node
        //                    sdo_no     (in) : number of the sdo:
        //                                      COP_k_DEFAULT_SDO or
        //                                      COP_k_USERDEFINED_SDO
        //                                      Shall the user defined SDO be used, it
        //                                      must already have been created with
        //                                      COP_CreateSDO
        //                    mode       (in) : COP_k_NO_BLOCKTRANSFER
        //                                      COP_k_BLOCKTRANSFER
        //                    rwAccess   (in) : COP_k_SDO_DOWNLOAD
        //                                      COP_k_SDO_UPLOAD
        //                    idx        (in) : index in OV
        //                    subidx     (in) : subindex in OV
        //                    length     (in) : length of SDO data
        //                    data       (in) : SDO data (max 2^32 bytes)
        //                    h_Event    (in) : event handle
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_OK        : success
        //                    COP_k_IV        : invalid parameter
        //                    COP_k_SDO_RUNNING:
        //                                      Thread is still busy, retry later
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_PutSDO([In] ushort boardhdl
            , [In] byte node_no
            , [In] byte sdo_no
            , [In] byte mode
            , [In] byte rwAccess
            , [In] ushort idx
            , [In] byte subidx
            , [In] uint length
            , [In] byte[] data
            , [In] IntPtr h_Event);

        //************************************************************************
        //
        //    Function      : COP_GetSDO
        //
        //    Description   : Read the data/abort code of a SDO transfer started
        //                    using COP_PutSDO()
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    length  (in/out): size of buffer for received data
        //                                      / number of received data bytes
        //                    data       (out): received data
        //                    abortcode  (out): abort code of SDO-transfer
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_OK        : success
        //                    BER_k_TIMEOUT   : timeout in communication PC to �C
        //                    BER_k_SDO_THREAD_ERR:
        //                                      Thread execution cancelled
        //                    BER_k_DATA_CORRUPT:
        //                                      Corrupt data detected �C to PC,
        //                                      SDO job still pending
        //                    BER_k_PC_MC_COMM_ERR:
        //                                      Communication error PC to �C
        //                    COP_k_IV        : invalid parameter
        //                    COP_k_NOT_FOUND : node not present in network
        //                    COP_k_SDO_RUNNING:SDO transfer currently running, the
        //                                      approximate progress in permille is
        //                                      included in length
        //                    COP_k_TIMEOUT   : SDO response timeout expired
        //                    COP_k_ABORT     : SDO transfer aborted
        //                    COP_k_BSY       : SDO transfer discarded (Transmit
        //                                      queue of the CAN is full)
        //                    COP_k_QUEUE_EMPTY:
        //                                      No SDO response from master
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_GetSDO([In] ushort boardhdl
            , [In] [Out] ref uint length
            , [Out] byte[] data
            , [Out] out uint abortcode);

        //************************************************************************
        //
        //    Function      : COP_CancelSDO
        //
        //    Description   : Cancel a running SDO transfer with a node
        //                    This function only applies when COP_PutSDO() has been
        //                    utilized for SDO access.
        //                    This function will not work with COP_ReadSDO() and
        //                    COP_WriteSDO().
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    node_no    (in) : number of the node
        //                    sdo_no     (in) : number of the sdo:
        //                                      COP_k_DEFAULT_SDO or
        //                                      COP_k_USERDEFINED_SDO
        //                                      Shall the user defined SDO be used, it
        //                                      must already have been created with
        //                                      COP_CreateSDO
        //                    idx        (in) : index in OV
        //                    subidx     (in) : subindex in OV
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    BER_k_OK        : success
        //                    COP_k_IV        : invalid parameter
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_CancelSDO([In] ushort boardhdl
            , [In] byte node_no
            , [In] byte sdo_no
            , [In] ushort idx
            , [In] byte subidx);

        //************************************************************************
        //
        //    Function      : COP_LMT_GetAddress
        //
        //    Description   : Deliver the manufacturer name, the product name and
        //                    the serial number of the connected device.
        //                    Attn: There must be only one device connected
        //                    Parameter 'baudrate' is considered when firmware hasn't
        //                    already been initialised using COP_InitInterface() only.
        //                    The internal protocol sequence is as follows:
        //                      - SwitchModeGlobal()
        //                      - InquireManufacturerName()
        //                      - InquireProductName()
        //                      - InquireSerialNumber()
        //                      - SwitchModeGlobal()
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    baudrate   (in) : baudrate to access the node for
        //                                      configuration (CiA timing table).
        //                                      Applies only if COP_InitInterface()
        //                                      hasn't been called yet.
        //                                      COP_k_10_KB
        //                                      COP_k_20_KB
        //                                      COP_k_50_KB
        //                                      COP_k_100_KB
        //                                      COP_k_125_KB
        //                                      COP_k_250_KB
        //                                      COP_k_500_KB
        //                                      COP_k_1000_KB
        //                    sz_mname   (out): manufacturer name string.
        //                                      7 bytes and terminating '0'.
        //                    sz_pname   (out): product name string.
        //                                      7 bytes and terminating '0'.
        //                    sz_sno     (out): serial number string.
        //                                      14 bytes and terminating '0'.
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_NO        : general error
        //                    COP_k_IV        : invalid parameter
        //                    COP_k_TIMEOUT   : node not present
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll, CharSet = CharSet.Ansi)]
        public static extern short COP_LMT_GetAddress([In] ushort boardhdl
            , [In] byte baudrate
            , [MarshalAs(UnmanagedType.LPArray, SizeConst = 8)] [Out]
            out string sz_mname
            , [MarshalAs(UnmanagedType.LPArray, SizeConst = 8)] [Out]
            out string sz_pname
            , [MarshalAs(UnmanagedType.LPArray, SizeConst = 15)] [Out]
            out string sz_sno);

        //************************************************************************
        //
        //    Function      : COP_LMT_ConfigNode
        //
        //    Description   : Reconfigure a present node in the network
        //                    Parameter 'access_baudrate' is considered when firmware
        //                    hasn't already been initialised using COP_InitInterface()
        //                    only.
        //                    The internal protocol sequence is as follows:
        //                      - SwitchModeSelective(mname, pname, sno)
        //                      - ConfigureModuleID(node_no)
        //                      - ConfigureBitTimingParameters(new_baudrate)
        //                      - StoreConfiguration()
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    sz_mname   (in) : manufacturer name (7 chars)
        //                    sz_pname   (in) : product name (7 chars)
        //                    sz_sno     (in) : serial number (14 chars)
        //                    new_node_no(in) : new node number
        //                    access_baudrate (in) :
        //                                      baudrate to access the node for
        //                                      configuration (CiA timing table).
        //                                      Applies only if COP_InitInterface()
        //                                      hasn't been called yet.
        //                                      (for values see parameter new_baudrate)
        //                    new_baudrate (in) :
        //                                      new baudrate for operation after
        //                                      configuration (CiA timing table).
        //                                      COP_k_10_KB
        //                                      COP_k_20_KB
        //                                      COP_k_50_KB
        //                                      COP_k_100_KB
        //                                      COP_k_125_KB
        //                                      COP_k_250_KB
        //                                      COP_k_500_KB
        //                                      COP_k_800_KB
        //                                      COP_k_1000_KB
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_NO        : general error
        //                    COP_k_IV        : invalid parameter
        //                    COP_k_TIMEOUT   : node not present
        //
        //************************************************************************
        [DllImport(CANopenMasterAPIDll, CharSet = CharSet.Ansi)]
        public static extern short COP_LMT_ConfigNode([In] ushort boardhdl
            , [MarshalAs(UnmanagedType.LPArray, SizeConst = 7)] [In]
            string sz_mname
            , [MarshalAs(UnmanagedType.LPArray, SizeConst = 7)] [In]
            string sz_pname
            , [MarshalAs(UnmanagedType.LPArray, SizeConst = 14)] [In]
            string sz_sno
            , [In] byte new_node_no
            , [In] ushort access_baudrate
            , [In] ushort new_baudrate);

        //*************************************************************************
        //
        //    Function      : COP_LMT_ConfigModuleID
        //
        //    Description   : Configure the NodeID of a present node in the network
        //                    Parameter 'baudrate' is considered when firmware hasn't
        //                    already been initialised using COP_InitInterface() only.
        //                    The internal protocol sequence is as follows:
        //                      - SwitchModeSelective(sz_mname, sz_pname, sz_sno)
        //                      - ConfigureModuleID(new_node_no)
        //                      - StoreConfiguration()
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    baudrate   (in) : baudrate to access the node for
        //                                      configuration (CiA timing table).
        //                                      Applies only if COP_InitInterface()
        //                                      hasn't been called yet.
        //                                      COP_k_10_KB
        //                                      COP_k_20_KB
        //                                      COP_k_50_KB
        //                                      COP_k_100_KB
        //                                      COP_k_125_KB
        //                                      COP_k_250_KB
        //                                      COP_k_500_KB
        //                                      COP_k_800_KB
        //                                      COP_k_1000_KB
        //                    sz_mname   (in) : manufacturer name (7 chars)
        //                    sz_pname   (in) : product name (7 chars)
        //                    sz_sno     (in) : serial number (14 chars)
        //                    new_node_no(in) : new node number
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_NO        : general error
        //                    COP_k_IV        : invalid parameter
        //                    COP_k_TIMEOUT   : node not present
        //
        //*************************************************************************
        [DllImport(CANopenMasterAPIDll, CharSet = CharSet.Ansi)]
        public static extern short COP_LMT_ConfigModuleID([In] ushort boardhdl
            , [In] byte baudtable
            , [MarshalAs(UnmanagedType.LPArray, SizeConst = 7)] [In]
            string sz_mname
            , [MarshalAs(UnmanagedType.LPArray, SizeConst = 7)] [In]
            string sz_pname
            , [MarshalAs(UnmanagedType.LPArray, SizeConst = 14)] [In]
            string sz_sno
            , [In] byte new_node_no);

        //*************************************************************************
        //
        //    Function      : COP_LMT_IdentifyRemoteSlaves
        //
        //    Description   : Search for a Node in a specified range
        //                    Parameter 'baudrate' is considered when firmware hasn't
        //                    already been initialised using COP_InitInterface() only.
        //                    The internal protocol sequence is as follows:
        //                      - IdentifyRemoteSlaves(sz_mname, sz_pname, sz_snolow, sz_snohigh)
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    baudrate   (in) : baudrate to access the node for
        //                                      configuration (CiA timing table).
        //                                      Applies only if COP_InitInterface()
        //                                      hasn't been called yet.
        //                                      COP_k_10_KB
        //                                      COP_k_20_KB
        //                                      COP_k_50_KB
        //                                      COP_k_100_KB
        //                                      COP_k_125_KB
        //                                      COP_k_250_KB
        //                                      COP_k_500_KB
        //                                      COP_k_800_KB
        //                                      COP_k_1000_KB
        //                    sz_mname   (in) : manufacturer name (7 chars)
        //                    sz_pname   (in) : product name (7 chars)
        //                    sz_snolow  (in) : serial number (14 chars), low boundary
        //                    sz_snohigh (in) : serial number (14 chars), high boundary
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success, at least one node found
        //                    COP_k_NO        : general error
        //                    COP_k_IV        : invalid parameter
        //                    COP_k_TIMEOUT   : no node found in specified range
        //
        //*************************************************************************
        [DllImport(CANopenMasterAPIDll, CharSet = CharSet.Ansi)]
        public static extern short COP_LMT_IdentifyRemoteSlaves([In] ushort boardhdl
            , [In] byte baudrate
            , [MarshalAs(UnmanagedType.LPArray, SizeConst = 7)] [In]
            string sz_mname
            , [MarshalAs(UnmanagedType.LPArray, SizeConst = 7)] [In]
            string sz_pname
            , [MarshalAs(UnmanagedType.LPArray, SizeConst = 14)] [In]
            string sz_snolow
            , [MarshalAs(UnmanagedType.LPArray, SizeConst = 14)] [In]
            string sz_snohigh);

        //*************************************************************************
        //
        //    Function      : COP_LSS_InquireAddress
        //
        //    Description   : Deliver the Vendor-ID, the Product-Code, the Revision-
        //                    Number and the Serial-Number of the connected device.
        //                    Parameter 'baudrate' is considered when firmware hasn't
        //                    already been initialised using COP_InitInterface() only.
        //                    The internal protocol sequence is as follows:
        //                      - SwitchModeGlobal()
        //                      - InquireIdentityVendorID()
        //                      - InquireIdentityProductCode()
        //                      - InquireIdentityRevisionNumber()
        //                      - InquireIdentitySerialNumber()
        //                      - SwitchModeGlobal()
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    baudtable  (in) : baudrate table selector to access the
        //                                      node for configuration:
        //                                      COP_k_BAUD_CIA
        //                                      COP_k_BAUD_USER
        //                    baudrate   (in) : baudrate to access the node for
        //                                      configuration.
        //                                      Applies only if COP_InitInterface()
        //                                      hasn't been called yet.
        //                                      COP_k_10_KB
        //                                      COP_k_20_KB
        //                                      COP_k_50_KB
        //                                      COP_k_100_KB
        //                                      COP_k_125_KB
        //                                      COP_k_250_KB
        //                                      COP_k_500_KB
        //                                      COP_k_1000_KB
        //                    VendorId   (out): device Vendor-ID
        //                    ProductCode(out): device Product-Code
        //                    RevisionNo (out): device Revision-Number
        //                    SerialNo   (out): device Serial-Number
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_NO        : general error
        //                    COP_k_TIMEOUT   : device not present or lost
        //                    LSS_k_MEDIA_ACCESS_ERROR:
        //                                      CAN bus access failed
        //                    LSS_k_IV_PARAMETER:
        //                                      invalid parameter
        //                    LSS_k_PROTOCOL_ERR:
        //                                      invalid device response
        //                    LSS_k_BSY       : currently processing a LSS command
        //                                      sequence
        //
        //*************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_LSS_InquireAddress([In] ushort boardhdl
            , [In] byte baudtable
            , [In] byte baudrate
            , [Out] out uint VendorId
            , [Out] out uint ProductCode
            , [Out] out uint RevisionNo
            , [Out] out uint SerialNo);

        //*************************************************************************
        //
        //    Function      : COP_LSS_InquireNodeID
        //
        //    Description   : Deliver the NodeID of a present node in the network.
        //                    Parameter 'baudrate' is considered when firmware hasn't
        //                    already been initialised using COP_InitInterface() only.
        //                    The internal protocol sequence is as follows:
        //                      -* SwitchModeGlobal()
        //                      -* SwitchModeSelective(VendorId, ProductCode, RevisionNo, SerialNo)
        //                      - InquireNodeID()
        //                      - SwitchModeGlobal()
        //                    * in case corresponding mode flag is set
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    baudtable  (in) : baudrate table selector to access the
        //                                      node for configuration:
        //                                      COP_k_BAUD_CIA
        //                                      COP_k_BAUD_USER
        //                    baudrate   (in) : baudrate to access the node for
        //                                      configuration.
        //                                      Applies only if COP_InitInterface()
        //                                      hasn't been called yet.
        //                                      COP_k_10_KB
        //                                      COP_k_20_KB
        //                                      COP_k_50_KB
        //                                      COP_k_100_KB
        //                                      COP_k_125_KB
        //                                      COP_k_250_KB
        //                                      COP_k_500_KB
        //                                      COP_k_800_KB
        //                                      COP_k_1000_KB
        //                    mode       (in) : flags for working mode
        //                                      - LSS_k_SET_MODE_SWITCH_MODE_GLOBAL
        //                    VendorId   (in) : device Vendor-ID
        //                    ProductCode(in) : device Product-Code
        //                    RevisionNo (in) : device Revision-Number
        //                    SerialNo   (in) : device Serial-Number
        //                    node_id    (out): device Node-ID
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_NO        : general error
        //                    COP_k_TIMEOUT   : device not present or lost
        //                    LSS_k_MEDIA_ACCESS_ERROR:
        //                                      CAN bus access failed
        //                    LSS_k_IV_PARAMETER:
        //                                      invalid parameter
        //                    LSS_k_PROTOCOL_ERR:
        //                                      invalid device response
        //                    LSS_k_BSY       : currently processing a LSS command
        //                                      sequence
        //
        //*************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_LSS_InquireNodeID([In] ushort boardhdl
            , [In] byte baudtable
            , [In] byte baudrate
            , [In] byte mode
            , [In] uint VendorId
            , [In] uint ProductCode
            , [In] uint RevisionNo
            , [In] uint SerialNo
            , [Out] out byte node_id);

        //*************************************************************************
        //
        //    Function      : COP_LSS_ConfigNodeID
        //
        //    Description   : Configure the NodeID of a present node in the network.
        //                    Parameter 'baudrate' is considered when firmware hasn't
        //                    already been initialised using COP_InitInterface() only.
        //                    The internal protocol sequence is as follows:
        //                      -* SwitchModeGlobal()
        //                      -* SwitchModeSelective(VendorId, ProductCode, RevisionNo, SerialNo)
        //                      - ConfigureNodeID(new_node_no)
        //                      -* StoreConfiguration()
        //                      - SwitchModeGlobal()
        //                    * in case corresponding mode flag is set
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    baudtable  (in) : baudrate table selector to access the
        //                                      node for configuration:
        //                                      COP_k_BAUD_CIA
        //                                      COP_k_BAUD_USER
        //                    baudrate   (in) : baudrate to access the node for
        //                                      configuration.
        //                                      Applies only if COP_InitInterface()
        //                                      hasn't been called yet.
        //                                      COP_k_10_KB
        //                                      COP_k_20_KB
        //                                      COP_k_50_KB
        //                                      COP_k_100_KB
        //                                      COP_k_125_KB
        //                                      COP_k_250_KB
        //                                      COP_k_500_KB
        //                                      COP_k_800_KB
        //                                      COP_k_1000_KB
        //                    mode       (in) : flags for working mode
        //                                      - LSS_k_SET_MODE_SWITCH_MODE_GLOBAL
        //                                      - LSS_k_SET_MODE_STORE_CONFIGURATION
        //                    VendorId   (in) : device Vendor-ID
        //                    ProductCode(in) : device Product-Code
        //                    RevisionNo (in) : device Revision-Number
        //                    SerialNo   (in) : device Serial-Number
        //                    new_node_no(in) : new node number
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_NO        : general error
        //                    COP_k_TIMEOUT   : device not present or lost
        //                    LSS_k_MEDIA_ACCESS_ERROR:
        //                                      CAN bus access failed
        //                    LSS_k_IV_PARAMETER:
        //                                      invalid parameter
        //                    LSS_k_PROTOCOL_ERR:
        //                                      invalid device response
        //                    LSS_k_BSY       : currently processing a LSS command
        //                                      sequence
        //
        //*************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_LSS_ConfigNodeID([In] ushort boardhdl
            , [In] byte baudtable
            , [In] byte baudrate
            , [In] byte mode
            , [In] uint VendorId
            , [In] uint ProductCode
            , [In] uint RevisionNo
            , [In] uint SerialNo
            , [In] byte new_node_no);

        //*************************************************************************
        //
        //    Function      : COP_LSS_ConfigBitTiming
        //
        //    Description   : Configure the bit timing (baudrate) of a present node
        //                    in the network.
        //                    Parameter 'baudrate' is considered when firmware hasn't
        //                    already been initialised using COP_InitInterface() only.
        //                    The internal protocol sequence is as follows:
        //                      -* SwitchModeGlobal()
        //                      -* SwitchModeSelective(VendorId, ProductCode, RevisionNo, SerialNo)
        //                      - ConfigureBitTimingParameters(new_baudrate)
        //                      -* ActivateBitTimingParameters(switch_delay)
        //                      -* StoreConfiguration()
        //                      - SwitchModeGlobal()
        //                    * in case corresponding mode flag is set
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    baudtable  (in) : baudrate table selector to access the
        //                                      node for configuration:
        //                                      COP_k_BAUD_CIA
        //                                      COP_k_BAUD_USER
        //                    baudrate   (in) : baudrate to access the node for
        //                                      configuration.
        //                                      Applies only if COP_InitInterface()
        //                                      hasn't been called yet.
        //                                      COP_k_10_KB
        //                                      COP_k_20_KB
        //                                      COP_k_50_KB
        //                                      COP_k_100_KB
        //                                      COP_k_125_KB
        //                                      COP_k_250_KB
        //                                      COP_k_500_KB
        //                                      COP_k_800_KB
        //                                      COP_k_1000_KB
        //                    mode       (in) : flags for working mode
        //                                      - LSS_k_SET_MODE_SWITCH_MODE_GLOBAL
        //                                      - LSS_k_SET_MODE_STORE_CONFIGURATION
        //                                      - LSS_k_SET_MODE_ACTIVATE_NEW_BAUDRATE
        //                    VendorId   (in) : device Vendor-ID
        //                    ProductCode(in) : device Product-Code
        //                    RevisionNo (in) : device Revision-Number
        //                    SerialNo   (in) : device Serial-Number
        //                    new_baudtable(in):new baudrate table selector for
        //                                      operation after configuration
        //                    new_baudrate(in): new baudrate for operation after
        //                                      configuration (CiA timing table).
        //                                      COP_k_10_KB
        //                                      COP_k_20_KB
        //                                      COP_k_50_KB
        //                                      COP_k_100_KB
        //                                      COP_k_125_KB
        //                                      COP_k_250_KB
        //                                      COP_k_500_KB
        //                                      COP_k_800_KB
        //                                      COP_k_1000_KB
        //                    switch_delay(in): delay time in ms before transmitting
        //                                      any CAN message at new baudrate after
        //                                      performing the baudrate switch
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_NO        : general error
        //                    COP_k_TIMEOUT   : device not present or lost
        //                    LSS_k_MEDIA_ACCESS_ERROR:
        //                                      CAN bus access failed
        //                    LSS_k_IV_PARAMETER:
        //                                      invalid parameter
        //                    LSS_k_PROTOCOL_ERR:
        //                                      invalid device response
        //                    LSS_k_BSY       : currently processing a LSS command
        //                                      sequence
        //
        //*************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_LSS_ConfigBitTiming([In] ushort boardhdl
            , [In] byte baudtable
            , [In] byte baudrate
            , [In] byte mode
            , [In] uint VendorId
            , [In] uint ProductCode
            , [In] uint RevisionNo
            , [In] uint SerialNo
            , [In] byte new_baudtable
            , [In] byte new_baudrate
            , [In] ushort switch_delay);

        //*************************************************************************
        //
        //    Function      : COP_LSS_ActivateBitTiming
        //
        //    Description   : Activate the bit timing (baudrate) of the network.
        //                    Parameter 'baudrate' is considered when firmware hasn't
        //                    already been initialised using COP_InitInterface() only.
        //                    The internal protocol sequence is as follows:
        //                      - SwitchModeGlobal()
        //                      - ActivateBitTimingParameters(switch_delay)
        //                      - SwitchModeGlobal()
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    baudtable  (in) : baudrate table selector to access the
        //                                      node for configuration:
        //                                      COP_k_BAUD_CIA
        //                                      COP_k_BAUD_USER
        //                    baudrate   (in) : baudrate to access the network for
        //                                      configuration.
        //                                      Applies only if COP_InitInterface()
        //                                      hasn't been called yet.
        //                                      COP_k_10_KB
        //                                      COP_k_20_KB
        //                                      COP_k_50_KB
        //                                      COP_k_100_KB
        //                                      COP_k_125_KB
        //                                      COP_k_250_KB
        //                                      COP_k_500_KB
        //                                      COP_k_800_KB
        //                                      COP_k_1000_KB
        //                    new_baudtable(in):new baudrate table selector for
        //                                      operation after configuration
        //                    new_baudrate(in): new baudrate for operation after
        //                                      configuration (CiA timing table).
        //                                      COP_k_10_KB
        //                                      COP_k_20_KB
        //                                      COP_k_50_KB
        //                                      COP_k_100_KB
        //                                      COP_k_125_KB
        //                                      COP_k_250_KB
        //                                      COP_k_500_KB
        //                                      COP_k_800_KB
        //                                      COP_k_1000_KB
        //                    switch_delay(in): delay time in ms before transmitting
        //                                      any CAN message at new baudrate after
        //                                      performing the baudrate switch
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_NO        : general error
        //                    COP_k_TIMEOUT   : device not present or lost
        //                    LSS_k_MEDIA_ACCESS_ERROR:
        //                                      CAN bus access failed
        //                    LSS_k_IV_PARAMETER:
        //                                      invalid parameter
        //                    LSS_k_PROTOCOL_ERR:
        //                                      invalid device response
        //                    LSS_k_BSY       : currently processing a LSS command
        //                                      sequence
        //
        //*************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_LSS_ActivateBitTiming([In] ushort boardhdl
            , [In] byte baudtable
            , [In] byte baudrate
            , [In] byte new_baudtable
            , [In] byte new_baudrate
            , [In] ushort switch_delay);

        //*************************************************************************
        //
        //    Function      : COP_LSS_IdentifyRemoteSlaves
        //
        //    Description   : Search for a Node in a specified range
        //                    Parameter 'baudrate' is considered when firmware hasn't
        //                    already been initialised using COP_InitInterface() only.
        //                    The internal protocol sequence is as follows:
        //                      - IdentifyRemoteSlaves(sz_mname, sz_pname, sz_snolow, sz_snohigh)
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    baudtable  (in) : baudrate table selector to access the
        //                                      node for configuration:
        //                                      COP_k_BAUD_CIA
        //                                      COP_k_BAUD_USER
        //                    baudrate   (in) : baudrate to access the node for
        //                                      configuration.
        //                                      Applies only if COP_InitInterface()
        //                                      hasn't been called yet.
        //                                      COP_k_10_KB
        //                                      COP_k_20_KB
        //                                      COP_k_50_KB
        //                                      COP_k_100_KB
        //                                      COP_k_125_KB
        //                                      COP_k_250_KB
        //                                      COP_k_500_KB
        //                                      COP_k_800_KB
        //                                      COP_k_1000_KB
        //                    VendorId   (in) : device Vendor-ID
        //                    ProductCode(in) : device Product-Code
        //                    RevisionNoLow  (in):
        //                                      device Revision-Number, lower boundary
        //                    RevisionNoHigh (in):
        //                                      device Revision-Number, higher boundary
        //                    SerialNoLow   (in):
        //                                      device Serial-Number, lower boundary
        //                    SerialNoHigh  (in):
        //                                      device Serial-Number, higher boundary
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_NO        : general error
        //                    COP_k_TIMEOUT   : device not present or lost
        //                    LSS_k_MEDIA_ACCESS_ERROR:
        //                                      CAN bus access failed
        //                    LSS_k_IV_PARAMETER:
        //                                      invalid parameter
        //                    LSS_k_PROTOCOL_ERR:
        //                                      invalid device response
        //                    LSS_k_BSY       : currently processing a LSS command
        //                                      sequence
        //
        //*************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_LSS_IdentifyRemoteSlaves([In] ushort boardhdl
            , [In] byte baudtable
            , [In] byte baudrate
            , [In] uint VendorId
            , [In] uint ProductCode
            , [In] uint RevisionNoLow
            , [In] uint RevisionNoHigh
            , [In] uint SerialNoLow
            , [In] uint SerialNoHigh);

        //*************************************************************************
        //
        //    Function      : COP_LSS_IdentifyNonConfRemoteSlaves
        //
        //    Description   : Search for any present node in the network whose Node-ID
        //                    is not configured.
        //                    Parameter 'baudrate' is considered when firmware hasn't
        //                    already been initialised using COP_InitInterface() only.
        //                    The internal protocol sequence is as follows:
        //                      - IdentifyNonConfiguredRemoteSlaves()
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    baudtable  (in) : baudrate table selector to access the
        //                                      node for configuration:
        //                                      COP_k_BAUD_CIA
        //                                      COP_k_BAUD_USER
        //                    baudrate   (in) : baudrate to access the node for
        //                                      configuration.
        //                                      Applies only if COP_InitInterface()
        //                                      hasn't been called yet.
        //                                      COP_k_10_KB
        //                                      COP_k_20_KB
        //                                      COP_k_50_KB
        //                                      COP_k_100_KB
        //                                      COP_k_125_KB
        //                                      COP_k_250_KB
        //                                      COP_k_500_KB
        //                                      COP_k_800_KB
        //                                      COP_k_1000_KB
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_NO        : general error
        //                    COP_k_TIMEOUT   : device not present or lost
        //                    LSS_k_MEDIA_ACCESS_ERROR:
        //                                      CAN bus access failed
        //                    LSS_k_IV_PARAMETER:
        //                                      invalid parameter
        //                    LSS_k_PROTOCOL_ERR:
        //                                      invalid device response
        //                    LSS_k_BSY       : currently processing a LSS command
        //                                      sequence
        //
        //*************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_LSS_IdentifyNonConfRemoteSlaves([In] ushort boardhdl
            , [In] byte baudtable
            , [In] byte baudrate);

        //*************************************************************************
        //
        //    Function      : COP_SetLSSTimeOut
        //
        //    Description   : Change the LSS/LMT timeout value. Value in milliseconds.
        //                    Default value is 100 ms
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    w_timeout  (in) : new timeout in ms
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_IV        : invalid parameter
        //
        //*************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_SetLSSTimeOut([In] ushort boardhdl
            , [In] ushort w_timeout);

        //*************************************************************************
        //
        //    Function      : COP_LSS_Fastscan
        //
        //    Description   : Search non-configured LSS slaves
        //                    Parameter 'baudrate' is considered when firmware hasn't
        //                    already been initialised using COP_InitInterface() only.
        //                    This function provides a means to find the first non-
        //                    configured slave and returns its identity attributes.
        //                    By those, the slave might be configured using the other
        //                    LSS commands, and then Fastscan could be repeated until
        //                    no further unconfigured slave is found.
        //                    Another use case is find a non-configured device by a
        //                    (partially) given LSS numbers. For this, the partial
        //                    LSS numbers shall be given as input arguments, together
        //                    with the lowest bit to match of a LSS number. Thus,
        //                    comparison is performed for the count of high-order
        //                    bits that range just from MSB down to the individual
        //                    Bit number given:
        //                      *VendorId    = 0xA0000000 (10100000.00000000.00000....)
        //                      VendorIdBits = 29  (32-29 = 3;  i.e. 3 high-order bits)
        //                    In this example, any device whose Vendor-ID begins with
        //                    binary bit pattern 101xxx (i.e. 0xAxx, 0xBxx) will be found.
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    baudtable  (in) : baudrate table selector to access the
        //                                      node for configuration:
        //                                      COP_k_BAUD_CIA
        //                                      COP_k_BAUD_USER
        //                    baudrate   (in) : baudrate to access the node for
        //                                      configuration.
        //                                      Applies only if COP_InitInterface()
        //                                      hasn't been called yet.
        //                                      COP_k_10_KB
        //                                      COP_k_20_KB
        //                                      COP_k_50_KB
        //                                      COP_k_100_KB
        //                                      COP_k_125_KB
        //                                      COP_k_250_KB
        //                                      COP_k_500_KB
        //                                      COP_k_800_KB
        //                                      COP_k_1000_KB
        //                    VendorId   (in) : device Vendor-ID
        //                                      Typically, this value should be set to
        //                                      0 by the caller for a full range scan.
        //                                      Alternatively, it can be set to a bit
        //                                      pattern to match, i.e. to find a
        //                                      particular device or a group of devices
        //                               (out): Exact (first) found Vendor-ID
        //                    VendorIdBits(in): bits to be checked of given VendorId
        //                                      0 <= VendorIdBits <= 31
        //                                      Typically, this value should be set
        //                                      to 31, not to 0, by the caller for a
        //                                      full range scan.
        //                                      Any value below 31 determines the
        //                                      lowest bit position of a range starting
        //                                      at MSB to be checked for a bit pattern
        //                                      match: VendorIdBits up to 31 to be
        //                                      compared.
        //                    ProductCode (in): device Product-Code
        //                                      Typically, this value should be set to
        //                                      0 by the caller for a full range scan.
        //                                      Alternatively, it can be set to a bit
        //                                      pattern to match, i.e. to find a
        //                                      particular device or a group of devices.
        //                                      To skip scanning of the Product-Code,
        //                                      the Revision-Number and the Serial-
        //                                      Number, the argument can also be
        //                                      omitted (NULL)
        //                               (out): Exact (first) found Product-Code within
        //                                      given range
        //                    ProductCodeBits (in):
        //                                      bits to be checked of given ProductCode
        //                                      0 <= VendorIdBits <= 31
        //                                      Typically, this value should be set
        //                                      to 31, not to 0, by the caller for a
        //                                      full range scan.
        //                                      Any value below 31 determines the
        //                                      lowest bit position of a range starting
        //                                      at MSB to be checked for a bit pattern
        //                                      match: ProductCodeBits up to 31 to be
        //                                      compared.
        //                    RevisionNo (in) : device Revision-Number
        //                                      Typically, this value should be set to
        //                                      0 by the caller for a full range scan.
        //                                      Alternatively, it can be set to a bit
        //                                      pattern to match, i.e. to find a
        //                                      particular device or a group of devices.
        //                                      To skip scanning of the Revision-Number
        //                                      and the Serial-Number, the argument can
        //                                      also be omitted (NULL)
        //                               (out): Exact (first) found Revision-Number
        //                                      within given range
        //                    RevisionNoBits (in):
        //                                      bits to be checked of given RevisionNo
        //                                      0 <= RevisionNoBits <= 31
        //                                      Typically, this value should be set
        //                                      to 31, not to 0, by the caller for a
        //                                      full range scan.
        //                                      Any value below 31 determines the
        //                                      lowest bit position of a range starting
        //                                      at MSB to be checked for a bit pattern
        //                                      match: RevisionNoBits up to 31 to be
        //                                      compared
        //                    SerialNo   (in) : device Serial-Number
        //                                      Typically, this value should be set to
        //                                      0 by the caller for a full range scan.
        //                                      Alternatively, it can be set to a bit
        //                                      pattern to match, i.e. to find a
        //                                      particular device or a group of devices.
        //                                      To skip scanning of the Serial-Number,
        //                                      the argument can also be omitted (NULL)
        //                               (out): Exact (first) found Serial-Number
        //                                      within given range
        //                    SerialNoBits(in): bits to be checked of given SerialNo
        //                                      0 <= SerialNoBits <= 31
        //                                      Typically, this value should be set
        //                                      to 31, not to 0, by the caller for a
        //                                      full range scan.
        //                                      Any value below 31 determines the
        //                                      lowest bit position of a range starting
        //                                      at MSB to be checked for a bit pattern
        //                                      match: SerialNoBits up to 31 to be
        //                                      compared.
        //
        //    Returnvalues  : COP_k_OK        : Non-configured slave found
        //                    BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_NO        : general error
        //                    LSS_k_MEDIA_ACCESS_ERROR:
        //                                      CAN bus access failed
        //                    LSS_k_IV_PARAMETER:
        //                                      invalid parameter
        //                    LSS_k_PROTOCOL_ERR:
        //                                      invalid device response
        //                    LSS_k_BSY       : currently processing a LSS command
        //                                      sequence
        //                    LSS_k_FS_NO_NONCONFIGURED_SLAVE:
        //                                      No (non-configured) slave found
        //                    LSS_k_FS_NF_NONCONFIGURED_SLAVE:
        //                                      No slave found within given range
        //
        //*************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_LSS_Fastscan([In] ushort boardhdl
            , [In] byte baudtable
            , [In] byte baudrate
            , [In] [Out] ref uint VendorId
            , [In] byte VendorIdBits
            , [In] [Out] ref uint ProductCode
            , [In] byte ProductCodeBits
            , [In] [Out] ref uint RevisionNo
            , [In] byte RevisionNoBits
            , [In] [Out] ref uint SerialNo
            , [In] byte SerialNoBits);

        //*************************************************************************
        //
        //    Function      : COP_ConfigFlyMaster
        //
        //    Description   : Initial parameterisation of Flying Master
        //                    Flying Master kernel will not work if this function
        //                    has not been called.
        //                    Do not call this function more than once.
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    wDetectionTimeout (in):
        //                                      Contents of [1F90sub1]
        //                    wNegotiationDelay (in):
        //                                      Contents of [1F90sub2]
        //                    wPriorityLevel (in):
        //                                      Contents of [1F90sub3]
        //                    wPriorityTimeslot (in):
        //                                      Contents of [1F90sub4]
        //                    wNodeTimeslot (in):
        //                                      Contents of [1F90sub5]
        //                    wCycletimeCd (in):
        //                                      Contents of [1F90sub6]
        //                    wCycletimeTimeoutHbeat(in):
        //                                      Timeout for heartbeat monitoring
        //                                      when API is slave and other node
        //                                      is active master
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_IV        : invalid argument
        //                    COP_k_UNKNOWN   : Firmware does not support Flying Master
        //                                      or Function already successfully called
        //                    COP_k_NO_FLY_MASTER_PRESENT:
        //                                      Flying Master not activated
        //
        //*************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_ConfigFlyMaster([In] ushort boardhdl
            , [In] ushort wDetectionTimeout
            , [In] ushort wNegotiationDelay
            , [In] ushort wPriorityLevel
            , [In] ushort wPriorityTimeslot
            , [In] ushort wNodeTimeslot
            , [In] ushort wCycletimeCd
            , [In] ushort wCycletimeTimeoutHbeat);

        //*************************************************************************
        //
        //    Function      : COP_StartFlyMaster
        //
        //    Description   : Start configured Flying Master
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_UNKNOWN   : Function already successfully called
        //                    COP_k_NO_FLY_MASTER_PRESENT:
        //                                      Flying Master not activated
        //
        //*************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_StartFlyMaster([In] ushort boardhdl);

        //*************************************************************************
        //
        //    Function      : COP_GetStatusFlyMasterNeg
        //
        //    Description   : Return status of Flying Master negotiation
        //
        //    Parameter     : boardhdl   (in) : handle of CAN board
        //                    status     (out): Status of negotiation
        //                    masterid   (out): Node id of master
        //                    masterprio (out): Priority of master
        //
        //    Returnvalues  : BER_k_ERR       : boardhandle not valid
        //                    BER_k_NOT_SENT  : command msg couldn't be handed over
        //                    BER_k_TIMEOUT   : no confirmation received
        //                    COP_k_OK        : success
        //                    COP_k_IV        : invalid argument
        //                    COP_k_UNKNOWN   : Firmware does not support Flying Master
        //                    COP_k_NO_FLY_MASTER_PRESENT:
        //                                      Flying Master not activated
        //
        //*************************************************************************
        [DllImport(CANopenMasterAPIDll)]
        public static extern short COP_GetStatusFlyMasterNeg([In] ushort boardhdl
            , [Out] out byte status
            , [Out] out byte masterid
            , [Out] out byte masterprio);

        #endregion

        #region Errorcode Description Methods

        //************************************************************************
        //
        //    Function      : CopAbortCodeString
        //
        //    Description   : Return description of given CANopen Abort code
        //
        //    Parameter     : copAbortCode : CANopen Abort code according to
        //                                   DS-401 page 9-26f
        //
        //    Returnvalues  : CANopen Abort code description
        //
        //************************************************************************
        public static string CopAbortCodeString(uint copAbortCode)
        {
            string description;

            switch (copAbortCode)
            {
                case 0x00000000:
                    description = "No Abort, SDO transfer successful";
                    break;

                case 0x05030000:
                    description = "Toggle bit not alternated.";
                    break;
                case 0x05040000:
                    description = "SDO protocol timed out.";
                    break;
                case 0x05040001:
                    description = "Client/server command specifier not valid or unknown.";
                    break;
                case 0x05040002:
                    description = "Invalid block size (block mode only).";
                    break;
                case 0x05040003:
                    description = "Invalid sequence number (block mode only).";
                    break;
                case 0x05040004:
                    description = "CRC error (block mode only).";
                    break;
                case 0x05040005:
                    description = "Out of memory.";
                    break;

                case 0x06010000:
                    description = "Unsupported access to an object.";
                    break;
                case 0x06010001:
                    description = "Attempt to read a write only object.";
                    break;
                case 0x06010002:
                    description = "Attempt to write a read only object.";
                    break;
                case 0x06020000:
                    description = "Object does not exist in the object dictionary.";
                    break;
                case 0x06040041:
                    description = "Object cannot be mapped to the PDO.";
                    break;
                case 0x06040042:
                    description = "The number and length of the objects to be mapped would exceed PDO length.";
                    break;
                case 0x06040043:
                    description = "General parameter incompatibility reason.";
                    break;
                case 0x06040047:
                    description = "General internal incompatibility in the device.";
                    break;
                case 0x06060000:
                    description = "Access failed due to an hardware error.";
                    break;
                case 0x06070010:
                    description = "Data type does not match, length of service parameter does not match";
                    break;
                case 0x06070012:
                    description = "Data type does not match, length of service parameter too high";
                    break;
                case 0x06070013:
                    description = "Data type does not match, length of service parameter too low";
                    break;
                case 0x06090011:
                    description = "Sub-index does not exist.";
                    break;
                case 0x06090030:
                    description = "Value range of parameter exceeded (only for write access).";
                    break;
                case 0x06090031:
                    description = "Value of parameter written too high.";
                    break;
                case 0x06090032:
                    description = "Value of parameter written too low.";
                    break;
                case 0x06090036:
                    description = "Maximum value is less than minimum value.";
                    break;
                case 0x060A0023:
                    description = "Resource not available: SDO connection.";
                    break;

                case 0x08000000:
                    description = "general error";
                    break;
                case 0x08000020:
                    description = "Data cannot be transferred or stored to the application.";
                    break;
                case 0x08000021:
                    description = "Data cannot be transferred or stored to the application because of local control.";
                    break;
                case 0x08000022:
                    description =
                        "Data cannot be transferred or stored to the application because of the present device state.";
                    break;
                case 0x08000023:
                    description =
                        "Object dictionary dynamic generation fails or no object dictionary is present (e.g. object dictionary is generated from file and generation fails because of an file error).";
                    break;
                case 0x08000024:
                    description = "No data available.";
                    break;

                default:
                    description = "unknown AbortCode";
                    break;
            }

            return description;
        }

        //************************************************************************
        //
        //    Function      : CopErrorString
        //
        //    Description   : Return description of given COP error code
        //
        //    Parameter     : copErrorCode : Returnvalue of any CANopen Master
        //                                   API function
        //
        //    Returnvalues  : COP error code description
        //
        //************************************************************************
        public static string CopErrorString(int copErrorCode)
        {
            string description;

            switch (copErrorCode)
            {
                case BER_k_OK:
                    description = "success";
                    break;
                case BER_k_ERR:
                    description = "general error";
                    break;
                case BER_k_DATA_CORRUPT:
                    description = "corrupt data detected �C to PC";
                    break;
                case BER_k_NOT_SENT:
                    description = "msg not sent, try again";
                    break;
                //case  BER_k_NO_NEW_MSG            : description = "no new msg (queue empty)"; break;
                case BER_k_TIMEOUT:
                    description = "timeout in communication PC to �C";
                    break;
                case BER_k_BOARD_ALREADY_USED:
                    description = "board is used by another instance";
                    break;
                case BER_k_ALL_BOARDS_USED:
                    description = "no free board slots inside DLL";
                    break;
                case BER_k_BOARD_NOT_SUPP:
                    description = "the given board is not supported by CANopen Master API";
                    break;
                case BER_k_BOARD_NOT_FOUND:
                    description = "the board wasn't found";
                    break;
                case BER_k_CANNOT_SEARCH_BOARD:
                    description = "Hardware selection Dialog cancelled by user";
                    break;
                case BER_k_WRONG_FW:
                    description = "wrong firmware version";
                    break;
                case BER_k_USED_FROM_OTHER_PROCESS:
                    description = "board is used by another application";
                    break;
                case BER_k_PC_MC_COMM_ERR:
                    description = "communication error PC to �C";
                    break;
                case BER_k_BOARD_DLD_ERR:
                    description = "an error occured while firmware download";
                    break;
                case BER_k_BADCALLBACK_PTR:
                    description = "a callbackpointer is invalid";
                    break;
                case BER_k_NO_SUCH_CANLINE:
                    description = "given CANline is not available or not supported";
                    break;
                case BER_k_CANLINE_USED:
                    description = "CANline is already in use";
                    break;
                case BER_k_VCI_INST_ERR:
                    description = "IXXAT VCI driver missing";
                    break;
                case BER_k_BOARD_ERR:
                    description = "unknown board type or can't locate board type";
                    break;
                case BER_k_MEM_ALLOC_ERR:
                    description = "memory allocation error (internal)\ndata or OS element couldn't be created";
                    break;
                case BER_k_CCI_INST_ERR:
                    description = "CCI installation error (internal)";
                    break;
                case BER_k_SDO_INST_ERR:
                    description = "SDO handler installation error (internal)";
                    break;
                case BER_k_SDO_THREAD_ERR:
                    description = "SDO thread execution cancelled\nwhile waiting for SDO response from master";
                    break;

                case COP_k_CAL_ERR:
                    description = "failure in CAL";
                    break;
                case COP_k_IV:
                    description = "invalid parameter or service not allowed";
                    break;
                case COP_k_ABORT:
                    description = "transfer aborted";
                    break;
                case COP_k_NOT_FOUND:
                    description = "node not found";
                    break;
                case COP_k_NOT_INIT:
                    description = "CANopen-Master not initialised";
                    break;
                case COP_k_INIT:
                    description = "CANopen-Master already initialised";
                    break;
                case COP_k_QUEUE_EMPTY:
                    description = "no objects in queue";
                    break;
                case COP_k_TIMEOUT:
                    description = "timeout in CAN communication";
                    break;
                case COP_k_SDO_RUNNING:
                    description = "SDO transfer in progress, retry later";
                    break;
                case COP_k_BSY:
                    description = "generic process still running";
                    break;
                case COP_k_NO_OBJECT:
                    description = "object does not exist";
                    break;
                case COP_k_NO_SUBINDEX:
                    description = "subindex does not exist";
                    break;
                case COP_k_WRITE_ONLY:
                    description = "object is write only";
                    break;
                case COP_k_PRESENT_DEVICE_STATE:
                    description = "access currently not possible";
                    break;
                case COP_k_RANGE_EXCEEDED:
                    description = "parameter out of range";
                    break;
                case COP_k_UNKNOWN:
                    description = "unknown command";
                    break;
                case COP_k_NO_FLY_MASTER_PRESENT:
                    description = "API/hardware version does not support flying master";
                    break;
                case COP_k_NO_LOWSPEED:
                    description = "No LowSpeed bus-coupling present or supported";
                    break;
                default:
                    description = "unknown ErrorCode";
                    break;
            }

            return description;
        }

        //************************************************************************
        //
        //    Function      : CopEventTypeString
        //
        //    Description   : Return description of given COP status event type
        //
        //    Parameter     : copEventType : Returnvalue of any CANopen Master API
        //                                   status event (COP_k_aaa_EVT)
        //
        //    Returnvalues  : COP status event type description
        //
        //************************************************************************
        public static string CopEventTypeString(byte copEventType)
        {
            string description;

            switch (copEventType)
            {
                case COP_k_NMT_EVT:
                    description = "NMT event";
                    break;
                case COP_k_DLL_EVT:
                    description = "API/DLL event";
                    break;
                case COP_k_WPDO_EVT:
                    description = "WritePDO event";
                    break;
                case COP_k_RPDO_EVT:
                    description = "ReadPDO event";
                    break;
                case COP_k_QUEUE_OVRUN_EVT:
                    description = "Queue Overrun event";
                    break;
                case COP_k_FLY_EVT:
                    description = "Flying Master event";
                    break;
                default:
                    description = "Unknown EventType";
                    break;
            }

            return description;
        }

        #endregion
    }
}
/*
     //************************************************************************
    // Errortypes for COP_GetEvent
    //************************************************************************
    public const Byte COP_k_NMT_EVT                 = 1;
    public const Byte COP_k_DLL_EVT                 = 2;
    public const Byte COP_k_WPDO_EVT                = 3;
    public const Byte COP_k_RPDO_EVT                = 4;
    public const Byte COP_k_QUEUE_OVRUN_EVT         = 5;
    public const Byte COP_k_FLY_EVT                 = 6;

    //************************************************************************
    //  Errorcodes for COP_GetEvent (E) of type COP_k_DLL_EVT
    //************************************************************************
    public const Byte COP_k_DLL_NOERR               =  0;   // no error
    public const Byte COP_k_DLL_RXOVR               =  1;   // software overrun (rx-queue)
    public const Byte COP_k_DLL_COVR                =  2;   // CAN: overrun
    public const Byte COP_k_DLL_BOFF                =  4;   // CAN: bus off
    public const Byte COP_k_DLL_ESET                =  8;   // CAN: error-status-bit set
    public const Byte COP_k_DLL_ERESET              = 16;   // CAN: error-status-bit reset
    public const Byte COP_k_DLL_TXOVR               = 32;   // tx-queue full

    //************************************************************************
    //  Errorcodes for COP_GetEvent (E) of type COP_k_NMT_EVT
    //************************************************************************
    public const Byte COP_k_NMT_GUARDERR            = 1;
    public const Byte COP_k_NMT_BOOTIND             = 2;
    public const Byte COP_k_NMT_HEARTBEATERR        = 3;

    //************************************************************************
    // Errorcodes for COP_GetEvent (E) of type COP_k_FLY_EVT
    // Returncodes for Flying Master status (F) in COP_GetStatusFlyMasterNeg()
    //************************************************************************
    public const Byte COP_k_FLY_MASTER              = 4;    //  E,F received mastership
    public const Byte COP_k_FLY_NOT_MASTER          = 5;    //  E,F lost master negotiation
    public const Byte COP_k_FLY_LOST_MASTERSHIP     = 6;    //  E   high prior node kicked master
    public const Byte COP_k_FLY_LOST_ACTIVE_MASTER  = 7;    //  E   lost active master
    public const Byte COP_k_FLY_UNKNOWN             = 8;    //  E   unknown event
    public const Byte COP_k_FLY_WAIT_BUSCONNECTION  = 9;    //  F   waiting for busconnection
    public const Byte COP_k_FLY_NEGOTIATION_RUNNING = 10;   //  F   negotiation in progress

 */