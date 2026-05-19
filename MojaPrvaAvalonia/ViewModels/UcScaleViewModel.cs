using System;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EposCmd.Net;
using EposCmd.Net.DeviceScaleSet;
using Serilog;

namespace MojaPrvaAvalonia.ViewModels;

public partial class UcScaleViewModel : ObservableObject, IDisposable
{
    private CDeviceScale? _device;
    private DispatcherTimer? _refreshTimer;

    public void AssignDevice(CDeviceScale device)
    {
        _device = device;
        ScaleName = device.Name;
    }

    public CDeviceScale? Device => _device;

    [ObservableProperty] private string _scaleName = string.Empty;
    [ObservableProperty] private bool _isSetupVisible = true;
    [ObservableProperty] private string _nmtText = "UNKNOWN";
    [ObservableProperty] private IBrush _nmtColor = Brushes.DarkGray;
    
    // Zablokovanie UI počas vykonávania Async príkazu
    [ObservableProperty] private bool _isUiEnabled = true;

    // --- Telemetria (Hmotnosť) v kg ---
    [ObservableProperty] private double _weight32InterKg;
    [ObservableProperty] private double _weight32TareKg;
    [ObservableProperty] private double _weight32ActualKg;
    [ObservableProperty] private double _weightFinalKg;

    // --- Stavy ---
    [ObservableProperty] private string _statusMainProc = "---";
    [ObservableProperty] private string _statusMainMat = "---";
    [ObservableProperty] private string _statusMainZone = "---";
    
    [ObservableProperty] private string _statusVyloznikProc = "---";
    [ObservableProperty] private string _statusVyloznikMat = "---";
    
    [ObservableProperty] private string _statusDoserProc = "---";
    [ObservableProperty] private string _statusDoserMat = "---";

    [ObservableProperty] private string _weightResult = "---";

    public UcScaleViewModel() {}

    public UcScaleViewModel(CDeviceScale? device, string scaleName)
    {
        _device = device;
        ScaleName = scaleName;
    }

    public void StartRefresh()
    {
        if (_refreshTimer != null && _refreshTimer.IsEnabled) return;

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _refreshTimer.Tick += OnRefreshTick;
        _refreshTimer.Start();
    }

    public void StopRefresh()
    {
        if (_refreshTimer != null)
        {
            _refreshTimer.Stop();
            _refreshTimer.Tick -= OnRefreshTick;
            _refreshTimer = null;
        }
    }

    private void OnRefreshTick(object? sender, EventArgs e)
    {
        if (_device?.Data == null) return;

        try
        {
            var data = (CDataScale)_device.Data;
            ENmtStatus nmtState = data.NmtStatus;

            if (nmtState == ENmtStatus.NcsDISCONNECTED || nmtState == ENmtStatus.NcsUNKNOWN)
            {
                SetOfflineState();
                return;
            }

            // Prepočty z miligramov na kilogramy
            Weight32InterKg = data.Weight32Inter / 1000.0;
            Weight32TareKg = data.Weight32Tare / 1000.0;
            Weight32ActualKg = data.Weight32Actual / 1000.0;
            WeightFinalKg = data.WeightFinal / 1000.0;

            // Zobrazenie stavov ako string
            StatusMainProc = data.StatusMainProc.ToString();
            StatusMainMat = data.StatusMainMat.ToString();
            StatusMainZone = data.StatusMainZone.ToString();

            StatusVyloznikProc = data.StatusVyloznikProc.ToString();
            StatusVyloznikMat = data.StatusVyloznikMat.ToString();

            StatusDoserProc = data.StatusDoserProc.ToString();
            StatusDoserMat = data.StatusDoserMat.ToString();
            
            WeightResult = data.WeightResult.ToString();

            UpdateStatusAndColors(nmtState);
        }
        catch (Exception ex)
        {
            Log.Error($"UcScaleViewModel [{ScaleName}] Refresh Error: {ex.Message}");
        }
    }

    private void SetOfflineState()
    {
        NmtText = "OFFLINE";
        NmtColor = Brushes.Red;
    }

    private void UpdateStatusAndColors(ENmtStatus nmtState)
    {
        switch (nmtState)
        {
            case ENmtStatus.NcsOPERATIONAL: NmtText = "OP"; NmtColor = Brushes.Green; break;
            case ENmtStatus.NcsPREOPERATIONAL: NmtText = "PRE-OP"; NmtColor = Brushes.Orange; break;
            case ENmtStatus.NcsSTOPPED: NmtText = "STOP"; NmtColor = Brushes.Red; break;
            default: NmtText = nmtState.ToString(); NmtColor = Brushes.Gray; break;
        }
    }

    // ========================================================================
    // ASYNCHRÓNNE PRÍKAZY PRE OVLÁDANIE (ČASŤ B)
    // ========================================================================
    private async Task ExecuteCommandAsync(Func<Task> commandAction)
    {
        if (_device == null) return;

        IsUiEnabled = false;
        try
        {
            await Task.Run(commandAction);
        }
        catch (CDeviceException dex)
        {
            Log.Error($"Chyba zariadenia [{ScaleName}]: {dex.ErrorMessage}");
        }
        catch (Exception ex)
        {
            Log.Error($"Kritická chyba pri zápise na [{ScaleName}]: {ex.Message}");
        }
        finally
        {
            IsUiEnabled = true;
        }
    }

