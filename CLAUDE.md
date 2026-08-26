# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build

The system dotnet SDK is .NET 9 — use the Rider-bundled .NET 10 SDK for all builds:

```powershell
$dotnet = "C:\Program Files\JetBrains\JetBrains Rider 2026.1.1\lib\ReSharperHost\windows-x64\dotnet\dotnet.exe"
& $dotnet build Lisovanie\Lisovanie.csproj
```

The app must be stopped in Rider before building from CLI — `CanOpenMaster.dll` stays locked while the app is running.

## Project Structure

Two projects in `Lisovanie.sln`:
- **`Lisovanie/`** — WinExe Avalonia UI application (.NET 10)
- **`CanOpenMaster/`** — CANopen communication class library (.NET 10), depends on native Ixxat DLLs (`XatCOP_VCI3-64.dll`, `XatCOP60-64.dll`)

## Architecture

Three-layer motion control system:

```
UI (Avalonia MVVM)  →  PLC Logic (State Machines)  →  Hardware (CANopen / Ixxat)
```

### PLC Layer (`Lisovanie/Models/`)

`CPlc` is the abstract base for all controllers. It runs a dedicated OS thread (`ThreadPriority.AboveNormal`) executing a step-based state machine via `RunStep(int step)`. Steps return the next step number; returning `0` parks the program.

```
Init sequence:  step 1 → 10 → 20 → ... → 99  (SetStatusReady)
Main sequence:  step 100 → 110 → ... → 0      (parks)
```

Three concrete controllers:
- **`CControlManipulator`** (`CPlc → CPlcEpos`) — 4 EPOS4 motors, coaxial delta robot, jaw gripper, matrix-based stacking
- **`CControlLis`** (`CPlc → CPlcEpos`) — 3 EPOS4 motors, hydraulic press with force control; result stored in `CProduktLis` (Sila, Vyska, EnProduktLis status)
- **`CControlScales`** (`CPlc → CPlcScale`) — 3 CANopen scales (dávkovače), material dispensing

**`CMainProgram`** creates and coordinates all three controllers. It owns two CAN buses: `DeviceManagerCO` (motors) and `DeviceManagerScale` (scales). The `Connect()` method wires up devices and assigns NodeIDs from loaded parameters before `ConnectAsync()` is called on each PLC.

### Interlocking Layer (`IL` / `CMutexZone`)

`IL` is a static class in the `Lisovanie.Net` namespace (`Models/IL.cs`). It prevents race conditions between the three independent PLC threads. Zone ownership passes:  
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

Those flat files are **legacy** — they are only read once by `CRecipeManager.MigrateIfNeeded()`. The live mechanism is the three-layer recipe system (`CRecipeManager`): `Parameters/Machine.json` (stroj) → `Parameters/Forms/*.json` (forma, kalibrácia) → `Parameters/Recipes/*.json` (výrobok). `Apply()` maps all three into the running PLC objects by hand-written assignment; `SaveAll()` writes all three back at once. Recipe schema changes need `CurrentRecipeVersion` bumped plus a clause in `UpgradeRecipe`.

### Režimy výroby (`EnModeVyroby`) a metódy lisovania (`EnMetodaLisovania`)

Two **independent** axes on the recipe:

- **`Mode`** — `Single` (tableta z jednej zmesi) or `Multi` (multi-mix, trojvrstvová tableta).
- **`Metoda`** — `Sila` or `Vzdialenost`. Applies to the final press in **both** modes.

Multi-mix has its **own step chains**, deliberately separate so Single mode stays untouched:

| | Rozcestník | Multi vetva | Návrat do spoločnej vetvy |
|---|---|---|---|
| `CControlLis` | 100 → **102** → 105 \| 400 | 400…460 | 460 → **120** (ďalej 130 → 135 → 140\|300) |
| `CControlScales` | 102 → **105** → 110 \| 400 | 400…450 | slučka 450 → 400 |

Sequencing is driven by **`IL.ZonePress.VrstvaRequest`** (1..3): the press names the doser when it releases the zone as `InputEmpty`; the scales branch obeys it and has no round-robin state of its own. One dose = one `InputEmpty → InputFull` handoff, so a multi-mix cycle does three. The press **accumulates** `PayloadHmotnost` across them into `_aktualnaHmotnost`.

In Multi mode all three scales are forced active (`RecipeToRuntime` overrides `EnabledVaha1/2/3`, UI checkboxes disabled), and each doser has its **own** dosing profile (`CRecipe.Vaha.Davka1/2/3` → `CControlScales.GetDavkaProfile(i)`); Single mode keeps the one shared `Davka` broadcast to all.

`MotorMaster`'s position profile is set once in `InitStep30` (`SetMasterTransportProfile()`) and the pressing loops depend on it — multi-mix step 420 changes it for the compaction move, so step 430 **must** restore it via the same helper.

### UI

Avalonia 12.0.1 with Fluent Dark theme. MVVM via `CommunityToolkit.Mvvm` (`[ObservableProperty]`, `[RelayCommand]`). ViewModels poll hardware data via `Task.Run` refresh loops; all UI updates go through `Dispatcher.UIThread`.

Setup/parameter windows follow the pattern: `frm*.axaml` opens as a non-modal child window, guarded by a `private frm*? _window` null-check to prevent duplicates.

The UI log panel is backed by `ObservableCollectionSink` (Serilog sink) — capped at 1000 entries, marshalled to the UI thread via `Dispatcher.UIThread.Post`. The sink instance is accessible via `Program.UiSink`.

**Target device is a vertically mounted (portrait) touchscreen panel.** When creating or editing any window/view:
- Design for a narrow, tall viewport — stack content vertically, avoid layouts that assume wide horizontal space.
- Use `WrapPanel` instead of a horizontal `StackPanel` for item lists that can grow (e.g. `ScaleViewModels`) so extra items reflow instead of running off-screen.
- Prefer `SizeToContent="Height"` (fixed `Width` matching the panel's portrait width, e.g. `780`) over a hardcoded `Height` for setup/parameter windows, so the window always fits its actual content.
- Controls must be touch-friendly: large tap targets (buttons ≥ ~40px), no hover-only affordances, no fine-grained drag interactions.

## Coding Rules (from GEMINI.md)

1. Use modern C# — pattern matching, `async/await`, nullable reference types.
2. Log exclusively with Serilog: `Log.Information`, `Log.Error`, `Log.Fatal`. Always add `.ForContext("Name", Name)` in PLC classes.
3. Respond in Slovak, be technically precise and concise.
4. When showing code changes, show only the changed methods — not entire files.
5. **Always commit at the end of every completed task.** When the work is finished (and the build passes), stage the changed files and create a commit with a descriptive Slovak message — do not leave finished work uncommitted, and do not wait to be asked.

## CANopen / EPOS4 Rules (from `.gemini/prompts/sys_can.md`)

1. **Handshake timeouts:** Never wait for ACK (Statusword Bit 12) in an infinite loop — always use a timeout (default 100 ms).
2. **Edge-triggering (PPM):** Clear New Setpoint (Controlword Bit 4) after EPOS acknowledges — EPOS4 reacts to the rising edge; leaving it set blocks the next move.
3. **PDO COB-ID:** `Base + NodeID` (e.g. RxPDO1 for Node 14 → `0x200 + 0x0E = 0x20E`).
4. **Safety checks:** Before and after every move verify Bit 3 (Fault) and Bit 13 (Following Error) in Statusword.
