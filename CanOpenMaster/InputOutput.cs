namespace EposCmd
{
    namespace Net
    {
        namespace DeviceCmdSet
        {
            namespace Operation
            {
                public class InputOutput : CEpos4CommandGroupCO
                {
                    public InputOutput(ushort keyHandle, byte nodeId, CDataEpos4 data)
                    {
                        KeyHandle = keyHandle;
                        NodeId = nodeId;
                        BaseData = data;
                        PositionMarker = new PositionMarker(keyHandle, nodeId, data);
                        PositionCompare = new PositionCompare(keyHandle, nodeId, data);
                    }

                    public PositionCompare PositionCompare { get; }
                    public PositionMarker PositionMarker { get; }

                    public ushort GetAllDigitalInputs()
                    {
                        return Data.DigitalAllInput;
                    }

                    public ushort GetAllDigitalOutputs()
                    {
                        return Data.DigitalAllOutput;
                    }

                    public bool GetDigitalInput(EDigitalInputConfiguration digitalInput)
                    {
                        return (GetAllDigitalInputs() & (1 << (short)digitalInput)) != 0;
                    }
                    public bool GetDigitalInputNumber(short digitalInput)
                    {
                        return (GetAllDigitalInputs() & (1 << digitalInput)) != 0;
                    }

                    public short GetAnalogInput(short inputNumber)
                    {
                        switch (inputNumber)
                        {
                            case 1:
                                return Data.AnalogInput1;
                            case 2:
                                return Data.AnalogInput2;
                            default: return 0;
                        }
                    }

                    public bool IsAnalogValueGreat(short value, short inputNumber)
                    {
                        if (GetAnalogInput(inputNumber) > value) return true;
                        return false;
                    }
                    public bool IsAnalogValueGreatEq(short value, short inputNumber)
                    {
                        if (GetAnalogInput(inputNumber) >= value) return true;
                        return false;
                    }
                    public bool IsAnalogValueLess(short value, short inputNumber)
                    {
                        if (GetAnalogInput(inputNumber) < value) return true;
                        return false;
                    }
                    public bool IsAnalogValueLessEq(short value, short inputNumber)
                    {
                        if (GetAnalogInput(inputNumber) <= value) return true;
                        return false;
                    }

                    public void SetAllDigitalOutputs(UInt32 outputs)
                    {
                        //  WritedSDO(0x2078, 0x01, outputs, 2);
                        try
                        {
                            //  WritedSDO(0x6040, 0x00, Value, 2); // Set Enable
                           // var Txdata = new byte[8];
                           // Txdata = BitConverter.GetBytes(outputs);
                           // WritePDO(3, Txdata);
                            WritePDO3SetupOutput(outputs);
                        }
                        catch (CDeviceException)
                        {
                            //            throw new DeviceException($"SetAllDigitalOutputs:[{outputs:X02}] {e.ErrorMessage}", 0);
                        }
                    }

                    public void SetAnalogOutput(ushort outputNumber, ushort analogValue)
                    {
                        WritedSDO(0x207E, 0x00, analogValue, 2);
                    }

                    public void SetDigitalOutput(EDigitalOutputConfiguration digitalOutput)
                    {
                        Thread.Sleep(1);
                        UInt32 value = GetAllDigitalOutputs();
                        Thread.Sleep(1);
                        SetAllDigitalOutputs((ushort)((ushort)value | (1 << (ushort)digitalOutput)));
                    }

                    public void SetupOutput(bool value, int number)
                    {
                        switch (number)
                        {
                            case 1: { SetupOutput1(value); break; }
                            case 2: { SetupOutput2(value); break; }
                            default: throw new CDeviceException("Unknown digital output");
                        }
                    }

                    public void SetupOutput1(bool value)
                    {
                        Thread.Sleep(5);
                        //bool a = !GetDigitalOutputNumber(0);
                        bool b = !GetDigitalOutputNumber(1);

                        if (value)
                        {
                            if (b)
                            {
                                SetAllDigitalOutputs(0x30000);
                            }
                            else
                            {
                                SetAllDigitalOutputs(0x10000);
                            }
                        }
                        else
                        {
                            if (b)
                            {
                                SetAllDigitalOutputs(0x20000);
                            }
                            else
                            {
                                SetAllDigitalOutputs(0x00000);
                            }
                        }
                    }
                    public void SetupOutput2(bool value)
                    {
                        Thread.Sleep(5);
                        bool a = !GetDigitalOutputNumber(0);
                        //bool b = !GetDigitalOutputNumber(1);

                        if (value)
                        {
                            if (a)
                            {
                                SetAllDigitalOutputs(0x30000);
                            }
                            else
                            {
                                SetAllDigitalOutputs(0x20000);
                            }
                        }
                        else
                        {
                            if (a)
                            {
                                SetAllDigitalOutputs(0x10000);
                            }
                            else
                            {
                                SetAllDigitalOutputs(0x00000);
                            }
                        }
                    }

                    public void ResetDigitalOutput(EDigitalOutputConfiguration digitalOutput)
                    {
                        Thread.Sleep(5);
                        UInt32 allDigitalOutput = GetAllDigitalOutputs();
                        Thread.Sleep(5);
                        SetAllDigitalOutputs((ushort)(allDigitalOutput & ~(1 << (ushort)digitalOutput)));
                    }



                    public bool GetDigitalOutput(EDigitalOutputConfiguration digitalOutput)
                    {
                        return (GetAllDigitalOutputs() & (1 << (int)digitalOutput)) != 0;
                    }
                    public bool GetDigitalOutputNumber(int digitalOutput)
                    {
                        return (GetAllDigitalOutputs() & (1 << digitalOutput)) != 0;
                    }

                    public void SetupDigitalOutput(EDigitalOutputConfiguration digitalOutput, bool value)
                    {
                        Thread.Sleep(5);
                        if (value) SetDigitalOutput(digitalOutput);
                        else ResetDigitalOutput(digitalOutput);
                    }

                }
            }
        }
    }
}