using System;
using System.Buffers.Binary;
using System.Threading;

namespace EposCmd.Net
{
    public class CEpos4CommandGroupCO : CCommandGroupCO
    {
        // Typovaný prístup k EPOS4 dátam
        protected CDataEpos4 Data => (CDataEpos4)BaseData;

        protected void SetControlword(ushort value)
        {
            try
            {
                var Txdata = BitConverter.GetBytes(value);
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
                var Txdata = BitConverter.GetBytes(value);
                WritePDO(2, Txdata);
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
                var Txdata = BitConverter.GetBytes(value);
                Data.TxdataPDO3[0] = Txdata[0];
                Data.TxdataPDO3[1] = Txdata[1];
                Data.TxdataPDO3[2] = Txdata[2];
                Data.TxdataPDO3[3] = Txdata[3];
                WritePDO(3, Data.TxdataPDO3);
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
                BinaryPrimitives.WriteInt32LittleEndian(Data.TxdataPDO4.AsSpan(0, 4), targetPosition);
                WritePDO(4, Data.TxdataPDO4);
            }
        }

        public void WritePDO3TargetTorque(short targetTorque)
        {
            lock (Data.NodePdoLock)
            {
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
                    BinaryPrimitives.WriteUInt16LittleEndian(Data.TxdataPDO1.AsSpan(0, 2), controlword);
                    BinaryPrimitives.WriteInt32LittleEndian(Data.TxdataPDO1.AsSpan(2, 4), targetPosition);
                    WritePDO(1, Data.TxdataPDO1);
                }
            }
            catch (CDeviceException e)
            {
                throw new CDeviceException($"SetCW_TP:[CW:0x{controlword:X04}, TP:{targetPosition}] {e.ErrorMessage}", 0);
            }
        }

        protected void SetSwitchOnAndWait()
        {
            try
            {
                SetControlword(0x06);
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
                SetControlword(0x0F);
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
                SetControlword(0x00);
                WaitForDisableState(500);
            }
            catch (CDeviceException e)
            {
                throw new CDeviceException($"SetEneble: {e.ErrorMessage}", 0);
            }
        }

        protected void SetModeOfOperation(EOperationMode value)
        {
            if (Data.ModeOfOperationDisplay == value) return;

            Data.ResetWpdoError();
            WritePDO3ModeOfOperation(value);

            bool conditionMet = SpinWait.SpinUntil(() =>
                Data.ModeOfOperationDisplay == value || Data.WpdoError || Data.FaultState, 1000);

            if (!conditionMet)
                throw new CDeviceException($"SetModeOfOperation Node:{NodeId}. Timeout waiting for mode {value}. Actual mode is {Data.ModeOfOperationDisplay}.", 0);
            if (Data.WpdoError)
                throw new CDeviceException($"SetModeOfOperation Node:{NodeId}. Async WPDO Error on PDO {Data.WpdoErrorPdoNumber}", 0);
            if (Data.FaultState)
                throw new CDeviceException($"SetModeOfOperation Node:{NodeId}. Device entered Fault state during mode change.", 0);
        }

        public void WaitForACK()
        {
            WaitForSetACK(500);
            SetEnableStateAndWait();
            WaitForResetACK(500);
        }

        private void WaitForEnableState(int time)
        {
            if (!SpinWait.SpinUntil(() => Data.EnableState || Data.WpdoError, time))
                throw new CDeviceException($"SetEnable Node:{NodeId:d}. Timeout set enable. Time:{time}", 0);
            if (Data.WpdoError)
                throw new CDeviceException($"SetEnable Node:{NodeId}. Async WPDO Error on PDO {Data.WpdoErrorPdoNumber}", 0);
        }

        private void WaitForDisableState(int time)
        {
            if (!SpinWait.SpinUntil(() => Data.DisableState, time))
                throw new CDeviceException($"SetDisable Node:{NodeId:d}. Timeout set disable. Time:{time}", 0);
        }

        private void WaitForSwitchOn(int time)
        {
            if (!SpinWait.SpinUntil(() => Data.ReadyToSwitchOn, time))
                throw new CDeviceException($"SetSwitchOn  Node:{NodeId:d}. Timeout set SwitchOn. Time:{time}", 0);
        }

        public void WaitForSetACK(uint timeout)
        {
            if (!SpinWait.SpinUntil(() => Data.Ack || !Data.EnableState || Data.WpdoError, (int)timeout))
                throw new CDeviceException($"Wait for set ACK  Node:{NodeId}. Timeout {timeout}", 0);
            if (Data.WpdoError)
                throw new CDeviceException($"Wait for set ACK Node:{NodeId}. Async WPDO Error on PDO {Data.WpdoErrorPdoNumber}", 0);
        }

