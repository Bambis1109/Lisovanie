using static IXXAT.CANopenMasterAPI6;
using System.Threading.Channels;
using IXXAT;

namespace EposCmd.Net
{
    public class CDeviceManagerCO : ErrorHandlingCO
    {
        public ushort _keyHandle;
        private readonly object SyncLock = new object();
        private bool SyncEvent;
        public string Name { get; set; }
        private COP_t_EventCallback _mEmcyCallback;
        private COP_t_EventCallback _mPdoCallback;
        private COP_t_EventCallback _mStatusCallback;
        private COP_t_EventCallback _mSyncCallback;
        public Guid BoardId;
        public Guid BoardType;
        public int CanLine;
        private readonly Dictionary<byte, CDeviceCO> DeviceList = new Dictionary<byte, CDeviceCO>();
        public COP_t_EMERGENCY_OBJ LastEmergencyMsg;
        public COP_t_EVENT_OBJ LastEventMsg;
        public COP_BOARD_INFO sp_info;
        public int BoardLine;

        public byte BaudIndex { get; set; }
        public uint Timeout { get; set; }
        public event EventHandler ReceiveEmergency;
        public event EventHandler ReceiveStatus;

        private Channel<CanMessage> _messageChannel;
        private CancellationTokenSource _cts;
        private Task _readerTask;

        public string GetConnectionInfo
        {
            get =>
                $"BoartLine:{BoardLine}, CanLine:{CanLine}, SN:{sp_info.serial_num}  FW:{sp_info.fw_version}  HW:{sp_info.hw_version} ";
        }

