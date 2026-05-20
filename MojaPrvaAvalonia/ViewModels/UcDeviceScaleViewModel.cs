// ==========================================
// Súbor: MojaPrvaAvalonia\ViewModels\UcDeviceScaleViewModel.cs
// ==========================================

using System;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EposCmd.Net;
using EposCmd.Net.DeviceScaleSet;
using MojaPrvaAvalonia.Models;
using Serilog;

namespace MojaPrvaAvalonia.ViewModels;

public partial class UcDeviceScaleViewModel : ObservableObject, IDisposable
{
    private CDeviceScale? _device;
    public CControlScales? ControlScales { get; } // Referencia na PLC vrstvu
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

    // Konštruktor pre XAML Designer
    public UcDeviceScaleViewModel() {}

    // Hlavný konštruktor s injekciou PLC vrstvy
    public UcDeviceScaleViewModel(CControlScales? controlScales, CDeviceScale? device, string scaleName)
    {
        ControlScales = controlScales;
        _device = device;
        ScaleName = scaleName;
    }

    public void StartRefresh()
    {
        if (_refreshTimer != null && _refreshTimer.IsEnabled) return;

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100) // 10 Hz refresh
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
    // PRÍKAZY PRE OVLÁDANIE (Delegované na CControlScales)
    // ========================================================================
    
    private void ExecuteCommand(Action<CDeviceScale> commandAction, string commandName)
    {
        if (_device == null) return;

        if (ControlScales == null)
        {
            Log.Warning($"[{ScaleName}] Chýba referencia na CControlScales. Povel {commandName} nebol odoslaný.");
            return;
        }

        // Delegovanie na PLC vrstvu (Fire-and-Forget)
        ControlScales.SendScaleCommand(_device, commandAction, commandName);
    }

    // --- MAIN ---
    [RelayCommand] public void MasterInit() => ExecuteCommand(d => d.Operation.Master.SendCommand(EMasterCommand.Init), nameof(MasterInit));
    [RelayCommand] public void MasterProdukcia() => ExecuteCommand(d => d.Operation.Master.SendCommand(EMasterCommand.Produkcia), nameof(MasterProdukcia));
    [RelayCommand] public void MasterNext() => ExecuteCommand(d => d.Operation.Master.SendCommand(EMasterCommand.Next), nameof(MasterNext));
    [RelayCommand] public void MasterStop() => ExecuteCommand(d => d.Operation.Master.SendCommand(EMasterCommand.Stop), nameof(MasterStop));

    // --- DOSER ---
    [RelayCommand] public void DoserInit() => ExecuteCommand(d => d.Operation.Doser.SendCommand(EDoserCommand.Init), nameof(DoserInit));
    [RelayCommand] public void DoserTune() => ExecuteCommand(d => d.Operation.Doser.SendCommand(EDoserCommand.Tune), nameof(DoserTune));
    [RelayCommand] public void DoserProd() => ExecuteCommand(d => d.Operation.Doser.SendCommand(EDoserCommand.Prod), nameof(DoserProd));
    [RelayCommand] public void DoserVyklop() => ExecuteCommand(d => d.Operation.Doser.SendCommand(EDoserCommand.Vyklop), nameof(DoserVyklop));
    [RelayCommand] public void DoserStop() => ExecuteCommand(d => d.Operation.Doser.SendCommand(EDoserCommand.Stop), nameof(DoserStop));

    // --- BOOM ---
    [RelayCommand] public void BoomInit() => ExecuteCommand(d => d.Operation.Boom.SendCommand(EBoomCommand.Init), nameof(BoomInit));
    [RelayCommand] public void BoomVysun1() => ExecuteCommand(d => d.Operation.Boom.SendCommand(EBoomCommand.Vysun1), nameof(BoomVysun1));
    [RelayCommand] public void BoomVysun2() => ExecuteCommand(d => d.Operation.Boom.SendCommand(EBoomCommand.Vysun2), nameof(BoomVysun2));
    [RelayCommand] public void BoomVyloz1() => ExecuteCommand(d => d.Operation.Boom.SendCommand(EBoomCommand.Vyloz1), nameof(BoomVyloz1));
    [RelayCommand] public void BoomVyloz2() => ExecuteCommand(d => d.Operation.Boom.SendCommand(EBoomCommand.Vyloz2), nameof(BoomVyloz2));
    [RelayCommand] public void BoomVysyp() => ExecuteCommand(d => d.Operation.Boom.SendCommand(EBoomCommand.Vysyp), nameof(BoomVysyp));
    [RelayCommand] public void BoomZasun() => ExecuteCommand(d => d.Operation.Boom.SendCommand(EBoomCommand.Zasun), nameof(BoomZasun));

    // --- LOCK ---
    [RelayCommand] public void LockInit() => ExecuteCommand(d => d.Operation.Lock.SendCommand(ELockCommand.Init), nameof(LockInit));
    [RelayCommand] public void LockOdomkni() => ExecuteCommand(d => d.Operation.Lock.SendCommand(ELockCommand.Odomkni), nameof(LockOdomkni));
    [RelayCommand] public void LockZamkni() => ExecuteCommand(d => d.Operation.Lock.SendCommand(ELockCommand.Zamkni), nameof(LockZamkni));
    [RelayCommand] public void LockVysypVlavo() => ExecuteCommand(d => d.Operation.Lock.SendCommand(ELockCommand.VysypVlavo), nameof(LockVysypVlavo));
    [RelayCommand] public void LockVysypVpravo() => ExecuteCommand(d => d.Operation.Lock.SendCommand(ELockCommand.VysypVpravo), nameof(LockVysypVpravo));
    [RelayCommand] public void LockKalibruj() => ExecuteCommand(d => d.Operation.Lock.SendCommand(ELockCommand.Kalibruj), nameof(LockKalibruj));

    // --- WEIGHER (SCALE) ---
    [RelayCommand] public void ScaleInit() => ExecuteCommand(d => d.Operation.Weigher.SendCommand(EScaleCommand.Init), nameof(ScaleInit));
    [RelayCommand] public void ScaleKalibrujMin() => ExecuteCommand(d => d.Operation.Weigher.SendCommand(EScaleCommand.KalibrujMin), nameof(ScaleKalibrujMin));
    [RelayCommand] public void ScaleKalibrujMax() => ExecuteCommand(d => d.Operation.Weigher.SendCommand(EScaleCommand.KalibrujMax), nameof(ScaleKalibrujMax));
    [RelayCommand] public void ScaleTara() => ExecuteCommand(d => d.Operation.Weigher.SendCommand(EScaleCommand.Tara), nameof(ScaleTara));

    // --- SYSTEM ---
    [RelayCommand] public void SystemSave() => ExecuteCommand(d => d.Operation.System.SendCommand(ESystemCommand.Save), nameof(SystemSave));
    [RelayCommand] public void SystemLoad() => ExecuteCommand(d => d.Operation.System.SendCommand(ESystemCommand.Load), nameof(SystemLoad));
    [RelayCommand] public void SystemRestart() => ExecuteCommand(d => d.Operation.System.SendCommand(ESystemCommand.Restart), nameof(SystemRestart));

    public void Dispose()
    {
        StopRefresh();
    }
}