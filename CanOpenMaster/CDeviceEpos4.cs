using System;
using EposCmd.Net.DeviceCmdSet.Configuration;
using EposCmd.Net.DeviceCmdSet.Initialization;
using EposCmd.Net.DeviceCmdSet.LowLayer;
using EposCmd.Net.DeviceCmdSet.Operation;
using static IXXAT.CANopenMasterAPI6;

namespace EposCmd.Net
{
    public class CDeviceEpos4 : CDeviceCO
    {
        public CDataEpos4 EposData => (CDataEpos4)Data;

        public CDeviceEpos4(ushort keyHandle, byte nodeId, string name, double gear)
        {
            Name = name;
            NodeId = nodeId;
            Data = new CDataEpos4(nodeId, Name, gear);
            
            // ZMENA: Predávame EposData do všetkých modulov
            Configuration = new Configuration(keyHandle, nodeId);
            Initialization = new Initialization(keyHandle, nodeId);
            LowLayer = new LowLayer(keyHandle, nodeId, EposData);
            Operation = new Operation(keyHandle, nodeId, EposData);
        }

        public CDeviceEpos4(ushort keyHandle, byte nodeId, string name, double gear, double pulse)
        {
            Name = name;
            NodeId = nodeId;
            Data = new CDataEpos4(nodeId, Name, gear, pulse);
            
            // ZMENA: Predávame EposData do všetkých modulov
            Configuration = new Configuration(keyHandle, nodeId);
            Initialization = new Initialization(keyHandle, nodeId);
            LowLayer = new LowLayer(keyHandle, nodeId, EposData);
            Operation = new Operation(keyHandle, nodeId, EposData);
        }

        public Configuration Configuration { get; }
        public Initialization Initialization { get; }
        public override LowLayer LowLayer { get; }
        public Operation Operation { get; }

        public override void ReadPdo(COP_t_RX_PDO spPdo)
        {
            RecPdo(EventArgs.Empty);
            switch (spPdo.pdo_no)
            {
                case 1:
                    EposData.Statusword = BitConverter.ToUInt16(spPdo.a_data, 0);
                    EposData.ModeOfOperationDisplay = (EOperationMode)(sbyte)spPdo.a_data[2];
                    EposData.DigitalAllInput = BitConverter.ToUInt16(spPdo.a_data, 3);
                    EposData.DigitalAllOutput = BitConverter.ToUInt16(spPdo.a_data, 5);
                    break;
                case 2:
                    EposData.VelocityActual = BitConverter.ToInt32(spPdo.a_data, 0);
                    EposData.CurrentActualAveragePercentage = (double)(BitConverter.ToInt16(spPdo.a_data, 4)) / 10;
                    EposData.AnalogInput1 = BitConverter.ToInt16(spPdo.a_data, 6);
                    break;
                case 3:
                    EposData.PositionActual = BitConverter.ToInt32(spPdo.a_data, 0);
                    EposData.PositionActualSensor2 = BitConverter.ToInt32(spPdo.a_data, 4);
                    break;
                case 4:
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
                case COP_k_DLL_EVT:
                    EposData.Statusword = 0;
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
            return $" Node:{Data.LastEmergency.node_no}, Error code:{Data.LastEmergency.err_value:X04}({Operation.DeviceErrorHandling.GetErrorDescription(Data.LastEmergency.err_value)}) ";
        }
    }
}