using System;
using System.Buffers.Binary;
using System.Threading;
using static IXXAT.CANopenMasterAPI6;

namespace EposCmd.Net
{
    public class CCommandGroupCO : ErrorHandlingCO
    {
        protected CDataBaseCO BaseData;
        protected ushort KeyHandle;
        protected byte NodeId;
        private readonly byte[] _sdoTxBuffer = new byte[8];
        private readonly byte[] _sdoRxBuffer = new byte[8];

        protected void WritedSDO(ushort Index, byte Subindex, ulong Value, ushort Len)
        {
            lock (BaseData.NodeSdoLock)
            {
                short res;
                uint abortcode = 0;
                
                BinaryPrimitives.WriteUInt64LittleEndian(_sdoTxBuffer, Value);

                int retries = 0;
                var spinWait = new SpinWait();
                do
                {
                    res = COP_WriteSDO(KeyHandle, NodeId, COP_k_DEFAULT_SDO, COP_k_NO_BLOCKTRANSFER, Index,
                        Subindex, Len, _sdoTxBuffer, out abortcode);
                    
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
            lock (BaseData.NodeSdoLock)
            {
                short res;
                uint abortcode = 0;
                uint rxLen = Len;
                
                Array.Clear(_sdoRxBuffer, 0, _sdoRxBuffer.Length);

                res = COP_ReadSDO(KeyHandle, NodeId, COP_k_DEFAULT_SDO, COP_k_NO_BLOCKTRANSFER, 
                    Index, Subindex, ref rxLen, _sdoRxBuffer, out abortcode);

                if (COP_k_OK == res) 
                    return BitConverter.ToUInt64(_sdoRxBuffer, 0);
                
                var Message = $"ReadSDO Node {NodeId:d} [index:0x{Index:X04} sub:0x{Subindex:X02}]";
                if (COP_k_ABORT == res)
                    throw new CDeviceException($"{Message} ({CopAbortCodeString(abortcode)})", abortcode);
                throw new CDeviceException($"{Message} ({CopErrorString(res)})", (uint)res);
            }
        }

        protected void WritePDO(byte Pdo, byte[] TxData)
        {
            lock (BaseData.NodePdoLock)
            {
                // Využitie virtuálnej vlastnosti (pre EPOS4 to skontroluje RemoteStatus)
                if (!BaseData.IsPdoCommunicationAllowed)
                    throw new CDeviceException($"WritePDO  [Node:{NodeId:d}]  [PDO:{Pdo:d}] (Communication not allowed/Remote status off.) ", 0);

                short res = COP_WritePDO(KeyHandle, NodeId, Pdo, TxData);

                if (res != COP_k_OK)
                {
                    throw new CDeviceException($"WritePDO  [Node:{NodeId:d}]  [PDO:{Pdo:d}] ({CopErrorString(res)}) ", (uint)res);
                }
            }
        }
    }
}