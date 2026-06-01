using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EposCmd.Net;
using Serilog;

namespace MojaPrvaAvalonia.Models;

public partial class CPlc : ObservableObject
{
    public System.Diagnostics.Stopwatch _initStopwatch = new();
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
    [ObservableProperty] private int _step;
    [ObservableProperty] private bool _requestToNextStep;
    [ObservableProperty] private bool _requestToEnd;
    [ObservableProperty] private bool _stopImmediately;
    [ObservableProperty] private bool _requestToContinue;

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
        // Log.Logger.ForContext("Name", Name).Debug($"[KROK ZMENENÝ] Step: {oldValue} -> {newValue}");
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
        //  Log.Logger.ForContext("Name", Name).Information($"Volič režimu prepnutý na: {(IsStepMode ? "KROK" : "AUTO")}");
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
        Program.MainProgram?.IxxatState == EnIxxatState.Connected &&
        ((Connection == EnStatusConnection.Disconnect &&
          (StatusPlc == EnStatusPlc.NotInit || StatusPlc == EnStatusPlc.Error)) ||
         (Connection == EnStatusConnection.Connected &&
          (StatusPlc == EnStatusPlc.NotInit || StatusPlc == EnStatusPlc.Ready || StatusPlc == EnStatusPlc.Error)));

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
        // Log.Logger.ForContext("Name", Name).Debug("[CMD] Stlačené tlačidlo: Init");
        Step = 1;
        StatusPlc = IsStepMode ? EnStatusPlc.StepMode : EnStatusPlc.Initializing;

        _cancellationTokenSource = new CancellationTokenSource();

