namespace Lisovanie.Models;

public enum EnStatusPlc
{
    Ready,
    Running,
    StepMode,
    Pause,
    NotInit,
    WaitingToFinish,
    Error,
    Initializing,
    WaitForStoping
}

public enum EnIxxatState
{
    Disconnected,
    Connecting,
    Connected,
    BusFault
}

public enum EnStatusConnection
{
    WaitToConnect,
    Connected,
    Disconnect
}

public enum EnStatusCycle
{
    WaitForStart,
    WaitForInit,
    Moving,
    Inspecting,
    WaitingForLock,
    WaitForStep,
    WaitForPause,
    Error,
    Suspended
}

public enum EnModePlc
{
    Auto,
    Manual
}

/// <summary>Režim výroby daný receptom. Single = jeden materiál, Multi = viacvrstvová tableta.</summary>
public enum EnModeVyroby
{
    Single,
    Multi
}

/// <summary>
/// Metóda lisovania daná receptom.
/// Sila = lisuje sa na požadovanú silu, hrúbka je len dolná zarážka.
/// Vzdialenost = lisuje sa na požadovanú hrúbku, sila je horný limit.
/// </summary>
public enum EnMetodaLisovania
{
    Sila,
    Vzdialenost
}

public enum EnThreeState
{
    EnTrue,
    EnFalse,
    EnNone
}

public enum EnJawsHolding
{
    Empty,
    HoldType1,
    HoldType2,
    HoldType3
}

public enum EnGlydCheckStatus
{
    Present,
    Missing,
    Wrong
}

public enum EnZoneOwner
{
    Main,
    Free,
    Scale,
    Press,
    Manipulator
}

public enum EnZoneStatus
{
    Unknown, // Nedefinovaný stav po štarte stroja
    InputEmpty, // Šachta je prázdna, čaká na váhu
    InputFull, // Dávka je nasypaná, čaká na lis
    OutputProced, // Prebieha lisovanie (ochrana proti výpadku napájania/havárii)
    OutputFullOk, // Úspešne zlisované, čaká na manipulátor (dobrý kus)
    OutputFullNok, // Zlisované s chybou, čaká na manipulátor (vyhodiť do RED ZONE)
    OutputEmpty, // Odobraty vyrobok
    StackFull // zsobnik plny koniec vyroby
}