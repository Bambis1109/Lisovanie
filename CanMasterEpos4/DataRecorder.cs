namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace DataRecorder
            {
                public class Data : CCommandGroupCO
                {
                    public Data(ushort keyHandle, byte nodeId)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                    }

                    public void ExportChannelDataToFile(string fileName)
                    {
                    }

                    public void ReadChannelDataVector(byte channelNumber, ref byte[] dataVector)
                    {
                    }

                    public void ReadChannelVectorSize(ref uint vectorSize)
                    {
                    }

                    public void ShowChannelDataDlg()
                    {
                    }
                }

                public class DataRecorder
                {
                    public DataRecorder(ushort keyHandle, byte nodeId)
                    {
                    }

                    public DataRecorderAdvanced Advanced { get; }
                    public Data Data { get; }
                    public Setup Setup { get; }
                    public Status Status { get; }
                }

                public class DataRecorderAdvanced : CCommandGroupCO
                {
                    public DataRecorderAdvanced(ushort keyHandle, byte nodeId)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                    }

                    public void ExtractChannelDataVector(byte channelNumber, ref byte[] dataBuffer, uint bufferSize,
                        ref byte[] dataVector, uint vectorSize, ushort vectorStartOffset, ushort maxNbOfSamples,
                        ushort nbOfRecordedSamples)
                    {
                    }

                    public void ReadDataBuffer(ref byte[] dataBuffer, uint bufferSizeToRead, ref uint bufferSizeRead,
                        ref ushort vectorStartOffset, ref ushort maxNbOfSamples, ref ushort nbOfRecordedSamples)
                    {
                    }
                }

                public class Setup : CCommandGroupCO
                {
                    public Setup(ushort keyHandle, byte nodeId)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                    }

                    public void ActivateChannel(byte channelNumber, ushort objectIndex, byte objectSubIndex,
                        byte objectSize)
                    {
                    }

                    public void DeactivateAllChannels()
                    {
                    }

                    public void DisableAllTriggers()
                    {
                    }

                    public void EnableTrigger(EDataRecorderTriggerType triggerType)
                    {
                    }

                    public void GetRecorderParameter(ref ushort samplingPeriod, ref ushort pNbOfPrecedingSamples)
                    {
                    }

                    public void SetRecorderParameter(ushort samplingPeriod, ushort nbOfPrecedingSamples)
                    {
                    }
                }

                public class Status : CCommandGroupCO
                {
                    public Status(ushort keyHandle, byte nodeId)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                    }

                    public void ForceTrigger()
                    {
                    }

                    public void IsRecorderRunning(ref bool running)
                    {
                    }

                    public void IsRecorderTriggered(ref bool triggered)
                    {
                    }

                    public void StartRecorder()
                    {
                    }

                    public void StopRecorder()
                    {
                    }
                }
            }
        }
    }
}