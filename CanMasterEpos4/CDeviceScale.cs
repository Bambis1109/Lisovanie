using System;
using EposCmd.Net.DeviceCmdSet.LowLayer;
using EposCmd.Net.DeviceCmdSet.Operation;
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
            
            Operation = new ScaleOperation(keyHandle, nodeId, ScaleData);
        }

        // Spracovanie prichádzajúcich PDO z STM32 (z pohľadu mastra sú to RxPDO)
        public override LowLayer LowLayer => null;

        public override void ReadPdo(COP_t_RX_PDO spPdo)
        {
            RecPdo(EventArgs.Empty);
            
            switch (spPdo.pdo_no)
            {
                case 1: // TPDO1 z STM32 (Napr. Digitálne vstupy a stavové slovo)
                    ScaleData.DigitalInputs = BitConverter.ToUInt32(spPdo.a_data, 0);
                    ScaleData.VaStatus = (EVaStatus)spPdo.a_data[4];
                    break;

                case 2: // TPDO2 z STM32 (Napr. Aktuálna váha - Float/Double alebo Int32)
                    // Predpokladajme, že váha chodí ako 32-bitový integer (napr. v gramoch)
                    int rawWeight = BitConverter.ToInt32(spPdo.a_data, 0);
                    ScaleData.VaWeightInter = rawWeight; // Prípadne prepočet na double
                    break;

                case 3: // TPDO3 z STM32
                    // Doplň podľa tvojho mapovania v STM32
                    break;

                case 4: // TPDO4 z STM32
                    // Doplň podľa tvojho mapovania v STM32
                    break;
            }
        }

        public override void ReadStatus(COP_t_EVENT_OBJ eventMsg)
        {
            Data.LastEvent = eventMsg;

            switch (eventMsg.evt_type)
            {
                case COP_k_NMT_EVT:
                    // Tu môžeš spracovať zmenu NMT stavu (napr. STM32 prešlo do Pre-Op)
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
            // Tu môžeš neskôr pridať slovník chýb špecifický pre tvoje STM32
            return $"Scale Node:{Data.LastEmergency.node_no}, Error code:{Data.LastEmergency.err_value:X04}";
        }
    }
}