        Task.Factory.StartNew(() =>
            {
                // Povieme operačnému systému, že toto je dôležité riadiace vlákno
                Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
                Thread.CurrentThread.Name = $"PLC_Loop_{Name}"; // Výborné pre ladenie v Rideri/Visual Studiu

                // Spustíme synchrónne (odstránime async z ProgramLoop)
                ProgramLoop(_cancellationTokenSource.Token);
            },
            _cancellationTokenSource.Token,
            TaskCreationOptions.LongRunning, // TOTO urobí z Tasku dedikované OS vlákno
            TaskScheduler.Default);
        //  Task.Run(() => ProgramLoopAsync(_cancellationTokenSource.Token));
    }

    private bool CanStartProgram() => Connection == EnStatusConnection.Connected && StatusPlc == EnStatusPlc.Ready;

    [RelayCommand(CanExecute = nameof(CanStartProgram))]
    public virtual void StartProgram()
    {
        Step = 100;
        StatusPlc = IsStepMode ? EnStatusPlc.StepMode : EnStatusPlc.Running;

        // Bezpečnostná poistka pre prípad re-štartu
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();

        Task.Factory.StartNew(() =>
            {
                // Povieme OS, že toto je riadiace vlákno s vysokou prioritou
                Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
                Thread.CurrentThread.Name = $"PLC_Loop_{Name}"; // Uľahčí diagnostiku vo Visual Studiu/Rideri

                // Voláme synchrónne
                ProgramLoop(_cancellationTokenSource.Token);
            },
            _cancellationTokenSource.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    private bool CanNextStepProgram() => Connection == EnStatusConnection.Connected &&
                                         (StatusPlc == EnStatusPlc.StepMode ||
                                          (StatusPlc == EnStatusPlc.WaitingToFinish && IsStepMode));

    [RelayCommand(CanExecute = nameof(CanNextStepProgram))]
    public virtual void NextStepProgram()
    {
        // Log.Logger.ForContext("Name", Name).Debug("[CMD] Stlačené tlačidlo: NextStep");
        RequestToNextStep = true;
    }

    [RelayCommand]
    public virtual void ContinueProgram()
    {
        RequestToContinue = true;
        Serilog.Log.Logger.ForContext("Name", Name).Information("[CMD] Stlačené tlačidlo: Pokračovať (Continue)");
    }

    private bool CanStopProgramOnEnd() => Connection == EnStatusConnection.Connected &&
                                          (StatusPlc == EnStatusPlc.Running || StatusPlc == EnStatusPlc.StepMode) &&
                                          Step >= 100;

    [RelayCommand(CanExecute = nameof(CanStopProgramOnEnd))]
    public virtual void StopProgramOnEnd()
    {
        //   Log.Logger.ForContext("Name", Name).Debug("[CMD] Stlačené tlačidlo: Parkovať (StopOnEnd)");
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
        // OPRAVA: Ak nie sme v UI vlákne, pošleme to tam a skončíme
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(StopProgramImmediately);
            return;
        }

        // Log.Logger.ForContext("Name", Name).Debug("[CMD] Stlačené tlačidlo: Stop Immediately");
        StopImmediately = true;
        StatusPlc = EnStatusPlc.WaitForStoping;
        _cancellationTokenSource?.Cancel();
    }

    // ==========================================
    // SPRÁVA PARAMETROV (Generic s reflexiou)
    // ==========================================

    public void LoadParametersFromFile<T>(string fileName, T target) where T : class
    {
        try
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory;
            var path = System.IO.Path.Combine(directory, fileName);
            if (System.IO.File.Exists(path))
            {
                var json = System.IO.File.ReadAllText(path);
                var loaded = System.Text.Json.JsonSerializer.Deserialize<T>(json);
                if (loaded != null)
                {
                    var properties = typeof(T).GetProperties();
                    foreach (var property in properties)
                    {
                        if (property.CanWrite)
                        {
                            var value = property.GetValue(loaded);
                            property.SetValue(target, value);
                        }
                    }

                    Log.Logger.ForContext("Name", Name).Information($"Parametre načítané zo súboru: {fileName}");
                }
            }
            else
            {
                Log.Logger.ForContext("Name", Name).Warning($"Súbor s parametrami neexistuje: {fileName}");
            }
        }
        catch (Exception ex)
        {
            Log.Logger.ForContext("Name", Name).Error(ex, $"Chyba pri načítavaní parametrov zo súboru {fileName}");
        }
    }

    public void SaveParametersToFile<T>(string fileName, T source) where T : class
    {
        try
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory;
            var path = System.IO.Path.Combine(directory, fileName);
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            var json = System.Text.Json.JsonSerializer.Serialize(source, options);
            System.IO.File.WriteAllText(path, json);
            Log.Logger.ForContext("Name", Name).Information($"Parametre uložené do súboru: {fileName}");
        }
        catch (Exception ex)
        {
            Log.Logger.ForContext("Name", Name).Error(ex, $"Chyba pri ukladaní parametrov do súboru {fileName}");
        }
    }

    // ==========================================
    // HLAVNÁ SLUČKA
    // ==========================================

    protected void ProgramLoop(CancellationToken token)
    {
        bool success = false;
        Log.Logger.ForContext("Name", Name).Debug("[LOOP] Slučka ProgramLoop (dedikované vlákno) odštartovaná.");

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
                    Step = RunStep(Step);
                    RequestToNextStep = false;

                    // Uspí vlákno na 10ms. Ak niekto zavolá token.Cancel(), čakanie sa okamžite preruší.
                    token.WaitHandle.WaitOne(10);
                }
                else
                {
                    StatusCycle = EnStatusCycle.WaitForStep;

                    // Opäť priame uspávanie cez WaitHandle
                    token.WaitHandle.WaitOne(5);
                }
            }

            success = true;
        }
        // TaskCanceledException sa pri WaitOne nevyhadzuje, IsCancellationRequested zabezpečí čistý únik.
        catch (CDeviceException devEx)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Log.Logger.ForContext("Name", Name).Fatal($"Step:{Step}: {devEx.ErrorMessage}");
                StatusPlc = EnStatusPlc.Error;
                Message = $"Error step:{Step}";
            });
            success = false;
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Log.Logger.ForContext("Name", Name).Fatal($"Step:{Step}: {ex.Message}");
                StatusPlc = EnStatusPlc.Error;
                Message = $"Error step:{Step}";
            });
            success = false;
        }
        finally
        {
            if (success) FinishOKHandle();
            else FinishNOKHandle();

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
                Log.Logger.ForContext("Name", Name)
                    .Information(success ? "Slučka úspešne skončila." : "Slučka skončila s chybou.");
            });
        }
    }

    public virtual void FinishOKHandle()
    {
        Log.Logger.ForContext("Name", Name).Debug($"=>FinishOKHandle Step:{Step}  ");
    }

    public virtual void FinishNOKHandle()
    {
        Log.Logger.ForContext("Name", Name).Fatal($"=>FinishNokHandle Step:{Step}  ");
    }
}