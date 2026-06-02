using System;

namespace EposCmd.Net.DeviceScaleSet
{
    // --- STAVY (Status Word) ---
    public enum EProcStatus : byte
    {
        NoInit = 0x00,
        Ready = 0x01,
        Busy = 0x02,
        Error = 0x03,
        NoMaterial = 0x04
    }

    public enum EMatStatus : byte
    {
        NoSt = 0x00,
        Full = 0x01,
        Empty = 0x02
    }

    public enum EZoneStatus : byte
    {
        NoZone = 0x00,
        Free = 0x01,
        Occupied = 0x02
    }

    // --- POVELY (Control Word) ---
    public enum EMasterCommand : uint
    {
        Clear = 0x00000000,
        Init = 0x00000101,
        Produkcia = 0x00000201,
        Next = 0x00000003,
        Continue = 0x00000004,
        Stop = 0x00000002
    }

    public enum EDoserCommand : uint
    {
        Clear = 0x00000000,
        Init = 0x00000101,
        Tune = 0x00000201,
        Prod = 0x00000301,
        Vyklop = 0x00000401,
        Stop = 0x00000002
    }

    public enum EBoomCommand : uint
    {
        Clear = 0x00000000,
        Init = 0x00000101,
        Vysun1 = 0x00000201,
        Vysun2 = 0x00000301,
        Vyloz1 = 0x00000401,
        Vyloz2 = 0x00000501,
        Vysyp = 0x00000601,
        Zasun = 0x00000701
    }

    public enum ELockCommand : uint
    {
        Clear = 0x00000000,
        Init = 0x00000101,
        Odomkni = 0x00000201,
        Zamkni = 0x00000301,
        VysypVlavo = 0x00000401,
        VysypVpravo = 0x00000501,
        Kalibruj = 0x00000601
    }

    public enum EScaleCommand : uint
    {
        Clear = 0x00000000,
        Init = 0x00000101,
        KalibrujMin = 0x00000201,
        KalibrujMax = 0x00000301,
        Tara = 0x00000401
    }

    public enum ESystemCommand : uint
    {
        Clear = 0x00000000,
        Save = 0x00000101,
        Load = 0x00000201,
        Restart = 0x00000301
    }
}