using System;
using System.Threading;

namespace EposCmd.Net
{
    public class CScaleCommandGroupCO : CCommandGroupCO
    {
        protected CDataScale Data => (CDataScale)BaseData;

        /// <summary>
        /// Univerzálna metóda na zápis povelu s potvrdením (Handshake)
        /// </summary>
        protected void ExecuteCommandWithAck(ushort index, uint command, int timeoutMs = 2000)
        {
            // 1. Zápis povelu do subindexu 0x01
            WritedSDO(index, 0x01, command, 4);

            // Ak posielame len CLEAR (0x00000000), nečakáme na ACK=1
            if (command == 0) return;

            // 2. Čakanie na ACK = 1 (Byte 3 v stavovom slove na subindexe 0x02)
            bool ackSet = SpinWait.SpinUntil(() => 
            {
                uint status = (uint)ReadSdo(index, 0x02, 4);
                return (status >> 24) == 1; // Posun o 24 bitov doprava získa Byte 3 (ACK)
            }, timeoutMs);

            if (!ackSet) 
                throw new CDeviceException($"Scale Node:{NodeId}. Timeout waiting for ACK=1 on index 0x{index:X4}", 0);

            // 3. Zápis CLEAR (0x00000000) do subindexu 0x01
            WritedSDO(index, 0x01, 0, 4);

            // 4. Čakanie na ACK = 0
            bool ackReset = SpinWait.SpinUntil(() => 
            {
                uint status = (uint)ReadSdo(index, 0x02, 4);
                return (status >> 24) == 0;
            }, timeoutMs);

            if (!ackReset) 
                throw new CDeviceException($"Scale Node:{NodeId}. Timeout waiting for ACK=0 on index 0x{index:X4}", 0);
        }
    }
}