using System.Buffers.Binary;
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
                    var Txdata = new byte[8];
                    BinaryPrimitives.WriteUInt64LittleEndian(Txdata, Value);

                    int retries = 0;
                    var spinWait = new SpinWait();
                    do
                    {
                        res = COP_WriteSDO(KeyHandle, NodeId, COP_k_DEFAULT_SDO, COP_k_NO_BLOCKTRANSFER, Index,
                            Subindex, Len, Txdata, out abortcode);
                        if (res == COP_k_SDO_RUNNING || res == COP_k_BSY)
                        {
                            spinWait.SpinOnce();
                            retries++;
                        }
                    } while ((res == COP_k_SDO_RUNNING || res == COP_k_BSY) && retries < 1000);

                    if (COP_k_OK != res)
                    {
                        var Message = $"WriteSDO Node {NodeId:d} [index:0x{Index:X04} sub:0x{Subindex:X02}]";
                        if (COP_k_ABORT == res)
                            throw new CDeviceException($"{Message} ({CopAbortCodeString(abortcode)})", abortcode);
                        throw new CDeviceException($"{Message} ({CopErrorString(res)})", (uint)res);
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
                    if (!Data.RemoteStatus)
                        throw new CDeviceException($"WritePDO  [Node:{NodeId:d}]  [PDO:{Pdo:d}] (Remote status off.) ",
                            0);

                    short res = COP_WritePDO(KeyHandle, NodeId, Pdo, TxData);

                    if (res != COP_k_OK)
                    {
                        throw new CDeviceException(
                            $"WritePDO  [Node:{NodeId:d}]  [PDO:{Pdo:d}] ({CopErrorString(res)}) ", (uint)res);
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
                catch (CDeviceException e)
                {
                    throw new CDeviceException($"SetControlWordSync:[{value:X02}] {e.ErrorMessage}", 0);
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
                    Data.TxdataPDO3[4] = (byte)operationMode;
                    WritePDO(3, Data.TxdataPDO3);
                }
            }

            public void WritePDO4TargetPosition(int targetPosition)
            {
                lock (Data.NodePdoLock)
                {
                    // Zápis 4 bajtov (Int32) priamo do existujúceho poľa bez alokácie na halde
                    BinaryPrimitives.WriteInt32LittleEndian(Data.TxdataPDO4.AsSpan(0, 4), targetPosition);
                    WritePDO(4, Data.TxdataPDO4);
                }
            }

            public void WritePDO3TargetTorque(short targetTorque)
            {
                lock (Data.NodePdoLock)
                {
                    // Zápis 2 bajtov (Int16) priamo do existujúceho poľa PDO3 na pozíciu 5 a 6.
                    BinaryPrimitives.WriteInt16LittleEndian(Data.TxdataPDO3.AsSpan(5, 2), targetTorque);
                    WritePDO(3, Data.TxdataPDO3);
                }
            }

            protected void SetCW_TP(ushort controlword, int targetPosition)
            {
                try
                {
                    lock (Data.NodePdoLock)
                    {
                        // Zápis Controlword (2 bajty) a Target Position (4 bajty) do existujúceho poľa
                        BinaryPrimitives.WriteUInt16LittleEndian(Data.TxdataPDO1.AsSpan(0, 2), controlword);
                        BinaryPrimitives.WriteInt32LittleEndian(Data.TxdataPDO1.AsSpan(2, 4), targetPosition);

                        WritePDO(1, Data.TxdataPDO1);
                    }
                }
                catch (CDeviceException e)
                {
                    throw new CDeviceException(
                        $"SetCW_TP:[CW:0x{controlword:X04}, TP:{targetPosition}] {e.ErrorMessage}", 0);
                }
            }

            protected void SetSwitchOnAndWait()
            {
                try
                {
                    SetControlword(0x06); // Set SwitchOn
                    WaitForSwitchOn(500);
                }
                catch (CDeviceException e)
                {
                    throw new CDeviceException($"SetSwitchOn: {e.ErrorMessage}", 0);
                }
            }

            protected void SetEnableStateAndWait()
            {
                try
                {
                    SetControlword(0x0F); // Set Enable

                    WaitForEnableState(500);
                }
                catch (CDeviceException e)
                {
                    throw new CDeviceException($"SetEnable: {e.ErrorMessage}", 0);
                }
            }

            protected void SetDisableStateAndWait()
            {
                try
                {
                    SetControlword(0x00); // Set Enable
                    WaitForDisableState(500);
                }
                catch (CDeviceException e)
                {
                    throw new CDeviceException($"SetEneble: {e.ErrorMessage}", 0);
                }
            }

            protected void SetModeOfOperation(EOperationMode value)
            {
                // Ak už sme v požadovanom režime, okamžitý návrat
                if (Data.ModeOfOperationDisplay == value)
                    return;

                // Reset asynchrónneho chybového príznaku pred novým príkazom
                Data.ResetWpdoError();

                // Odoslanie požiadavky na zmenu režimu
                WritePDO3ModeOfOperation(value);

                // Exaktné čakanie na potvrdenie od EPOS4 (zmena v TxPDO1)
                bool conditionMet = SpinWait.SpinUntil(() =>
                    Data.ModeOfOperationDisplay == value || Data.WpdoError || Data.FaultState, 1000);

                // Vyhodnotenie výsledku
                if (!conditionMet)
                {
                    throw new CDeviceException(
                        $"SetModeOfOperation Node:{NodeId}. Timeout waiting for mode {value}. Actual mode is {Data.ModeOfOperationDisplay}.",
                        0);
                }

                if (Data.WpdoError)
                {
                    throw new CDeviceException(
                        $"SetModeOfOperation Node:{NodeId}. Async WPDO Error on PDO {Data.WpdoErrorPdoNumber}", 0);
                }

                if (Data.FaultState)
                {
                    throw new CDeviceException(
                        $"SetModeOfOperation Node:{NodeId}. Device entered Fault state during mode change.", 0);
                }
            }

            public void WaitForACK()
            {
                WaitForSetACK(500); //Cakanie na Ack = true
                SetEnableStateAndWait(); // Resetnutie ACK
                WaitForResetACK(500); // Cakanie na ACK = false
            }


            private void WaitForEnableState(int time)
            {
                if (!SpinWait.SpinUntil(() => Data.EnableState || Data.WpdoError, time))
                {
                    var message = $"SetEnable Node:{NodeId:d}. Timeout set enable. Time:{time}";
                    throw new CDeviceException(message, 0);
                }

                if (Data.WpdoError)
                {
                    throw new CDeviceException(
                        $"SetEnable Node:{NodeId}. Async WPDO Error on PDO {Data.WpdoErrorPdoNumber}", 0);
                }
            }

            private void WaitForDisableState(int time)
            {
                if (!SpinWait.SpinUntil(() => Data.DisableState, time))
                {
                    var message = $"SetDisable Node:{NodeId:d}. Timeout set disable. Time:{time}";
                    throw new CDeviceException(message, 0);
                }
            }

            private void WaitForSwitchOn(int time)
            {
                if (!SpinWait.SpinUntil(() => Data.ReadyToSwitchOn, time))
                {
                    var message = $"SetSwitchOn  Node:{NodeId:d}. Timeout set SwitchOn. Time:{time}";
                    throw new CDeviceException(message, 0);
                }
            }

            public void WaitForSetACK(uint timeout)
            {
                if (!SpinWait.SpinUntil(() => Data.Ack || !Data.EnableState || Data.WpdoError, (int)timeout))
                {
                    throw new CDeviceException($"Wait for set ACK  Node:{NodeId}. Timeout {timeout}", 0);
                }

                if (Data.WpdoError)
                {
                    throw new CDeviceException(
                        $"Wait for set ACK Node:{NodeId}. Async WPDO Error on PDO {Data.WpdoErrorPdoNumber}", 0);
                }
            }

            public void WaitForResetACK(uint timeout)
            {
                if (!SpinWait.SpinUntil(() => !Data.Ack || Data.WpdoError, (int)timeout))
                {
                    throw new CDeviceException($"Wait for reset ACK  Node:{NodeId:d}. Timeout {timeout}", 0);
                }

                if (Data.WpdoError)
                {
                    throw new CDeviceException(
                        $"Wait for reset ACK Node:{NodeId}. Async WPDO Error on PDO {Data.WpdoErrorPdoNumber}", 0);
                }
            }

            protected EOperationMode GetModeOfOperation()
            {
                return Data.ModeOfOperationDisplay;
            }

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

            public EStates GetState()
            {
                return GetStateCommand();
            }

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
                        var Message = string.Format("SetQuickStopState  Node:{0:d}. Device is ReadyToSwitchOn.",
                            NodeId);
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
                    // Vymazanie histórie chýb (ponechané z pôvodného kódu)
                    WritedSDO(0x1003, 0, 0, 1);

                    // 1. Garantovaný prechod Bitu 7 (Fault Reset) z 0 na 1
                    SetControlword(0x0000);
                    Thread.Sleep(10); // Krátka pauza pre spracovanie na strane EPOS4
                    SetControlword(0x0080);

                    // Čakanie na opustenie stavu Fault (EPOS4 prechádza do Switch On Disabled)
                    if (!SpinWait.SpinUntil(() => !Data.FaultState, 1000))
                    {
                        throw new CDeviceException($"ClearFault Node:{NodeId}. Timeout waiting for Fault state clear.",
                            0);
                    }

                    // 2. Sekvencia pre návrat do Operation Enabled (0x0006 -> 0x0007 -> 0x000F)

                    // Krok A: Shutdown (0x0006) -> prechod do Ready to Switch On
                    SetControlword(0x0006);
                    if (!SpinWait.SpinUntil(() => Data.ReadyToSwitchOn, 500))
                    {
                        throw new CDeviceException($"ClearFault Node:{NodeId}. Timeout waiting for ReadyToSwitchOn.",
                            0);
                    }

                    // Krok B: Switch On (0x0007) -> prechod do Switched On
                    // Pre Switched On stav je maska Statuswordu: xxxx xxxx x01x 0011 (0x0023)
                    SetControlword(0x0007);
                    if (!SpinWait.SpinUntil(() => (Data.Statusword & 0x007F) == 0x0023, 500))
                    {
                        throw new CDeviceException($"ClearFault Node:{NodeId}. Timeout waiting for Switched On.", 0);
                    }

                    // Krok C: Enable Operation (0x000F) -> prechod do Operation Enabled
                    SetControlword(0x000F);
                    if (!SpinWait.SpinUntil(() => Data.EnableState, 500))
                    {
                        throw new CDeviceException($"ClearFault Node:{NodeId}. Timeout waiting for Operation Enabled.",
                            0);
                    }
                }
            }
        }
    }
}