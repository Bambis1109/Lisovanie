using System;
using System.Threading;
using static IXXAT.CANopenMasterAPI6;


namespace EposCmd
{
    namespace Net
    {
        public class CCommandGroupCO : ErrorHandlingCO
        {
            protected CDataCO Data;
            protected ushort KeyHandle;
            protected byte NodeId;
            protected void WritedSDO(ushort Index, byte Subindex, ulong Value, ushort Len)
            {
                lock (Data.NodeSdoLock)
                {
                    short res;
                    uint abortcode = 0;
                    var Txdata = new byte[Len];
                    Txdata = BitConverter.GetBytes(Value);
                    res = COP_WriteSDO(KeyHandle //  handle of CAN board
                        , NodeId //  number of the node
                        , COP_k_DEFAULT_SDO
                        , COP_k_NO_BLOCKTRANSFER
                        , Index //  index in OV
                        , Subindex //  subindex in OV
                        , Len //  length of transmit data
                        , Txdata //  transmit data
                        , out abortcode); //  abort code of SDO-transfer
                    if (COP_k_OK != res)
                    {
                        var Message = string.Format("WriteSDO  Node {0:d}  [index:0x{1:X04} sub:0x{2:X02}]", NodeId, Index,
                            Subindex);

                        if (COP_k_ABORT == res)
                            throw new CDeviceException(Message + "  (" + CopAbortCodeString(abortcode) + ")", abortcode);

                        throw new CDeviceException(Message + "  (" + CopErrorString(res) + ")", (uint)res);
                    }
                }
            }
            protected ulong ReadSdo(ushort Index, byte Subindex, ushort Len)
            {
                lock (Data.NodeSdoLock)
                {
                    short res;
                    var rxdata = new byte[8];
                    uint abortcode = 0;
                    uint rxLen = Len;
                    res = COP_ReadSDO(KeyHandle //  handle of CAN board
                        , NodeId //  number of the node
                        , COP_k_DEFAULT_SDO
                        , COP_k_NO_BLOCKTRANSFER
                        , Index //  index in OV
                        , Subindex //  subindex in OV
                        , ref rxLen //  size of buffer / length of received data
                        , rxdata //  received data
                        , out abortcode); //  abort code of SDO-transfer 

                    if (COP_k_OK == res) return BitConverter.ToUInt64(rxdata, 0);
                    var Message = string.Format("WriteSDO  Node {0:d}  [index:0x{1:X04} sub:0x{2:X02}]", NodeId, Index,
                        Subindex);
                    if (COP_k_ABORT == res)
                        throw new CDeviceException(Message + "  (" + CopAbortCodeString(abortcode) + ")", abortcode);
                    throw new CDeviceException(Message + "  (" + CopErrorString(res) + ")", (uint)res);
                }
            }
            protected void WritePDO(byte Pdo, byte[] TxData)
            {
                lock (Data.NodePdoLock)
                {
                    var temp = TxData.Clone();
                    if (!Data.RemoteStatus)
                        throw new CDeviceException($"WritePDO  [Node:{NodeId:d}]  [PDO:{NodeId:d}] (Remote status off.) ", 0);
                        
                    short res;
                    int retries = 0;
                    const int maxRetries = 1000;
                    var spinWait = new SpinWait();
                    
                    do
                    {
                        res = COP_WritePDO(KeyHandle //  handle of CAN board
                            , NodeId //  number of the node
                            , Pdo //  number of the pdo
                            , TxData);
                            
                        if (res == COP_k_BSY)
                        {
                            spinWait.SpinOnce();
                            retries++;
                        }
                    } while (res == COP_k_BSY && retries < maxRetries);

                    if (COP_k_OK != res)
                    {
                        throw new CDeviceException($"WritePDO  [Node:{NodeId:d}]  [PDO:{Pdo:d}] ({CopErrorString(res)}) ", (uint)res);
                    }
                }
            }
            protected void SetControlword(ushort value)
            {
                try
                {
                    var Txdata = new byte[2];
                    Txdata = BitConverter.GetBytes(value);

                    WritePDO(1, Txdata);

                }
                catch (CDeviceException e)
                {
                    throw new CDeviceException($"SetControlWord:[{value:X02}] {e.ErrorMessage}", 0);
                }
            }
            protected void SetControlwordSync(ushort value)
            {
                try
                {
                    var Txdata = new byte[2];
                    Txdata = BitConverter.GetBytes(value);
                    WritePDO(2, Txdata); // Prepisany PDO na 1 z 2
                }
                catch (CDeviceException e) { throw new CDeviceException($"SetControlWordSync:[{value:X02}] {e.ErrorMessage}", 0); }
            }
            protected void SetCW_TP(ushort controlword, int targetPosition)
            {
                try
                {
                    var txData = new byte[8];
                    BitConverter.GetBytes(controlword).CopyTo(txData, 0);
                    BitConverter.GetBytes(targetPosition).CopyTo(txData, 2);

                    WritePDO(1, txData);
                }
                catch (CDeviceException e)
                {
                    throw new CDeviceException($"SetCW_TP:[CW:0x{controlword:X04}, TP:{targetPosition}] {e.ErrorMessage}", 0);
                }
            }
            
