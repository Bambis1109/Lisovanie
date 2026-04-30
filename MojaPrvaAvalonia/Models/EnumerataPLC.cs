namespace MojaPrvaAvalonia.Models;

public enum EnStatusPlc { Ready, Running, StepMode, Pause, NotInit, WaitingToFinish, Error, Initializing, WaitForStoping }
public enum EnStatusConnection { WaitToConnect, Connected, Disconnect }
public enum EnStatusCycle { WaitForStart, WaitForInit, Moving, Inspecting, WaitingForLock, WaitForStep, WaitForPause, Error }
public enum EnModePlc { Auto, Manual }
public enum EnThreeState { EnTrue, EnFalse, EnNone }
public enum EnJawsHolding { Empty, HoldType1, HoldType2, HoldType3 }
public enum EnGlydCheckStatus { Present, Missing, Wrong }