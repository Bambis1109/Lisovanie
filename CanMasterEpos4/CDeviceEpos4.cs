using EposCmd.Net.DeviceCmdSet.Configuration;
using EposCmd.Net.DeviceCmdSet.Initialization;
using EposCmd.Net.DeviceCmdSet.LowLayer;
using EposCmd.Net.DeviceCmdSet.Operation;
using static IXXAT.CANopenMasterAPI6;

namespace EposCmd
{
    namespace Net
    {
        public class CDeviceEpos4 : CDeviceCO
        {
            public CDeviceEpos4(ushort keyHandle, byte nodeId, string name, double gear)
            {
                Name = name;
                NodeId = nodeId;
                Data = new CDataCO(nodeId, Name, gear);
                Configuration = new Configuration(keyHandle, nodeId);
                Initialization = new Initialization(keyHandle, nodeId);
                LowLayer = new LowLayer(keyHandle, nodeId, Data);
                Operation = new Operation(keyHandle, nodeId, Data);
            }

            public CDeviceEpos4(ushort keyHandle, byte nodeId, string name, double gear, double pulse)
            {
                Name = name;
                NodeId = nodeId;
                Data = new CDataCO(nodeId, Name, gear, pulse);
                Configuration = new Configuration(keyHandle, nodeId);
                Initialization = new Initialization(keyHandle, nodeId);
                LowLayer = new LowLayer(keyHandle, nodeId, Data);
                Operation = new Operation(keyHandle, nodeId, Data);
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
                    {
                        Data.Statusword = BitConverter.ToUInt16(spPdo.a_data, 0);
                        Data.ModeOfOperationDisplay = (EOperationMode)(sbyte)spPdo.a_data[2];
                        Data.DigitalAllInput = BitConverter.ToUInt16(spPdo.a_data, 3);
                        Data.DigitalAllOutput = BitConverter.ToUInt16(spPdo.a_data, 5);
                    }
                        break;
                    case 2:
                    {
                        Data.VelocityActual = BitConverter.ToInt32(spPdo.a_data, 0);
                        Data.CurrentActualAveragePercentage = (double)(BitConverter.ToInt16(spPdo.a_data, 4)) / 10;
                        Data.AnalogInput1 = BitConverter.ToInt16(spPdo.a_data, 6);
                    }
                        break;
                    case 3:
                    {
                        Data.PositionActual = BitConverter.ToInt32(spPdo.a_data, 0);
                        Data.PositionActualSensor2 = BitConverter.ToInt32(spPdo.a_data, 4);
                        // Data.CurrentActualAverage = BitConverter.ToInt32(spPdo.a_data, 4);
                    }
                        break;
                    case 4:
                    {
                        // Data.AnalogInput2 = BitConverter.ToInt16(spPdo.a_data, 6);
                    }
                        break;
                    default:
                        break;
                }
            }
          public override void ReadStatus(COP_t_EVENT_OBJ eventMsg)
            {
                Data.LastEvent = eventMsg;

                switch (eventMsg.evt_type)
                {
                    case COP_k_NMT_EVT:
                    case COP_k_DLL_EVT:
                    {
                        Data.Statusword = 0;
                    }
                        break;
                    case COP_k_WPDO_EVT:
                    {
                        // Zaznamenanie asynchrónnej chyby zápisu PDO
                        Data.WpdoError = true;
                        Data.WpdoErrorPdoNumber = eventMsg.evt_data3;
                    }
                        break;
                    case COP_k_QUEUE_OVRUN_EVT:
                    case COP_k_FLY_EVT:
                    {
                    }
                        break;
                    default:
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
                if (Data.LastEmergency.err_value == 0xFF0C)
                    for (var i = 0; i < 7; i++)
                    {
                        var code = (byte)(1 << i);
                    }

                return
                    $" Node:{Data.LastEmergency.node_no}, Error code:{Data.LastEmergency.err_value:X04}({Operation.DeviceErrorHandling.GetErrorDescription(Data.LastEmergency.err_value)}) ";
            }
        }
    }
}