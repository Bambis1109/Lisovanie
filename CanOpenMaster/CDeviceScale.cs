using System;
using System.Buffers.Binary;
using EposCmd.Net.DeviceCmdSet.LowLayer;
using EposCmd.Net.DeviceCmdSet.Operation;
using EposCmd.Net.DeviceScaleSet;
//using EposCmd.Net.DeviceScaleSet;
using static IXXAT.CANopenMasterAPI6;

namespace EposCmd.Net
{
    public class CDeviceScale : CDeviceCO
    {
        private CDataScale ScaleData => (CDataScale)Data;

        public ScaleOperation Operation { get; }

        public CDeviceScale(ushort keyHandle, byte nodeId, string name)
        {
            Name = name;
            NodeId = nodeId;
            Data = new CDataScale(nodeId, Name);
            LowLayer = new LowLayer(keyHandle, nodeId, ScaleData);
            Operation = new ScaleOperation(keyHandle, nodeId, ScaleData);
        }

        public override void ReadPdo(COP_t_RX_PDO spPdo)
        {
            RecPdo(EventArgs.Empty);

            // Z pohľadu Mastra sú to RxPDO (1 až 4). Z pohľadu STM32 sú to TPDO1 až TPDO4.
            switch (spPdo.pdo_no)
            {
                case 1: // TPDO1
                    ScaleData.StatusMainProc = (EProcStatus)spPdo.a_data[0];
                    ScaleData.StatusMainMat = (EMatStatus)spPdo.a_data[1];
                    ScaleData.WeightResult = (EVaResult)spPdo.a_data[2];
                    ScaleData.StatusMainZone = (EZoneStatus)spPdo.a_data[3];
                    ScaleData.WeightFinal = BinaryPrimitives.ReadInt32LittleEndian(spPdo.a_data.AsSpan(4, 4));
                    break;

                case 2: // TPDO2
                    ScaleData.Weight32Inter = BinaryPrimitives.ReadInt32LittleEndian(spPdo.a_data.AsSpan(0, 4));
                    ScaleData.Weight32Tare = BinaryPrimitives.ReadInt32LittleEndian(spPdo.a_data.AsSpan(4, 4));
                    break;

                case 3: // TPDO3
                    ScaleData.WeightRaw = BinaryPrimitives.ReadInt32LittleEndian(spPdo.a_data.AsSpan(0, 4));
                    ScaleData.WeightDuration = BinaryPrimitives.ReadInt32LittleEndian(spPdo.a_data.AsSpan(4, 4));
                    break;

                case 4: // TPDO4
                    ScaleData.StatusDoserProc = (EProcStatus)spPdo.a_data[0];
                    ScaleData.StatusDoserMat = (EMatStatus)spPdo.a_data[1];
                    // Byte 2 a 3 sú voľné
                    ScaleData.StatusVyloznikProc = (EProcStatus)spPdo.a_data[4];
                    ScaleData.StatusVyloznikMat = (EMatStatus)spPdo.a_data[5];
                    // Byte 6 a 7 sú voľné
                    break;
            }
        }

        public override void ReadStatus(COP_t_EVENT_OBJ eventMsg)
        {
            Data.LastEvent = eventMsg;

            switch (eventMsg.evt_type)
            {
                case COP_k_NMT_EVT:
                    Data.NmtStatus = (ENmtStatus)eventMsg.evt_data3;
                    break;
                case COP_k_WPDO_EVT:
                    Data.WpdoError = true;
                    Data.WpdoErrorPdoNumber = eventMsg.evt_data3;
                    break;
            }

            RecStatus(EventArgs.Empty);
        }

        public override void ReadEmergency(COP_t_EMERGENCY_OBJ spEmergency)
        {
            Data.LastEmergency = spEmergency;
            RecEmergency(EventArgs.Empty);
        }

        public override string GetLastEmergencyMsg()
        {
            return $"Scale Node:{Data.LastEmergency.node_no}, Error code:{Data.LastEmergency.err_value:X04}";
        }

        public bool WaitForProcStatus(EProcStatus expectedProc, uint timeoutMs)
        {
            long startTime = Environment.TickCount64;
            while (true)
            {
                EProcStatus currentProc = ScaleData.StatusMainProc;

                // Ak je chyba na zbernici alebo STM32 hlási Error, okamžite končíme s false
                if (ScaleData.WpdoError || LowLayer.Can.GetNMTState() != ENmtStatus.NcsOPERATIONAL ||
                    currentProc == EProcStatus.Error)
                    return false;

                if (currentProc == expectedProc)
                    return true;

                if (Environment.TickCount64 - startTime > timeoutMs)
                    return false;

                Thread.Sleep(10);
            }
        }

        public bool WaitForProcAndZoneStatus(EProcStatus expectedProc, EZoneStatus expectedZone, uint timeoutMs)
        {
            long startTime = Environment.TickCount64;
            while (true)
            {
                EProcStatus currentProc = ScaleData.StatusMainProc;
                EZoneStatus currentZone = ScaleData.StatusMainZone;

                if (ScaleData.WpdoError || LowLayer.Can.GetNMTState() != ENmtStatus.NcsOPERATIONAL ||
                    currentProc == EProcStatus.Error)
                    return false;

                if (currentProc == expectedProc && currentZone == expectedZone)
                    return true;

                if (Environment.TickCount64 - startTime > timeoutMs)
                    return false;

                Thread.Sleep(10);
            }
        }

        public void WaitForInitAttained(uint timeoutMs)
        {
            long startTime = Environment.TickCount64;

            while (true)
            {
                // 1. Atomické načítanie stavov z PDO
                EProcStatus currentStatus = ScaleData.StatusMainProc;
                bool wpdoError = ScaleData.WpdoError;

                // 2. Aktívne dopytovanie reálneho NMT stavu cez LowLayer
                ENmtStatus nmtStatus = LowLayer.Can.GetNMTState();

                // 3. Fail-Fast: Kontrola chýb zbernice a NMT
                if (wpdoError)
                    throw new CDeviceException(
                        $"WaitForInitAttained Node:{NodeId}. Async WPDO Error na PDO {ScaleData.WpdoErrorPdoNumber}.",
                        0);

                if (nmtStatus != ENmtStatus.NcsOPERATIONAL)
                    throw new CDeviceException(
                        $"WaitForInitAttained Node:{NodeId}. Zariadenie stratilo stav OPERATIONAL (Aktuálny: {nmtStatus}).",
                        0);

                // 4. Fail-Fast: Kontrola chybového stavu procesu v STM32
                if (currentStatus == EProcStatus.Error)
                    throw new CDeviceException(
                        $"WaitForInitAttained Node:{NodeId}. Zariadenie hlási chybový stav (EProcStatus.Error).", 0);

                // 5. Úspešné dokončenie
                if (currentStatus == EProcStatus.Ready)
                    return;

                // 6. Kontrola pretečenia času (Timeout)
                if (Environment.TickCount64 - startTime > timeoutMs)
                    throw new CDeviceException(
                        $"WaitForInitAttained Node:{NodeId}. Timeout {timeoutMs}ms vypršal. Aktuálny stav: {currentStatus}.",
                        0);

                // 7. Uvoľnenie CPU pre OS (Makro-čakanie)
                Thread.Sleep(10);
            }
        }
    }
}