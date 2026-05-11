# MojaPrvaAvalonia

An Avalonia UI application simulating a PLC-controlled machine (Manipulator) with motor data and real-time logging.

## AI Context & Documentation

> [!IMPORTANT]
> This project uses standalone `.txt` files to store detailed AI-readable context and architectural documentation for specific C# classes. 
> 
> **GLOBAL RULE:** Before modifying ANY `.cs` file, you MUST check if a corresponding `.txt` file exists in the same directory (e.g., if editing `MyClass.cs`, check for `MyClass.txt`). If it exists, you MUST read its contents to understand the underlying math, architecture, or business logic before making any changes.

## Project Overview

- **Purpose**: Simulate industrial automation control (PLC) and monitoring (HMI).
- **Architecture**: MVVM (Model-View-ViewModel) utilizing `CommunityToolkit.Mvvm`.
- **Core Logic**:
  - `CPlc`: Base class for PLC simulation with an asynchronous program loop (`ProgramLoopAsync`).
  - `CManipulator`: Specialized PLC implementation with multiple motors and specific step-based sequences.
  - `CMainProgram`: Orchestrates the application logic and manages the list of PLCs.
- **Logging**: Uses Serilog for structured logging, including a custom `ObservableCollectionSink` to display logs directly in the UI.

## Technologies

- **Runtime**: .NET 10.0
- **UI Framework**: Avalonia UI (v12.0.1) with Fluent Theme.
- **MVVM Toolkit**: CommunityToolkit.Mvvm.
- **Logging**: Serilog with Console, File, and Custom UI sinks.

## Building and Running

### Prerequisites
- .NET 10.0 SDK

### Commands
- **Build**: `dotnet build`
- **Run**: `dotnet run --project MojaPrvaAvalonia/MojaPrvaAvalonia.csproj`
- **Clean**: `dotnet clean`

## Development Conventions

- **Naming**: Standard C# PascalCase for classes and methods; `_camelCase` for private fields.
- **MVVM**:
  - ViewModels inherit from `ViewModelBase` (which inherits from `ObservableObject`).
  - Use `[ObservableProperty]` for reactive properties.
  - Use `[RelayCommand]` for UI commands.
- **Asynchronous Patterns**:
  - Program loops are handled via `Task.Run` with `CancellationToken` support.
  - UI updates from background threads must use `Dispatcher.UIThread.Post`.
- **Project Structure**:
  - `Models/`: Domain logic and data structures (PLC, Motors, Enums).
  - `ViewModels/`: UI logic and state.
  - `Views/`: XAML files and code-behind for the UI.
  - `Logging/`: Custom logging infrastructure.
  - `Converters/`: XAML value converters (e.g., `LogColorConverter`).

## Key Files

- `MojaPrvaAvalonia/Program.cs`: Entry point, Serilog configuration.
- `MojaPrvaAvalonia/App.axaml.cs`: Application lifecycle, DI for `CMainProgram` into `MainWindowViewModel`.
- `MojaPrvaAvalonia/Models/CPlc.cs`: Core simulation logic and state machine.
- `MojaPrvaAvalonia/Models/CManipulator.cs`: Step definitions for the simulation.
- `MojaPrvaAvalonia/ViewModels/MainWindowViewModel.cs`: Main UI state and log bridge.