    // --- MAIN ---
    [RelayCommand] public Task MasterInit() => ExecuteCommandAsync(async () => _device!.Operation.Master.SendCommand(EMasterCommand.Init));
    [RelayCommand] public Task MasterProdukcia() => ExecuteCommandAsync(async () => _device!.Operation.Master.SendCommand(EMasterCommand.Produkcia));
    [RelayCommand] public Task MasterNext() => ExecuteCommandAsync(async () => _device!.Operation.Master.SendCommand(EMasterCommand.Next));
    [RelayCommand] public Task MasterStop() => ExecuteCommandAsync(async () => _device!.Operation.Master.SendCommand(EMasterCommand.Stop));

    // --- DOSER ---
    [RelayCommand] public Task DoserInit() => ExecuteCommandAsync(async () => _device!.Operation.Doser.SendCommand(EDoserCommand.Init));
    [RelayCommand] public Task DoserTune() => ExecuteCommandAsync(async () => _device!.Operation.Doser.SendCommand(EDoserCommand.Tune));
    [RelayCommand] public Task DoserProd() => ExecuteCommandAsync(async () => _device!.Operation.Doser.SendCommand(EDoserCommand.Prod));
    [RelayCommand] public Task DoserVyklop() => ExecuteCommandAsync(async () => _device!.Operation.Doser.SendCommand(EDoserCommand.Vyklop));
    [RelayCommand] public Task DoserStop() => ExecuteCommandAsync(async () => _device!.Operation.Doser.SendCommand(EDoserCommand.Stop));

    // --- BOOM ---
    [RelayCommand] public Task BoomInit() => ExecuteCommandAsync(async () => _device!.Operation.Boom.SendCommand(EBoomCommand.Init));
    [RelayCommand] public Task BoomVysun1() => ExecuteCommandAsync(async () => _device!.Operation.Boom.SendCommand(EBoomCommand.Vysun1));
    [RelayCommand] public Task BoomVysun2() => ExecuteCommandAsync(async () => _device!.Operation.Boom.SendCommand(EBoomCommand.Vysun2));
    [RelayCommand] public Task BoomVyloz1() => ExecuteCommandAsync(async () => _device!.Operation.Boom.SendCommand(EBoomCommand.Vyloz1));
    [RelayCommand] public Task BoomVyloz2() => ExecuteCommandAsync(async () => _device!.Operation.Boom.SendCommand(EBoomCommand.Vyloz2));
    [RelayCommand] public Task BoomVysyp() => ExecuteCommandAsync(async () => _device!.Operation.Boom.SendCommand(EBoomCommand.Vysyp));
    [RelayCommand] public Task BoomZasun() => ExecuteCommandAsync(async () => _device!.Operation.Boom.SendCommand(EBoomCommand.Zasun));

    // --- LOCK ---
    [RelayCommand] public Task LockInit() => ExecuteCommandAsync(async () => _device!.Operation.Lock.SendCommand(ELockCommand.Init));
    [RelayCommand] public Task LockOdomkni() => ExecuteCommandAsync(async () => _device!.Operation.Lock.SendCommand(ELockCommand.Odomkni));
    [RelayCommand] public Task LockZamkni() => ExecuteCommandAsync(async () => _device!.Operation.Lock.SendCommand(ELockCommand.Zamkni));
    [RelayCommand] public Task LockVysypVlavo() => ExecuteCommandAsync(async () => _device!.Operation.Lock.SendCommand(ELockCommand.VysypVlavo));
    [RelayCommand] public Task LockVysypVpravo() => ExecuteCommandAsync(async () => _device!.Operation.Lock.SendCommand(ELockCommand.VysypVpravo));
    [RelayCommand] public Task LockKalibruj() => ExecuteCommandAsync(async () => _device!.Operation.Lock.SendCommand(ELockCommand.Kalibruj));

    // --- WEIGHER (SCALE) ---
    [RelayCommand] public Task ScaleInit() => ExecuteCommandAsync(async () => _device!.Operation.Weigher.SendCommand(EScaleCommand.Init));
    [RelayCommand] public Task ScaleKalibrujMin() => ExecuteCommandAsync(async () => _device!.Operation.Weigher.SendCommand(EScaleCommand.KalibrujMin));
    [RelayCommand] public Task ScaleKalibrujMax() => ExecuteCommandAsync(async () => _device!.Operation.Weigher.SendCommand(EScaleCommand.KalibrujMax));
    [RelayCommand] public Task ScaleTara() => ExecuteCommandAsync(async () => _device!.Operation.Weigher.SendCommand(EScaleCommand.Tara));

    // --- SYSTEM ---
    [RelayCommand] public Task SystemSave() => ExecuteCommandAsync(async () => _device!.Operation.System.SendCommand(ESystemCommand.Save));
    [RelayCommand] public Task SystemLoad() => ExecuteCommandAsync(async () => _device!.Operation.System.SendCommand(ESystemCommand.Load));
    [RelayCommand] public Task SystemRestart() => ExecuteCommandAsync(async () => _device!.Operation.System.SendCommand(ESystemCommand.Restart));

    public void Dispose()
    {
        StopRefresh();
    }
}