        public void WaitForResetACK(uint timeout)
        {
            if (!SpinWait.SpinUntil(() => !Data.Ack || Data.WpdoError, (int)timeout))
                throw new CDeviceException($"Wait for reset ACK  Node:{NodeId:d}. Timeout {timeout}", 0);
            if (Data.WpdoError)
                throw new CDeviceException($"Wait for reset ACK Node:{NodeId}. Async WPDO Error on PDO {Data.WpdoErrorPdoNumber}", 0);
        }

        protected EOperationMode GetModeOfOperation() => Data.ModeOfOperationDisplay;

        protected EStates GetStateCommand()
        {
            if (Data.FaultState) return EStates.StFault;
            if (Data.QuickStopState) return EStates.StQuickStop;
            if (Data.DisableState) return EStates.StDisabled;
            if (Data.EnableState) return EStates.StEnabled;
            if (Data.ReadyToSwitchOn) return EStates.StReadyToSwitchOn;
            if (!Data.RemoteStatus)
                throw new CDeviceException($" GetState Node {NodeId:d}: Remote Node Off [0x{Data.Statusword:X02}] ", 0);
            throw new CDeviceException($" GetState Node {NodeId:d}: Unknow statusword value [0x{Data.Statusword:X02}] ", 0);
        }

        public EStates GetState() => GetStateCommand();

        public void SetDisableState()
        {
            var state = GetState();
            switch (state)
            {
                case EStates.StEnabled:
                case EStates.StQuickStop:
                case EStates.StReadyToSwitchOn:
                    SetControlword(0x00);
                    break;
                case EStates.StFault:
                    throw new CDeviceException($"SetDisable  Node:{NodeId:d}. Device is fault.", 0);
            }
        }

        public void SetEnableState()
        {
            var state = GetState();
            switch (state)
            {
                case EStates.StEnabled:
                case EStates.StQuickStop:
                case EStates.StReadyToSwitchOn:
                    SetEnableStateAndWait();
                    break;
                case EStates.StDisabled:
                    SetReadyToSwitchAndWait();
                    SetEnableStateAndWait();
                    break;
                case EStates.StFault:
                    throw new CDeviceException($"SetEnable  Node:{NodeId:d}. Device is fault.", 0);
            }
        }

        public void SetReadyToSwitchOnState()
        {
            var state = GetState();
            switch (state)
            {
                case EStates.StEnabled:
                case EStates.StDisabled:
                    SetReadyToSwitchAndWait();
                    break;
                case EStates.StQuickStop:
                    SetEnableStateAndWait();
                    SetReadyToSwitchAndWait();
                    break;
                case EStates.StFault:
                    throw new CDeviceException($"SetReadyToSwitchOn  Node:{NodeId:d}. Device is fault.", 0);
            }
        }

        private void SetReadyToSwitchAndWait()
        {
            SetControlword(0x06);
            if (!SpinWait.SpinUntil(() => Data.ReadyToSwitchOn, 250))
                throw new CDeviceException($"SetEnable  Node:{NodeId:d}. Timeout set switch on.", 0);
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
                    throw new CDeviceException($"SetQuickStopState  Node:{NodeId:d}. Device is fault.", 0);
                case EStates.StDisabled:
                    throw new CDeviceException($"SetQuickStopState  Node:{NodeId:d}. Device is Disable.", 0);
                case EStates.StReadyToSwitchOn:
                    throw new CDeviceException($"SetQuickStopState  Node:{NodeId:d}. Device is ReadyToSwitchOn.", 0);
            }
        }

        public void SetState(EStates state)
        {
            switch (state)
            {
                case EStates.StDisabled: SetDisableState(); break;
                case EStates.StEnabled: SetEnableState(); break;
                case EStates.StQuickStop: SetQuickStopState(); break;
            }
        }

        public void ClearFault()
        {
            if (Data.FaultState)
            {
                WritedSDO(0x1003, 0, 0, 1);
                SetControlword(0x0000);
                Thread.Sleep(10);
                SetControlword(0x0080);

                if (!SpinWait.SpinUntil(() => !Data.FaultState, 1000))
                    throw new CDeviceException($"ClearFault Node:{NodeId}. Timeout waiting for Fault state clear.", 0);

                SetControlword(0x0006);
                if (!SpinWait.SpinUntil(() => Data.ReadyToSwitchOn, 500))
                    throw new CDeviceException($"ClearFault Node:{NodeId}. Timeout waiting for ReadyToSwitchOn.", 0);

                SetControlword(0x0007);
                if (!SpinWait.SpinUntil(() => (Data.Statusword & 0x007F) == 0x0023, 500))
                    throw new CDeviceException($"ClearFault Node:{NodeId}. Timeout waiting for Switched On.", 0);

                SetControlword(0x000F);
                if (!SpinWait.SpinUntil(() => Data.EnableState, 500))
                    throw new CDeviceException($"ClearFault Node:{NodeId}. Timeout waiting for Operation Enabled.", 0);
            }
        }
    }
}