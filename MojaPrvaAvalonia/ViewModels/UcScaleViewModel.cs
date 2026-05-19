using System;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using EposCmd.Net;
using MojaPrvaAvalonia.Models;
using Serilog;

namespace MojaPrvaAvalonia.ViewModels;

public partial class UcScaleViewModel : ObservableObject, IDisposable
{
    private CDeviceScale? _device;
    private DispatcherTimer? _refreshTimer;

    public void AssignDevice(CDeviceScale device)
    {
        _device = device;
    }

    [ObservableProperty] private CScaleData _scaleData;

    [ObservableProperty] private string _nmtText = "UNKNOWN";
    [ObservableProperty] private IBrush _nmtColor = Brushes.DarkGray;
    
    [ObservableProperty] private bool _isSetupVisible = true;

    public CDeviceScale? Device => _device;

    public UcScaleViewModel(CDeviceScale? device, string scaleName)
    {
        _device = device;
        ScaleData = new CScaleData { ScaleName = scaleName };
    }

    public void StartRefresh()
    {
        if (_refreshTimer != null && _refreshTimer.IsEnabled) return;

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100) // 10 Hz refresh rate
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

            ScaleData.NodeId = _device.NodeId;
            ScaleData.WeightFinal = data.WeightFinal;
            ScaleData.WeightRaw = data.WeightRaw;
            ScaleData.Weight32Actual = data.Weight32Actual;
            ScaleData.StatusMainProc = data.StatusMainProc.ToString();
            ScaleData.StatusMainMat = data.StatusMainMat.ToString();

            UpdateStatusAndColors(nmtState);
        }
        catch (Exception ex)
        {
            Log.Error($"UcScaleViewModel [{ScaleData.ScaleName}] Refresh Error: {ex.Message}");
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

    public void Dispose()
    {
        StopRefresh();
    }
}