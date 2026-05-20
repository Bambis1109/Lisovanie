// ==========================================
// Súbor: CanOpenMaster\CScaleCommandGroupCO.cs
// ==========================================

using System;

namespace EposCmd.Net
{
    public class CScaleCommandGroupCO : CCommandGroupCO
    {
        protected CDataScale Data => (CDataScale)BaseData;

        /// <summary>
        /// Fire-and-Forget zápis povelu (Bez blokovania vlákna).
        /// STM32 prijme povel a zmení svoj stav v TPDO (napr. na Busy).
        /// </summary>
        protected void SendCommandFireAndForget(ushort index, uint command)
        {
            // Zápis povelu do subindexu 0x01 (4 byty)
            WritedSDO(index, 0x01, command, 4);
        }
    }
}