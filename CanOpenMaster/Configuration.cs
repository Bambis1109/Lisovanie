namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Configuration
            {
                public class Compatibility : CEpos4CommandGroupCO
                {
                    public Compatibility(ushort keyHandle, byte nodeId)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                    }

                    public void GetEncoderParameter(ref ushort counts, ref ESensorType positionSensorType)
                    {
                    }

                    public void GetMotorParameter(ref EMotorType motorType, ref ushort continuousCurrent,
                        ref ushort peakCurrent, ref byte polePair, ref ushort thermalTimeConstant)
                    {
                    }

                    public void SetEncoderParameter(ushort counts, ESensorType positionSensorType)
                    {
                    }

                    public void SetMotorParameter(EMotorType motorType, ushort continuousCurrent, ushort peakCurrent,
                        byte polePair, ushort thermalTimeConstant)
                    {
                    }
                }

                public class Configuration : CEpos4CommandGroupCO
                {
                    public void SetGear(double gear)
                    {
                        Data.Gear = gear;
                    }

                    public Configuration(ushort keyHandle, byte nodeId)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;

                    }



                    public class ConfigurationAdvanced : CEpos4CommandGroupCO
                    {
                        public ConfigurationAdvanced(ushort keyHandle, byte nodeId)
                        {
                            KeyHandle = keyHandle;
                            NodeId = nodeId;
                            InputOutput = new InputOutput(keyHandle, nodeId);
                            Motor = new Motor(keyHandle, nodeId);
                            Safety = new Safety(keyHandle, nodeId);
                        }

                        public Compatibility Compatibility { get; }
                        public InputOutput InputOutput { get; }
                        public Motor Motor { get; }
                        public Safety Safety { get; }
                    }

                    public class InputOutput : CEpos4CommandGroupCO
                    {
                        public InputOutput(ushort keyHandle, byte nodeId)
                        {
                            KeyHandle = keyHandle;
                            NodeId = nodeId;
                        }

                        public void AnalogInputConfiguration(ushort analogInputNb,
                            EAnalogInputConfiguration configuration,
                            int executionMask)
                        {
                        }

                        public void DigitalInputConfiguration(ushort digitalInputNb,
                            EDigitalInputConfiguration configuration, int mask, int polarity, int executionMask)
                        {
                        }

                        public void DigitalOutputConfiguration(ushort digitalOutputNb,
                            EDigitalOutputConfiguration configuration, bool state, bool mask, bool polarity)
                        {
                            //Set configuration for output port
                            WritedSDO(0x2079, (byte)digitalOutputNb, (ushort)configuration, 2);

                            ushort digitalOutpuState = (ushort)ReadSdo(0x2078, 0x01, 2);
                            WritedSDO(0x2078, 0x01, ModificationBit(digitalOutpuState, (ushort)configuration, state),
                                2);

                            ushort digitalOutpuMask = (ushort)ReadSdo(0x2078, 0x02, 2);
                            WritedSDO(0x2078, 0x02, ModificationBit(digitalOutpuMask, (ushort)configuration, mask), 2);

                            ushort digitalOutpuPolarity = (ushort)ReadSdo(0x2078, 0x03, 2);
                            WritedSDO(0x2078, 0x03,
                                ModificationBit(digitalOutpuPolarity, (ushort)configuration, polarity),
                                2);
                        }


                        public ushort ModificationBit(ushort value, ushort bitNumber, bool bit)
                        {
                            if (!bit)
                            {
                                return (ushort)(value & ~(1 << bitNumber));
                            }
                            else
                            {
                                return (ushort)(value | (1 << bitNumber));
                            }
                        }
                    }

                    public class Motor : CEpos4CommandGroupCO
                    {
                        public Motor(ushort keyHandle, byte nodeId)
                        {
                            KeyHandle = keyHandle;
                            NodeId = nodeId;
                        }

                        public void SetNominalCurrent(UInt32 nominalCurent)
                        {
                            WritedSDO(0x3001, 0x01, (UInt32)nominalCurent, 4);
                        }

                        public void SetOutputCurrentLimit(UInt32 outputCurrentLimit)
                        {
                            WritedSDO(0x3001, 0x02, (UInt32)outputCurrentLimit, 4);
                        }

                        public void SetCurrentLimits(UInt32 nominalCurrent, UInt32 outputCurrentLimit)
                        {
                            SetNominalCurrent(nominalCurrent);
                            SetOutputCurrentLimit(outputCurrentLimit);
                        }


                        public UInt32 GetNominalCurrent()
                        {
                            return (UInt32)ReadSdo(0x3001, 0x01, 4);
                        }

                        public UInt32 GetOutputCurrentLimit()
                        {
                            return (UInt32)ReadSdo(0x3001, 0x02, 4);
                        }


                        public void GetDcMotorParameter(ref ushort nominalCurrent, ref ushort maxOutputCurrent,
                            ref ushort thermalTimeConstant)
                        {
                        }

                        public void GetEcMotorParameter(ref ushort nominalCurrent, ref ushort maxOutputCurrent,
                            ref ushort thermalTimeConstant, ref byte nbOfPolePairs)
                        {
                        }

                        public void GetMotorType(ref EMotorType motorType)
                        {
                        }

                        public void SetDcMotorParameter(ushort nominalCurrent, ushort maxOutputCurrent,
                            ushort thermalTimeConstant)
                        {
                        }

                        public void SetEcMotorParameter(ushort nominalCurrent, ushort maxOutputCurrent,
                            ushort thermalTimeConstant, byte nbOfPolePairs)
                        {
                        }

                        public void SetMotorType(EMotorType motorType)
                        {
                        }
                    }


                    public class Safety : CCommandGroupCO
                    {
                        public Safety(ushort keyHandle, byte nodeId)
                        {
                            KeyHandle = keyHandle;
                            NodeId = nodeId;
                        }

                        public uint GetMaxAcceleration()
                        {
                            return 0;
                        }

                        public uint GetMaxFollowingError()
                        {
                            return (uint)ReadSdo(0x6065, 0x00, 4);
                            ;
                        }

                        public uint GetMaxProfileVelocity()
                        {
                            return 0;
                        }

                        public void SetMaxAcceleration(uint maxAcceleration)
                        {
                        }

                        public void SetMaxFollowingError(uint maxFollowingError)
                        {
                            WritedSDO(0x6065, 0x00, maxFollowingError, 4);
                        }

                        public void SetMaxProfileVelocity(uint maxProfileVelocity)
                        {
                        }
                    }
                }
            }
        }
    }
}