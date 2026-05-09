using IXXAT;
using System;
using System.Threading;

namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace LowLayer
            {
                public class LowLayer
                {
                    public LowLayer(ushort keyHandle, byte nodeId, CDataCO data)
                    {
                        Can = new CanLayer(keyHandle, nodeId, data);
                    }

                    public CanLayer Can { get; }
                }

                public class CanLayer : CCommandGroupCO
                {
                    public CanLayer(ushort keyHandle, byte nodeId, CDataCO data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        Data = data;
                    }




                    public void SetRegister(ushort Index, byte Subindex, ulong Value)
                    {
                        WritedSDO(Index, Subindex, Value, 4); 

                    }
                    public void SetRegister32(ushort Index, byte Subindex, byte Command, byte Task)
                    {
                        // poskladanie 32-bitovej hodnoty:
                        // bit[7..0]   = Command
                        // bit[15..8]  = Task
                        // bit[31..16] = 0
                        uint value = ((uint)Task << 8) | (uint)(Command & 0xFF);
                        WritedSDO(Index, Subindex, value, 4); 

                    }
                    public UInt32 GetRegister(ushort Index, byte Subindex)
                    {
                        return (UInt32)ReadSdo(Index, Subindex, 4); 

                    }

                    public void ReadCanFrame(ushort cobId, ushort length, byte[] data, uint timeout)
                    {
                    }

                    public void RequestCanFrame(ushort cobId, ushort length, byte[] data)
                    {
                    }

                    public void SendCanFrame(ushort cobId, ushort length, byte[] data)
                    {
                    }

                    public ENmtStatus GetNMTState()
                    {
                        ushort node_state = 0;
                        var res = CANopenMasterAPI6.COP_GetNodeState(KeyHandle, NodeId, out node_state);
                        if (CANopenMasterAPI6.COP_k_OK == res) return (ENmtStatus)node_state;

                        var Message = string.Format(" - Remote Node {0:d}: Abort ", NodeId) +
                                      CANopenMasterAPI6.CopErrorString(res);
                        throw new CDeviceException(Message, (uint)res);
                    }

                    public void Sync(ESync sync)
                    {
                        short res = 0;
                        switch (sync)
                        {
                            case ESync.NcsEnable:
                                res = CANopenMasterAPI6.COP_EnableSync(KeyHandle, CANopenMasterAPI6.COP_k_SINGLE_LINE);
                                break;
                            case ESync.NcsDisable:
                                res = CANopenMasterAPI6.COP_DisableSync(KeyHandle, CANopenMasterAPI6.COP_k_SINGLE_LINE);
                                break;
                            case ESync.NcsOneSync:
                                res = CANopenMasterAPI6.COP_EnableSync(KeyHandle, CANopenMasterAPI6.COP_k_SINGLE_LINE);
                                Thread.Sleep(10);

                                res = CANopenMasterAPI6.COP_DisableSync(KeyHandle, CANopenMasterAPI6.COP_k_SINGLE_LINE);

                                break;
                            default:
                                break;
                        }

                        if (CANopenMasterAPI6.COP_k_OK != res)
                        {
                            var Message = sync + " - Remote Nodes : Abort " + CANopenMasterAPI6.CopErrorString(res);
                            throw new CDeviceException(Message, (uint)res);
                        }
                    }

                    public void SendNmtService(ECommandSpecifier commandSpecifier)
                    {
                        short res = 0;
                        switch (commandSpecifier)
                        {
                            case ECommandSpecifier.NcsStartRemoteNode:
                                {
                                    res = CANopenMasterAPI6.COP_StartNode(KeyHandle, NodeId);
                                }
                                break;
                            case ECommandSpecifier.NcsStopRemoteNode:
                                {
                                    res = CANopenMasterAPI6.COP_StopNode(KeyHandle, NodeId);
                                }
                                break;
                            case ECommandSpecifier.NcsEnterPreOperational:
                                {
                                    res = CANopenMasterAPI6.COP_EnterPreOperational(KeyHandle, NodeId);
                                }
                                break;
                            case ECommandSpecifier.NcsResetNode:
                                {
                                    res = CANopenMasterAPI6.COP_ResetNode(KeyHandle, NodeId);
                                }
                                break;
                            case ECommandSpecifier.NcsResetCommunication:
                                {
                                    res = CANopenMasterAPI6.COP_ResetComm(KeyHandle, NodeId);
                                }
                                break;
                            default:
                                break;
                        }

                        Data.Statusword = 0;
                        Data.ModeOfOperationDisplay = 0;
                        if (CANopenMasterAPI6.COP_k_OK != res)
                        {
                            var Message = commandSpecifier + string.Format(" - Remote Node {0:d}: Abort ", NodeId) +
                                          CANopenMasterAPI6.CopErrorString(res);
                            throw new CDeviceException(Message, (uint)res);
                        }
                    }
                }
            }
        }
    }
}