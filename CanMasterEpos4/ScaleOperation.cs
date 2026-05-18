using System;
using System.Buffers.Binary;

namespace EposCmd.Net.DeviceCmdSet.Operation
{
    public class ScaleOperation : CScaleCommandGroupCO
    {
        public ScaleOperation(ushort keyHandle, byte nodeId, CDataScale data)
        {
            KeyHandle = keyHandle;
            NodeId = nodeId;
            BaseData = data;
        }

        // Príklad: Zápis do RPDO1 (napríklad nastavenie digitálnych výstupov)
        public void SetDigitalOutputs(uint outputs)
        {
            lock (Data.NodePdoLock)
            {
                // Zápis 4 bajtov do pripraveného buffra
                BinaryPrimitives.WriteUInt32LittleEndian(Data.TxdataPDO1.AsSpan(0, 4), outputs);
                
                // Odošleme PDO1 (z pohľadu mastra je to TxPDO1, STM32 to prijme ako RPDO1)
                WritePDO(1, Data.TxdataPDO1);
            }
        }

        // Príklad: Odoslanie príkazu na Tarovanie (napr. cez RPDO2)
        public void TareScale()
        {
            lock (Data.NodePdoLock)
            {
                // Povedzme, že v RPDO2 posielame command byte na nultom bajte (0x01 = Tare)
                Data.TxdataPDO2[0] = 0x01; 
                WritePDO(2, Data.TxdataPDO2);
            }
        }

        // Príklad: Zápis SDO (ak by si chcel neskôr konfigurovať filtre)
        public void SetFilterConstant(ushort filterValue)
        {
            // Predpokladajme, že filter je na indexe 0x2000, subindex 0x01
            WritedSDO(0x2000, 0x01, filterValue, 2);
        }
    }
}