            public void WritePDO3SetupOutput(UInt32 value)
            {
                lock (Data.NodePdoLock)
                {
                    var Txdata = new byte[4];
                    Txdata = BitConverter.GetBytes(value);
                    Data.TxdataPDO3[0] = Txdata[0];
                    Data.TxdataPDO3[1] = Txdata[1];
                    Data.TxdataPDO3[2] = Txdata[2];
                    Data.TxdataPDO3[3] = Txdata[3];
                    WritePDO(3, Data.TxdataPDO3); // WritePDO3SetupOutput
                }
            }
            public void WritePDO3ModeOfOperation(EOperationMode operationMode)
            {
                lock (Data.NodePdoLock)
                {
                    var Txdata = new byte[1];
                    Txdata = BitConverter.GetBytes((short)(byte)operationMode);
                    Data.TxdataPDO3[4] = Txdata[0];
                    WritePDO(3, Data.TxdataPDO3); // WritePDO3ModeOfOperation
                }
            }
            public void WritePDO3TargetTorque(short targetTorque)
            {
                lock (Data.NodePdoLock)
                {
                    var Txdata = new byte[2];
                    Txdata = BitConverter.GetBytes(unchecked((ushort)targetTorque));
                    Data.TxdataPDO4[5] = Txdata[0];
                    Data.TxdataPDO4[6] = Txdata[1];
                    WritePDO(3, Data.TxdataPDO3); //WritePDO3TargetTorque
                }
            }
            public void WritePDO4TargetPosition(int targetPosition)
            {
                lock (Data.NodePdoLock)
                {
                    var Txdata = new byte[4];
                    Txdata = BitConverter.GetBytes(targetPosition);
                    Data.TxdataPDO4[0] = Txdata[0];
                    Data.TxdataPDO4[1] = Txdata[1];
                    Data.TxdataPDO4[2] = Txdata[2];
                    Data.TxdataPDO4[3] = Txdata[3];
                    WritePDO(4, Data.TxdataPDO4);
                }
            }
       
