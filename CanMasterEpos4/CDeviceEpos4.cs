using System;
using EposCmd.Net.DeviceCmdSet.Configuration;
using EposCmd.Net.DeviceCmdSet.DataRecorder;
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
            public CDeviceEpos4()
            {
            }

            public CDeviceEpos4(ushort keyHandle, byte nodeId, string name, double gear)
            {
                Name = name;
                NodeId = nodeId;
                Data = new CDataCO(nodeId, Name, gear);
                Configuration = new Configuration(keyHandle, nodeId);
                Initialization = new Initialization(keyHandle, nodeId);
                DataRecording = new DataRecorder(keyHandle, nodeId);
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
                DataRecording = new DataRecorder(keyHandle, nodeId);
                LowLayer = new LowLayer(keyHandle, nodeId, Data);
                Operation = new Operation(keyHandle, nodeId, Data);
            }

            public Configuration Configuration { get; }
            public DataRecorder DataRecording { get; }
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
                switch (Data.LastEvent.evt_data1)
                {
                    case COP_k_NMT_EVT:
                    case COP_k_DLL_EVT:
                    case COP_k_WPDO_EVT:
                        {
                            Data.Statusword = 0;
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
                var errorIPM = $"";
                var description = $"";

                if (Data.LastEmergency.err_value == 0xFF0C)
                    for (var i = 0; i < 7; i++)
                    {
                        var code = (byte)(1 << i);
                        if ((Data.IpmStatus & code) != 0) errorIPM += $"({ErrorIPMMode(code)}), ";
                    }

                return
                    $" Node:{Data.LastEmergency.node_no}, Error code:{Data.LastEmergency.err_value:X04}({ErrorCodeEpos2ToString(Data.LastEmergency.err_value)}) [{errorIPM}]";
            }

            private string ErrorCodeEpos2ToString(ushort errorCode)
            {
                string description;
                switch (errorCode)
                {
                    case 0x0000:
                        description = "No Error";
                        break;
                    case 0x1000:
                        description = "Generic Error";
                        break;
                    case 0x1090:
                        description = "Firmware incompatibility error";
                        break;
                    case 0x2310:
                        description = "Overcurrent Error";
                        break;
                    case 0x3210:
                        description = "Overvoltage Error";
                        break;
                    case 0x3220:
                        description = "Undervoltage Error";
                        break;
                    case 0x4210:
                        description = "Overtemperature Error";
                        break;
                    case 0x5113:
                        description = "Logic Supply Voltage Too Low Error";
                        break;
                    case 0x5114:
                        description = "Supply Voltage Output Stage Too Low Error";
                        break;
                    case 0x6100:
                        description = "Internal Software Error";
                        break;
                    case 0x6320:
                        description = "Software Parameter Error";
                        break;
                    case 0x7320:
                        description = "Position Sensor Error";
                        break;
                    case 0x8110:
                        description = "CAN Overrun Error(Objects lost)";
                        break;
                    case 0x8111:
                        description = "CAN Overrun Error";
                        break;
                    case 0x8120:
                        description = "CAN Passive Mode Error";
                        break;
                    case 0x8130:
                        description = "CAN Life Guard Error";
                        break;
                    case 0x8150:
                        description = "CAN Transmit COB - ID Collision Error";
                        break;
                    case 0x81FD:
                        description = "CAN Bus Off Error";
                        break;
                    case 0x81FE:
                        description = "CAN Rx Queue Overflow Error";
                        break;
                    case 0x81FF:
                        description = "CAN Tx Queue Overflow Error";
                        break;
                    case 0x8210:
                        description = "CAN PDO Length Error";
                        break;
                    case 0x8611:
                        description = "Following Error";
                        break;
                    case 0x8A80:
                        description = "Negative limit switch error";
                        break;
                    case 0x8A81:
                        description = "Positive limit switch error";
                        break;
                    case 0x8A82:
                        description = "Software positive limit error";
                        break;
                    case 0x8A88:
                        description = "STO error";
                        break;
                    case 0xFF01:
                        description = "Hall Sensor Error";
                        break;
                    case 0xFF02:
                        description = "Index Processing Error";
                        break;
                    case 0xFF03:
                        description = "Encoder Resolution Error";
                        break;
                    case 0xFF04:
                        description = "Hall Sensor not found Error";
                        break;
                    case 0xFF06:
                        description = "Negative Limit Switch Error";
                        break;
                    case 0xFF07:
                        description = "Positive Limit Switch Error";
                        break;
                    case 0xFF08:
                        description = "Hall Angle Detection Error";
                        break;
                    case 0xFF09:
                        description = "Software Position Limit Error";
                        break;
                    case 0xFF0A:
                        description = "Position Sensor Breach Error";
                        break;
                    case 0xFF0B:
                        description = "System Overloaded Error";
                        break;
                    case 0xFF0C:
                        description = "Interpolated Position Mode Error";
                        break;
                    case 0xFF0D:
                        description = "Auto Tuning Identification Error";
                        break;
                    case 0xFF0F:
                        description = "Gear Scaling Factor Error";
                        break;
                    case 0xFF10:
                        description = "Controller Gain Error";
                        break;
                    case 0xFF11:
                        description = "Main Sensor Direction Error";
                        break;
                    case 0xFF12:
                        description = "Auxiliary Sensor Direction Error";
                        break;
                    default:
                        description = "Unknown Error code";
                        break;
                }

                return description;
            }

            private string ErrorIPMMode(ushort errorCode)
            {
                string description;
                switch (errorCode)
                {
                    case 1:
                        description = "Buffer underflow warning level (0x20C4-2) is reached";
                        break;
                    case 2:
                        description = "Buffer overflow warning level (0x20C4-3) is reached";
                        break;
                    case 4:
                        description = "IPM velocity greater than profile velocity (0x6081) detected";
                        break;
                    case 8:
                        description = "IPM acceleration greater than profile acceleration (0x6083) detected";
                        break;
                    case 256:
                        description = "Buffer underflow error (trajectory abort)";
                        break;
                    case 512:
                        description = "Buffer overflow error (trajectory abort)";
                        break;
                    case 1024:
                        description = "IPM velocity greater than maximal profile velocity (0x607F) detected";
                        break;
                    case 2048:
                        description = "IPM acceleration greater than maximal profile acceleration (0x60C5) detected";
                        break;
                    case 16384:
                        description = "Access to the input buffer enabled";
                        break;
                    case 32768:
                        description = "IP mode active";
                        break;

                    default:
                        description = "Unknow";
                        break;
                }

                return description;
            }
        }
    }
}