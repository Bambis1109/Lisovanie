# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build

The system dotnet SDK is .NET 9 — use the Rider-bundled .NET 10 SDK for all builds:

```powershell
$dotnet = "C:\Program Files\JetBrains\JetBrains Rider 2026.1.0.1\lib\ReSharperHost\windows-x64\dotnet\dotnet.exe"
& $dotnet build MojaPrvaAvalonia\MojaPrvaAvalonia.csproj
```

The app must be stopped in Rider before building from CLI — `CanOpenMaster.dll` stays locked while the app is running.

## Project Structure

Two projects in `MojaPrvaAvalonia.sln`:
- **`MojaPrvaAvalonia/`** — WinExe Avalonia UI application (.NET 10)
- **`CanOpenMaster/`** — CANopen communication class library (.NET 10), depends on native Ixxat DLLs (`XatCOP_VCI3-64.dll`)

## Architecture

Three-layer motion control system:

```
UI (Avalonia MVVM)  →  PLC Logic (State Machines)  →  Hardware (CANopen / Ixxat)
```

### PLC Layer (`Models/`)

`CPlc` is the abstract base for all controllers. It runs a dedicated OS thread (`ThreadPriority.AboveNormal`) executing a step-based state machine via `RunStep(int step)`. Steps return the next step number; returning `0` parks the program.

```
Init sequence:  step 1 → 10 → 20 → ... → 99  (SetStatusReady)
Main sequence:  step 100 → 110 → ... → 0      (parks)
```

Three concrete controllers:
- **`CControlManipulator`** (`CPlc → CPlcEpos`) — 4 EPOS4 motors, coaxial delta robot, jaw gripper, matrix-based stacking
- **`CControlLis`** (`CPlc → CPlcEpos`) — 3 EPOS4 motors, hydraulic press with force control
- **`CControlScales`** (`CPlc → CPlcScale`) — 2 CANopen scales, dual-scale material dispensing

**`CMainProgram`** creates and coordinates all three controllers. It owns two CAN buses: `DeviceManagerCO` (motors) and `DeviceManagerScale` (scales). The `Connect()` method wires up devices and assigns NodeIDs from loaded parameters before `ConnectAsync()` is called on each PLC.

### Interlocking Layer (`IL` / `CMutexZone`)

Prevents race conditions between the three independent PLC threads. Zone ownership passes:  
`Scale → ZonePress → Press → ZonePress → Manipulator`

`TryLock(owner, status)` acquires zone ownership atomically; `Release(owner, newStatus)` hands it off.

### Hardware Layer (`CanOpenMaster/`)

- **`CDeviceEpos4`** — Maxon EPOS4 motor abstraction (PDO real-time data, SDO config, NMT state)
- **`CDeviceScale`** — STM32-based scale over CANopen
- **`CDeviceManagerCO`** — Ixxat CANopen master bus manager; routes PDOs via `System.Threading.Channels`

### Parameters Persistence

`CPlc.SaveParametersToFile<T>` / `LoadParametersFromFile<T>` serialize to JSON in `AppDomain.CurrentDomain.BaseDirectory` (i.e. `bin\Debug\net10.0\`). Reflection copies all writable properties onto the existing instance. Missing file = keep defaults.

| Controller | File | Parameters class |
|---|---|---|
| Manipulator | `ParametersDelta.json` | `CParameters` |
| Lis | `ParametersLis.json` | `CParametersLis` |
| Scales | `ParametersScale.json` | `CParametersScale` |

### UI

Avalonia 12 with Fluent Dark theme. MVVM via `CommunityToolkit.Mvvm` (`[ObservableProperty]`, `[RelayCommand]`). ViewModels poll hardware data via `Task.Run` refresh loops; all UI updates go through `Dispatcher.UIThread`.

Setup/parameter windows follow the pattern: `frm*.axaml` opens as a non-modal child window, guarded by a `private frm*? _window` null-check to prevent duplicates.

## Coding Rules (from GEMINI.md)

1. Use modern C# — pattern matching, `async/await`, nullable reference types.
2. Log exclusively with Serilog: `Log.Information`, `Log.Error`, `Log.Fatal`. Always add `.ForContext("Name", Name)` in PLC classes.
3. Respond in Slovak, be technically precise and concise.
4. When showing code changes, show only the changed methods — not entire files.

## CANopen / EPOS4 Rules (from `.gemini/prompts/sys_can.md`)

1. **Handshake timeouts:** Never wait for ACK (Statusword Bit 12) in an infinite loop — always use a timeout (default 100 ms).
2. **Edge-triggering (PPM):** Clear New Setpoint (Controlword Bit 4) after EPOS acknowledges — EPOS4 reacts to the rising edge; leaving it set blocks the next move.
3. **PDO COB-ID:** `Base + NodeID` (e.g. RxPDO1 for Node 14 → `0x200 + 0x0E = 0x20E`).
4. **Safety checks:** Before and after every move verify Bit 3 (Fault) and Bit 13 (Following Error) in Statusword.