        public CDeviceManagerCO()
        {
            DeviceList = new Dictionary<byte, CDeviceCO>();

            _messageChannel = Channel.CreateUnbounded<CanMessage>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
            _cts = new CancellationTokenSource();
            _readerTask = Task.Factory.StartNew(ProcessMessagesAsync, _cts.Token, TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public CDeviceManagerCO(string deviceName, string protocolStackName, string interfaceName,
            string portName) : this()
        {
            Name = deviceName;
        }

        private async Task ProcessMessagesAsync()
        {
            try
            {
                while (await _messageChannel.Reader.WaitToReadAsync(_cts.Token))
                {
                    while (_messageChannel.Reader.TryRead(out var msg))
                    {
                        switch (msg.Type)
                        {
                            case ECanMessageType.Pdo:
                                if (DeviceList.TryGetValue(msg.Pdo.node_no, out var devicePdo))
                                {
                                    devicePdo.ReadPdo(msg.Pdo);
                                }

                                break;

                            case ECanMessageType.Emergency:
                                LastEmergencyMsg = msg.Emergency;
                                if (DeviceList.TryGetValue(msg.Emergency.node_no, out var deviceEmcy))
                                {
                                    deviceEmcy.ReadEmergency(msg.Emergency);
                                }

                                RecEmergency(EventArgs.Empty);
                                break;

                            case ECanMessageType.Event:
                                LastEventMsg = msg.Event;
                                switch (msg.Event.evt_type)
                                {
                                    case COP_k_NMT_EVT:
                                    case COP_k_RPDO_EVT:
                                    case COP_k_WPDO_EVT:
                                        if (DeviceList.TryGetValue(msg.Event.evt_data2, out var deviceEvent))
                                        {
                                            deviceEvent.ReadStatus(msg.Event);
                                        }

                                        break;
                                    case COP_k_DLL_EVT:
                                    case COP_k_QUEUE_OVRUN_EVT:
                                    case COP_k_FLY_EVT:
                                        break;
                                }

                                RecStatus(EventArgs.Empty);
                                break;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Vlákno bolo korektne ukončené
            }
        }

        public void Init()
        {
            short result = 0;
            //    result = COP_InitBoard(out _keyHandle, ref BoardType, ref BoardId, CanLine | (int)COP_VCI3GENERIC);   //Povolenie Cam medzi aolikaciami
            result = COP_InitBoard(out _keyHandle, ref BoardType, ref BoardId, CanLine);


            if (COP_k_OK != result) throw new CDeviceException("Error init DLL", (uint)result);
            _mPdoCallback = PDOCallback;
            _mEmcyCallback = EmcyCallback;
            _mStatusCallback = StatusCallback;
            _mSyncCallback = SyncCallback;
            result = COP_DefineCallbacks(_keyHandle, _mPdoCallback, _mEmcyCallback, _mStatusCallback, _mSyncCallback);
            var r = COP_GetBoardInfo(_keyHandle, out sp_info);
            if (COP_k_OK != result)
            {
                COP_ReleaseBoard(_keyHandle);
                _keyHandle = 0;
                throw new CDeviceException("Error RegisterCallbacks", (uint)result);
            }

            result = COP_InitInterface(_keyHandle, COP_k_BAUD_CIA, BaudIndex, 127, 100, COP_k_NO_FEATURES);
            if (COP_k_OK != result)
            {
                COP_ReleaseBoard(_keyHandle);
                _keyHandle = 0;
                throw new CDeviceException("Error init CAN interface", (uint)result);
            }
        }

// WORKAROUND: Ixxat API bug - must add node with 0 time first, then change to Heartbeat
        public void AddDevice(CDeviceCO device)
        {
            if (DeviceList.ContainsKey(device.NodeId))
                throw new CDeviceException("Error add node: " + device.NodeId + " Node already exist", 0);
            var result = 0;
            result = COP_AddNode((ushort)_keyHandle, (byte)device.NodeId, (byte)COP_k_NODE_GUARDING, (ushort)0,
                (byte)0);

            if (COP_k_OK != result)
            {
                COP_ReleaseBoard(_keyHandle);
                _keyHandle = 0;
                throw new CDeviceException("Error add node:" + device.NodeId, (uint)result);
            }

            result = COP_ChangeNodeParameter(_keyHandle, device.NodeId, (byte)COP_k_HEARTBEAT, (ushort)150, 2);
            if (COP_k_OK != result)
            {
                COP_ReleaseBoard(_keyHandle);
                _keyHandle = 0;
                throw new CDeviceException("Error add node:" + device.NodeId, (uint)result);
            }

            DeviceList.Add(device.NodeId, device);
        }


        protected void RecEmergency(EventArgs e)
        {
            ReceiveEmergency?.Invoke(this, e);
        }

        protected void RecStatus(EventArgs e)
        {
            ReceiveStatus?.Invoke(this, e);
        }

        public void SendNmtServiceForAllNodes(ECommandSpecifier commandSpecifier)
        {
            short res = 0;
            switch (commandSpecifier)
            {
                case ECommandSpecifier.NcsStartRemoteNode:
                {
                    res = COP_StartNode(_keyHandle, 0);
                }
                    break;
                case ECommandSpecifier.NcsStopRemoteNode:
                {
                    res = COP_StopNode(_keyHandle, 0);
                }
                    break;
                case ECommandSpecifier.NcsEnterPreOperational:
                {
                    res = COP_EnterPreOperational(_keyHandle, 0);
                }
                    break;
                case ECommandSpecifier.NcsResetNode:
                {
                    res = COP_ResetNode(_keyHandle, 0);
                }
                    break;
                case ECommandSpecifier.NcsResetCommunication:
                {
                    res = COP_ResetComm(_keyHandle, 0);
                }
                    break;
                default:
                    break;
            }

            if (COP_k_OK != res)
            {
                var Message = commandSpecifier + " - Remote Nodes : Abort " + CopErrorString(res);
                throw new CDeviceException(Message, (uint)res);
            }
        }

        public void Sync(ESync sync)
        {
            short res = 0;
            switch (sync)
            {
                case ESync.NcsEnable:
                    res = COP_DefSyncObj(_keyHandle, 20, 20, 0);
                    res = COP_EnableSync(_keyHandle, COP_k_SINGLE_LINE);

                    break;
                case ESync.NcsDisable:
                    res = COP_DisableSync(_keyHandle, COP_k_SINGLE_LINE);
                    break;
                case ESync.NcsOneSync:

                    lock (IL.LockSync)
                    {
                        if (!WaitForSync(500))
                        {
                            Thread.Sleep(5);
                            if (!WaitForSync(500))
                            {
                                throw new CDeviceException($"WaitForSync 2 opakovanie");
                            }
                        }
                    }
                    //  Thread.Sleep(20);

                    res = COP_DisableSync(_keyHandle, COP_k_SINGLE_LINE);
                    break;
                default:
                    break;
            }

            if (COP_k_OK != res)
            {
                throw new CDeviceException(sync + " - Remote Nodes : Abort " + CopErrorString(res), (uint)res);
            }
        }

        public bool WaitForSync(int timeout)
        {
            int timeoutLoc = timeout;
            lock (SyncLock)
            {
                SyncEvent = true;
            }

            short res = COP_EnableSync(_keyHandle, COP_k_SINGLE_LINE);
            if (COP_k_OK != res)
            {
                throw new CDeviceException($"Wait for sync - COP_EnableSync {CopErrorString(res)}");
            }

            Thread.Sleep(3);
            bool wait = false;
            do
            {
                lock (SyncLock)
                {
                    wait = SyncEvent;
                }

                Thread.Sleep(1);
                if (timeoutLoc <= 0) break;
                timeoutLoc = timeoutLoc - 1;
            } while (wait);

            Thread.Sleep(10);
            res = COP_DisableSync(_keyHandle, COP_k_SINGLE_LINE);
            Thread.Sleep(10);
            if (COP_k_OK != res)
            {
                return false;
                throw new CDeviceException($"Wait for sync - COP_DisableSync {CopErrorString(res)}");
            }

            if (timeoutLoc <= 0)
            {
                return false;
                throw new CDeviceException($"WaitForSync  Timeout:{timeout}", 0);
            }

            return true;
        }

        protected override void Dispose(bool isDisposeByUser)
        {
            if (isDisposeByUser)
            {
                _cts?.Cancel();
                _readerTask?.Wait(500);
                _cts?.Dispose();
            }
        }

        #region CANopen Master API Callbacks

        public void PDOCallback(ushort aBoardhdl, byte aQueID, byte aCANline)
        {
            short res;
            COP_t_RX_PDO sp_pdo;
            do
            {
                res = COP_ReadPDO_S(_keyHandle, out sp_pdo);
                if (res == COP_k_OK)
                {
                    var msg = new CanMessage { Type = ECanMessageType.Pdo, Pdo = sp_pdo };
                    _messageChannel.Writer.TryWrite(msg);
                }
            } while (COP_k_QUEUE_EMPTY != res);
        }

        public void EmcyCallback(ushort aBoardhdl, byte aQueID, byte aCANline)
        {
            short res;
            COP_t_EMERGENCY_OBJ emcy;
            do
            {
                res = COP_GetEmergencyObj_S(_keyHandle, out emcy);
                if (res == COP_k_OK)
                {
                    var msg = new CanMessage { Type = ECanMessageType.Emergency, Emergency = emcy };
                    _messageChannel.Writer.TryWrite(msg);
                }
            } while (COP_k_QUEUE_EMPTY != res);
        }

        public void StatusCallback(ushort aBoardhdl, byte aQueID, byte aCANline)
        {
            short res;
            COP_t_EVENT_OBJ evt;
            do
            {
                res = COP_GetEvent(_keyHandle, out evt.evt_type, out evt.evt_data1, out evt.evt_data2,
                    out evt.evt_data3);
                evt.evt_data4 = 0; // Inicializácia štruktúry

                if (res == COP_k_OK)
                {
                    var msg = new CanMessage { Type = ECanMessageType.Event, Event = evt };
                    _messageChannel.Writer.TryWrite(msg);
                }
            } while (COP_k_QUEUE_EMPTY != res);
        }

        private void SyncCallback(ushort boardhdl, byte que_num, byte canline)
        {
            short res = 0;
            byte syncCounter = 0;
            bool resultOk = false;
            do
            {
                res = COP_CheckSync(_keyHandle, out syncCounter);
                if (COP_k_OK == res)
                {
                    resultOk = true;
                }

                if (BER_k_ERR == res)
                {
                    throw new CDeviceException($"SyncCallback : COP_CheckSync {CopErrorString(res)}");
                }

                if (BER_k_PC_MC_COMM_ERR == res)
                {
                    throw new CDeviceException($"SyncCallback : COP_CheckSync {CopErrorString(res)}");
                }
            } while (COP_k_QUEUE_EMPTY != res);

            if (resultOk)
                lock (SyncLock)
                {
                    SyncEvent = false;
                }
        }

        #endregion

        #region EventMeassages

        public string GetLastEventMsg()
        {
            var description = " ";
            switch (LastEventMsg.evt_type)
            {
                case COP_k_NMT_EVT:
                {
                    description =
                        $"({LastEventMsg.evt_type:X02}h){CopEventTypeString(LastEventMsg.evt_type)}, " +
                        $"({LastEventMsg.evt_data1:X02}h){EventNMTtoStringData1(LastEventMsg.evt_data1)}, " +
                        $"({LastEventMsg.evt_data2:X02}h)Node:{LastEventMsg.evt_data2}, " +
                        $"({LastEventMsg.evt_data3:X02}h)Status:{EventNMTtoStringData3(LastEventMsg.evt_data3)}, ";
                }
                    break;
                case COP_k_DLL_EVT:
                {
                    description =
                        $" ({LastEventMsg.evt_type:X02}h){CopEventTypeString(LastEventMsg.evt_type)}, ";


                    for (var i = 0; i < 6; i++)
                    {
                        var code = (byte)(1 << i);
                        if ((LastEventMsg.evt_data1 & code) != 0)
                            description += $"({code:X02}h){EventDLLtoStringData1(code)}, ";
                    }
                }
                    break;
                case COP_k_WPDO_EVT:
                {
                    description =
                        $"({LastEventMsg.evt_type:X02}h){CopEventTypeString(LastEventMsg.evt_type)}, " +
                        $"({LastEventMsg.evt_data1:X02}h){EventPDOtoStringData1(LastEventMsg.evt_data1)}, " +
                        $"({LastEventMsg.evt_data2:X02}h)Node:{LastEventMsg.evt_data2}, " +
                        $"({LastEventMsg.evt_data3:X02}h)Number:{LastEventMsg.evt_data3}, ";
                }
                    break;
                case COP_k_RPDO_EVT:
                {
                    description =
                        $"({LastEventMsg.evt_type:X02}h){CopEventTypeString(LastEventMsg.evt_type)}, " +
                        $"({LastEventMsg.evt_data1:X02}h){EventPDOtoStringData1(LastEventMsg.evt_data1)}, " +
                        $"({LastEventMsg.evt_data2:X02}h)Node:{LastEventMsg.evt_data2}, " +
                        $"({LastEventMsg.evt_data3:X02}h)Number:{LastEventMsg.evt_data3}, ";
                }
                    break;
                case COP_k_QUEUE_OVRUN_EVT:
                {
                    description =
                        $"({LastEventMsg.evt_type:X02}h){CopEventTypeString(LastEventMsg.evt_type)}, " +
                        $"({LastEventMsg.evt_data1:X02}h)EMCY, " +
                        $"({LastEventMsg.evt_data2:X02}h)RPDO{LastEventMsg.evt_data2}, " +
                        $"({LastEventMsg.evt_data3:X02}h)Event{LastEventMsg.evt_data3}, ";
                }
                    break;
                case COP_k_FLY_EVT:
                {
                    description =
                        $"({LastEventMsg.evt_type:X02}h){CopEventTypeString(LastEventMsg.evt_type)}, ";
                }
                    break;
                default:
                {
                    description = $"({LastEventMsg.evt_type:X02}h){CopEventTypeString(LastEventMsg.evt_type)}, ";
                }
                    break;
            }

            return description;
        }

        private string EventNMTtoStringData1(byte errorCode)
        {
            string description;
            switch (errorCode)
            {
                case COP_k_NMT_GUARDERR:
                    description = "Guard error";
                    break;
                case COP_k_NMT_BOOTIND:
                    description = "Device has sent Bootup message";
                    break;
                case COP_k_NMT_HEARTBEATERR:
                    description = "Heartbeat error";
                    break;
                default:
                    description = "Unknow";
                    break;
            }

            return description;
        }

        private string EventNMTtoStringData3(byte errorCode)
        {
            string description;
            switch (errorCode)
            {
                case COP_k_NS_BOOTUP:
                    description = "BOOTUP";
                    break;
                case COP_k_NS_DISCONNECTED:
                    description = "DISCONNECTED";
                    break;
                case COP_k_NS_STOPPED:
                    description = "STOPPED";
                    break;
                case COP_k_NS_OPERATIONAL:
                    description = "OPERATIONAL";
                    break;
                case COP_k_NS_PREOPERATIONAL:
                    description = "PREOPERATIONAL";
                    break;
                case COP_k_NS_UNKNOWN:
                    description = "UNKNOWN";
                    break;
                default:
                    description = "Unknow";
                    break;
            }

            return description;
        }

        private string EventDLLtoStringData1(byte errorCode)
        {
            string description;
            switch (errorCode)
            {
                case COP_k_DLL_NOERR:
                    description = "No error";
                    break;
                case COP_k_DLL_RXOVR:
                    description = "Overrun of the firmware-internal receive queue";
                    break;
                case COP_k_DLL_COVR:
                    description = "Overrun of the receive queue of the CANController";
                    break;
                case COP_k_DLL_BOFF:
                    description = "CAN-Controller in the BusOff state";
                    break;
                case COP_k_DLL_ESET:
                    description = "Error Status Bit of the CAN-Controller set";
                    break;
                case COP_k_DLL_ERESET:
                    description = "Error Status Bit of the CAN-Controller reset";
                    break;
                case COP_k_DLL_TXOVR:
                    description = "Overrun of the firmware-internal transmit queue";
                    break;

                default:
                    description = "Unknow";
                    break;
            }

            return description;
        }

        private string EventPDOtoStringData1(byte errorCode)
        {
            string description;
            switch (errorCode)
            {
                case COP_k_ERR_PDO_IV:
                    description = " ";
                    break;
                case COP_k_ERR_PDO_OVR:
                    description = " ";
                    break;
                default:
                    description = "Unknow";
                    break;
            }

            return description;
        }

        #endregion
    }
}