            protected void SetSwitchOnAndWait()
            {
                try
                {
                    SetControlword(0x06); // Set SwitchOn
                    WaitForSwitchOn(500);
                }
                catch (CDeviceException e) { throw new CDeviceException($"SetSwitchOn: {e.ErrorMessage}", 0); }
            }
            protected void SetEnableStateAndWait()
            {
                try
                {
                    SetControlword(0x0F); // Set Enable

                    WaitForEnableState(500);
                }
                catch (CDeviceException e) { throw new CDeviceException($"SetEnable: {e.ErrorMessage}", 0); }
            }
            protected void SetDisableStateAndWait()
            {
                try
                {
                    SetControlword(0x00); // Set Enable
                    WaitForDisableState(500);
                }
                catch (CDeviceException e) { throw new CDeviceException($"SetEneble: {e.ErrorMessage}", 0); }
            }
            protected void SetModeOfOperation(EOperationMode value)
            {
                EOperationMode temp = value;

                if (Data.ModeOfOperationDisplay == value)
                    return;
                
                WritePDO3ModeOfOperation(value);
                if (!SpinWait.SpinUntil(() => Data.ModeOfOperationDisplay == value, 1000))
                {
                    Thread.Sleep(1000);
                    var message1 = $"SetOperetion mode from {temp} to {value} actual mode is {Data.ModeOfOperationDisplay} Node:{NodeId:d}. Timeout .";
                    Thread.Sleep(1000);
                    WritePDO3ModeOfOperation(value);
                    if (!SpinWait.SpinUntil(() => Data.ModeOfOperationDisplay == value, 1000))
                    {
                        Thread.Sleep(1000);
                        var message2 = $"SetOperetion mode from {temp} to {value} actual mode is {Data.ModeOfOperationDisplay} Node:{NodeId:d}. Timeout .";
                        throw new CDeviceException($"{message1}---{message2}", 0);
                    }
                }
                Thread.Sleep(1);
            }
            public void WaitForACK()
            {
                WaitForSetACK(2000); //Cakanie na Ack = true
                SetEnableStateAndWait(); // Resetnutie ACK
                WaitForResetACK(2000); // Cakanie na ACK = false
            }
            private void WaitForEnableState(int time)
            {
                if (!SpinWait.SpinUntil(() => { var x = Data.Statusword; return Data.EnableState; }, time))
                {
                    var message = $"SetEnable Node:{NodeId:d}. Timeout set enable. Time:{time}"; throw new CDeviceException(message, 0);
                }
            }
            private void WaitForDisableState(int time)
            {
                if (!SpinWait.SpinUntil(() => Data.DisableState, time))
                {
                    var message = $"SetDisable Node:{NodeId:d}. Timeout set disable. Time:{time}"; throw new CDeviceException(message, 0);
                }
            }
            private void WaitForSwitchOn(int time)
            {
                if (!SpinWait.SpinUntil(() => Data.ReadyToSwitchOn, time))
                {
                    var message = $"SetSwitchOn  Node:{NodeId:d}. Timeout set SwitchOn. Time:{time}"; throw new CDeviceException(message, 0);
                }
            }
            public void WaitForSetACK(uint timeout)
            {
                if (!SpinWait.SpinUntil(() => Data.Ack || !Data.EnableState, (int)timeout))
                {
                    throw new CDeviceException($"Wait for set ACK  Node:{NodeId}. Timeout {timeout}", 0);
                }
            }
            public void WaitForResetACK(uint timeout)
            {
                if (!SpinWait.SpinUntil(() => !Data.Ack, (int)timeout))
                {
                    throw new CDeviceException($"Wait for reset ACK  Node:{NodeId:d}. Timeout {timeout}", 0);
                }
            }
            public void WaitForEnableState(uint timeout)
            {
                if (!SpinWait.SpinUntil(() => Data.Ack, (int)timeout))
                {
                    var Message = $"Wait for Enable  Node:{NodeId:d}. Timeout";
                    throw new CDeviceException(Message, 0);
                }
            }
            protected EOperationMode GetModeOfOperation() { return Data.ModeOfOperationDisplay; }
            protected EStates GetStateCommand()
            {
                if (Data.FaultState) return EStates.StFault;
                if (Data.QuickStopState) return EStates.StQuickStop;
                if (Data.DisableState) return EStates.StDisabled;
                if (Data.EnableState) return EStates.StEnabled;
                if (Data.ReadyToSwitchOn) return EStates.StReadyToSwitchOn;
                if (!Data.RemoteStatus)
                    throw new CDeviceException($" GetState Node {NodeId:d}: Remote Node Off [0x{Data.Statusword:X02}] ",
                        0);
                throw new CDeviceException(
                    $" GetState Node {NodeId:d}: Unknow statusword value [0x{Data.Statusword:X02}] ", 0);
            }
            public EStates GetState() { return GetStateCommand(); }
            public void SetDisableState()
            {
                var state = GetState();
                switch (state)
                {
                    case EStates.StEnabled:
                        SetControlword(0x00);
                        break;
                    case EStates.StFault:
                        {
                            var Message = string.Format("SetDisable  Node:{0:d}. Device is fault.", NodeId);
                            throw new CDeviceException(Message, 0);
                        }
                    case EStates.StDisabled:
                        {

                        }
                        break;
                    case EStates.StQuickStop:
                        {
                            SetControlword(0x00);
                        }
                        break;
                    case EStates.StReadyToSwitchOn:
                        {
                            SetControlword(0x00);
                        }
                        break;
                    default:
                        break;
                }
            }
            public void SetEnableState()
            {
                var state = GetState();
                switch (state)
                {
                    case EStates.StEnabled:
                        SetEnableStateAndWait();
                        break;
                    case EStates.StFault:
                        {
                            var Message = string.Format("SetEnable  Node:{0:d}. Device is fault.", NodeId);
                            throw new CDeviceException(Message, 0);
                        }
                    case EStates.StDisabled:
                        {
                            SetReadyToSwitchAndWait();
                            SetEnableStateAndWait();
                        }
                        break;
                    case EStates.StQuickStop:
                        {
                            SetEnableStateAndWait();
                        }
                        break;
                    case EStates.StReadyToSwitchOn:
                        {
                            SetEnableStateAndWait();
                        }
                        break;
                    default:
                        break;
                }
            }
            public void SetReadyToSwitchOnState()
            {
                var state = GetState();
                switch (state)
                {
                    case EStates.StEnabled:
                        SetReadyToSwitchAndWait();
                        break;
                    case EStates.StFault:
                        {
                            var Message = string.Format("SetReadyToSwitchOn  Node:{0:d}. Device is fault.", NodeId);
                            throw new CDeviceException(Message, 0);
                        }
                    case EStates.StDisabled:
                        {
                            SetReadyToSwitchAndWait();

                        }
                        break;
                    case EStates.StQuickStop:
                        {
                            SetEnableStateAndWait();
                            SetReadyToSwitchAndWait();
                        }
                        break;
                    case EStates.StReadyToSwitchOn:
                        {

                        }
                        break;
                    default:
                        break;
                }
            }
            private void SetReadyToSwitchAndWait()
            {
                SetControlword(0x06); //ShutDown
                if (!SpinWait.SpinUntil(() => Data.ReadyToSwitchOn, 250))
                {
                    var Message = $"SetEnable  Node:{NodeId:d}. Timeout set switch on.";
                    throw new CDeviceException(Message, 0);
                }
            }
            public void SetQuickStopState()
            {

                var state = GetState();
                switch (state)
                {
                    case EStates.StEnabled:
                        SetControlword(0x0B);
                        break;
                    case EStates.StFault:
                        {
                            var Message = string.Format("SetQuickStopState  Node:{0:d}. Device is fault.", NodeId);
                            throw new CDeviceException(Message, 0);
                        }

                    case EStates.StDisabled:
                        {
                            var Message = string.Format("SetQuickStopState  Node:{0:d}. Device is Disable.", NodeId);
                            throw new CDeviceException(Message, 0);
                        }

                    case EStates.StQuickStop:
                        {

                        }
                        break;
                    case EStates.StReadyToSwitchOn:
                        {
                            var Message = string.Format("SetQuickStopState  Node:{0:d}. Device is ReadyToSwitchOn.", NodeId);
                            throw new CDeviceException(Message, 0);
                        }

                    default:
                        break;
                }
            }
            public void SetState(EStates state)
            {
                switch (state)
                {
                    case EStates.StDisabled:
                        SetDisableState();
                        break;
                    case EStates.StEnabled:
                        SetEnableState();
                        break;
                    case EStates.StQuickStop:
                        SetQuickStopState();
                        break;
                    default: break;
                }
            }
            public void ClearFault()
            {
                if (Data.FaultState)
                {
                    WritedSDO(0x1003, 0, 0, 1);    //SetControlword(0x00); //Disable
                    Thread.Sleep(1);
                    SetControlword(0x80); // Clear device error
                    Thread.Sleep(100);
                }
            }
        }
    }
}