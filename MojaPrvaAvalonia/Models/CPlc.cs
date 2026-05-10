using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace MojaPrvaAvalonia.Models;

public partial class CPlc : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(InitCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartProgramCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextStepProgramCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopProgramOnEndCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopProgramImmediatelyCommand))]
    private EnStatusPlc _statusPlc = EnStatusPlc.NotInit;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(InitCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartProgramCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextStepProgramCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopProgramOnEndCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopProgramImmediatelyCommand))]
    private EnStatusConnection _connection = EnStatusConnection.Disconnect;

    [ObservableProperty] private EnStatusCycle _statusCycle = EnStatusCycle.WaitForInit;
    [ObservableProperty] private EnModePlc _modeMachine = EnModePlc.Auto;

    [ObservableProperty] private string _message = "Stroj vypnutý";

    // OPRAVA: Odstránený NotifyCanExecuteChangedFor. Zabraňuje to pádu na background vlákne.
    [ObservableProperty] private int _step;

    [ObservableProperty] private bool _requestToNextStep;
    [ObservableProperty] private bool _requestToEnd;
    [ObservableProperty] private bool _stopImmediately;

    [ObservableProperty] private bool _isStepMode;

    public string ModeText => IsStepMode ? "KROK" : "AUTO";

    protected CancellationTokenSource? _cancellationTokenSource;

    public CPlc(string name)
    {
        Name = name;
    }

    // ==========================================
    // AUTOMATICKÉ LOGOVANIE ZMIEN
    // ==========================================

    partial void OnStatusPlcChanged(EnStatusPlc oldValue, EnStatusPlc newValue)
    {
        Log.Logger.ForContext("Name", Name).Debug($"[STAV ZMENENÝ] StatusPlc: {oldValue} -> {newValue}");
    }

    partial void OnConnectionChanged(EnStatusConnection oldValue, EnStatusConnection newValue)
    {
        Log.Logger.ForContext("Name", Name).Debug($"[STAV ZMENENÝ] Connection: {oldValue} -> {newValue}");
    }

    partial void OnStepChanged(int oldValue, int newValue)
    {
        Log.Logger.ForContext("Name", Name).Debug($"[KROK ZMENENÝ] Step: {oldValue} -> {newValue}");
    }

    // ==========================================
    // LOGIKA KROKOV
    // ==========================================

    public virtual int RunStep(int step)
    {
        Log.Logger.ForContext("Name", Name).Fatal($"Neznámy krok ({step}) => program zastavený!");
        return 0;
    }

    // ==========================================
    // PRÍKAZY A ICH PODMIENKY
    // ==========================================
    [RelayCommand]
    public void ToggleMode()
    {
        IsStepMode = !IsStepMode;
        OnPropertyChanged(nameof(ModeText));
        Log.Logger.ForContext("Name", Name).Information($"Volič režimu prepnutý na: {(IsStepMode ? "KROK" : "AUTO")}");

        if (StatusPlc == EnStatusPlc.Running && IsStepMode)
        {
            Log.Logger.ForContext("Name", Name).Information("Stroj pozastavený (Prepnuté do krokového režimu).");
            StatusPlc = EnStatusPlc.StepMode;
        }
        else if (StatusPlc == EnStatusPlc.StepMode && !IsStepMode)
        {
            Log.Logger.ForContext("Name", Name).Information("Stroj pokračuje plynule (Prepnuté do AUTO).");
            StatusPlc = EnStatusPlc.Running;
        }

        NextStepProgramCommand.NotifyCanExecuteChanged();
    }

    private bool CanConnect() =>
        (Connection == EnStatusConnection.Disconnect && (StatusPlc == EnStatusPlc.NotInit || StatusPlc == EnStatusPlc.Error)) ||
        (Connection == EnStatusConnection.Connected &&
         (StatusPlc == EnStatusPlc.NotInit || StatusPlc == EnStatusPlc.Ready || StatusPlc == EnStatusPlc.Error));

    [RelayCommand(CanExecute = nameof(CanConnect))]
    public virtual async Task ConnectAsync()
    {
        Log.Logger.ForContext("Name", Name).Debug($"[CPlc] ConnectAsync base call.");
        await Task.CompletedTask;
    }

    private bool CanInit() => Connection == EnStatusConnection.Connected &&
                              (StatusPlc == EnStatusPlc.NotInit || StatusPlc == EnStatusPlc.Ready ||
                               StatusPlc == EnStatusPlc.Error);

    [RelayCommand(CanExecute = nameof(CanInit))]
    public virtual void Init()
    {
        Log.Logger.ForContext("Name", Name).Debug("[CMD] Stlačené tlačidlo: Init");

        // OPRAVA: Najprv nastavíme krok, až potom StatusPlc, aby UI prepočítalo tlačidlá so správnym krokom
        Step = 1;
        StatusPlc = IsStepMode ? EnStatusPlc.StepMode : EnStatusPlc.Initializing;

        _cancellationTokenSource = new CancellationTokenSource();
        Task.Run(() => ProgramLoopAsync(_cancellationTokenSource.Token));
    }

    private bool CanStartProgram() => Connection == EnStatusConnection.Connected && StatusPlc == EnStatusPlc.Ready;

    [RelayCommand(CanExecute = nameof(CanStartProgram))]
    public virtual void StartProgram()
    {
        Log.Logger.ForContext("Name", Name).Debug("[CMD] Stlačené tlačidlo: Start");

        // OPRAVA: Najprv nastavíme krok, až potom StatusPlc
        Step = 100;
        StatusPlc = IsStepMode ? EnStatusPlc.StepMode : EnStatusPlc.Running;

        _cancellationTokenSource = new CancellationTokenSource();
        Task.Run(() => ProgramLoopAsync(_cancellationTokenSource.Token));
    }

    private bool CanNextStepProgram() => Connection == EnStatusConnection.Connected &&
                                         (StatusPlc == EnStatusPlc.StepMode ||
                                          (StatusPlc == EnStatusPlc.WaitingToFinish && IsStepMode));

    [RelayCommand(CanExecute = nameof(CanNextStepProgram))]
    public virtual void NextStepProgram()
    {
        Log.Logger.ForContext("Name", Name).Debug("[CMD] Stlačené tlačidlo: NextStep");
        RequestToNextStep = true;
    }

    private bool CanStopProgramOnEnd() => Connection == EnStatusConnection.Connected &&
                                          (StatusPlc == EnStatusPlc.Running || StatusPlc == EnStatusPlc.StepMode) &&
                                          Step >= 100;

    [RelayCommand(CanExecute = nameof(CanStopProgramOnEnd))]
    public virtual void StopProgramOnEnd()
    {
        Log.Logger.ForContext("Name", Name).Debug("[CMD] Stlačené tlačidlo: Parkovať (StopOnEnd)");
        RequestToEnd = true;
        StatusPlc = EnStatusPlc.WaitingToFinish;
        NextStepProgramCommand.NotifyCanExecuteChanged();
    }

    private bool CanStopProgramImmediately() => Connection == EnStatusConnection.Connected &&
                                                (StatusPlc == EnStatusPlc.Running ||
                                                 StatusPlc == EnStatusPlc.Initializing ||
                                                 StatusPlc == EnStatusPlc.StepMode ||
                                                 StatusPlc == EnStatusPlc.WaitingToFinish);

    [RelayCommand(CanExecute = nameof(CanStopProgramImmediately))]
    public virtual void StopProgramImmediately()
    {
        Log.Logger.ForContext("Name", Name).Debug("[CMD] Stlačené tlačidlo: Stop Immediately");
        StopImmediately = true;
        StatusPlc = EnStatusPlc.WaitForStoping;
        _cancellationTokenSource?.Cancel();
    }

    // ==========================================
    // HLAVNÁ SLUČKA
    // ==========================================

    protected async Task ProgramLoopAsync(CancellationToken token)
    {
        Log.Logger.ForContext("Name", Name).Debug("[LOOP] Slučka ProgramLoopAsync odštartovaná.");

        Dispatcher.UIThread.Post(() =>
        {
            StopImmediately = false;
            RequestToEnd = false;
            Message = Step < 100 ? "Prebieha inicializácia..." : "Program beží";
        });

        try
        {
            while (!token.IsCancellationRequested)
            {
                if (Step == 99)
                {
                    Log.Logger.ForContext("Name", Name)
                        .Information("[LOOP] Krok 99: Inicializácia úspešná. Pripravené na Štart.");
                    Dispatcher.UIThread.Post(() =>
                    {
                        StatusPlc = EnStatusPlc.Ready;
                        Message = "Pripravené na štart.";
                    });
                    break;
                }

                if (Step == 0)
                {
                    Log.Logger.ForContext("Name", Name).Information("[LOOP] Krok 0: Program ukončený/zaparkovaný.");
                    Dispatcher.UIThread.Post(() =>
                    {
                        StatusPlc = EnStatusPlc.NotInit;
                        Message = "Program ukončený. Vyžaduje sa Init.";
                    });
                    break;
                }

                if (StopImmediately)
                {
                    Log.Logger.ForContext("Name", Name).Warning("[LOOP] Zachytený StopImmediately, ukončujem slučku.");
                    Dispatcher.UIThread.Post(() =>
                    {
                        StatusPlc = EnStatusPlc.NotInit;
                        Message = "Zastavené (Stop). Vyžaduje sa Init.";
                    });
                    break;
                }

                bool shouldWait = StatusPlc == EnStatusPlc.StepMode ||
                                  (StatusPlc == EnStatusPlc.WaitingToFinish && IsStepMode);

                if (!shouldWait || RequestToNextStep)
                {
                    Log.Logger.ForContext("Name", Name).Debug($"[LOOP] Vykonávam RunStep({Step})");
                    Step = RunStep(Step);
                    RequestToNextStep = false;
                    await Task.Delay(10, token);
                }
                else
                {
                    StatusCycle = EnStatusCycle.WaitForStep;
                    await Task.Delay(10, token);
                }
            }
        }
        catch (TaskCanceledException)
        {
            Log.Logger.ForContext("Name", Name).Debug("[LOOP] Slučka bola zrušená cez CancellationToken.");
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Log.Logger.ForContext("Name", Name).Fatal(ex, "Kritická chyba v slučke!");
                StatusPlc = EnStatusPlc.Error;
            });
        }
        finally
        {
            Log.Logger.ForContext("Name", Name).Debug("[LOOP] Slučka vstupuje do bloku finally (Ukončovanie).");

            Dispatcher.UIThread.Post(() =>
            {
                RequestToEnd = false;
                StopImmediately = false;
                StatusCycle = EnStatusCycle.WaitForInit;

                if (StatusPlc != EnStatusPlc.Ready)
                {
                    StatusPlc = EnStatusPlc.NotInit;
                    if (Step != 0 && Step != 99)
                    {
                        Message = "Slučka prerušená. Vyžaduje sa Init.";
                    }
                }

                Step = 0;
                Log.Logger.ForContext("Name", Name).Information("Slučka úspešne skončila.");
            });
        }
    }
}