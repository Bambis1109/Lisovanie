using System;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using EposCmd.Net;
using Lisovanie.Models;
using Serilog;

namespace Lisovanie.ViewModels;

public partial class UcDeviceEpos4ViewModel : ObservableObject, IDisposable
{
    private CDeviceEpos4? _device;
    private DispatcherTimer? _refreshTimer;

    public void AssignDevice(CDeviceEpos4 device)
    {
        _device = device;
    }

    [ObservableProperty] private CDeviceEpos4Data _deviceEpos4Data;

    [ObservableProperty] private string _modeShorthand = "---";
    [ObservableProperty] private string _statusText = "OFFLINE";
    [ObservableProperty] private string _nmtText = "UNKNOWN"; // Nová property pre NMT stav

    [ObservableProperty] private IBrush _modeColor = Brushes.DarkGray;
    [ObservableProperty] private IBrush _statusColor = Brushes.DarkGray;
    [ObservableProperty] private IBrush _remoteColor = Brushes.DarkGray;
    [ObservableProperty] private IBrush _nmtColor = Brushes.DarkGray; // Farba pre NMT

    public UcDeviceEpos4ViewModel(CDeviceEpos4? device, string motorName)
    {
        _device = device;
        DeviceEpos4Data = new CDeviceEpos4Data { MotorName = motorName };
    }

    public void StartRefresh()
    {
        if (_refreshTimer != null && _refreshTimer.IsEnabled) return;

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50) // 20 Hz refresh rate
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
        if (_device?.Operation == null || _device.LowLayer?.Can == null) return;

        try
        {
            // 1. NAJDÔLEŽITEJŠÍ KROK: Zistenie fyzického stavu komunikácie
            ENmtStatus nmtState = _device.LowLayer.Can.GetNMTState();

            if (nmtState == ENmtStatus.NcsDISCONNECTED || nmtState == ENmtStatus.NcsUNKNOWN)
            {
                // Motor je mŕtvy / odpojený. Ignorujeme staré dáta a ukážeme OFFLINE.
                SetOfflineState();
                return;
            }

            // 2. Motor žije, ťaháme aktuálne dáta
            DeviceEpos4Data.NodeId = _device.NodeId;
            DeviceEpos4Data.ActualGearPosition = _device.EposData.PositionActualGear;
            DeviceEpos4Data.ActualPositionSensor2Float = _device.EposData.PositionActualSensor2;
            DeviceEpos4Data.ActualVelocity = _device.EposData.VelocityActual;
            DeviceEpos4Data.ActualCurrent = _device.EposData.CurrentActualAveragePercentage;
            DeviceEpos4Data.ActualAnalog1 = _device.EposData.AnalogInput1;

            UpdateStatusAndColors(nmtState);
        }
        catch (Exception ex)
        {
            Log.Error($"UcMotorViewModel [{DeviceEpos4Data.MotorName}] Refresh Error: {ex.Message}");
        }
    }

    private void SetOfflineState()
    {
        NmtText = "OFFLINE";
        NmtColor = Brushes.Red;

        StatusText = "---";
        StatusColor = Brushes.DarkGray;

        ModeShorthand = "---";
        ModeColor = Brushes.DarkGray;

        RemoteColor = Brushes.DarkGray;
    }

    private void UpdateStatusAndColors(ENmtStatus nmtState)
    {
        // 1. NMT Status Logic (Zobrazenie stavu zbernice)
        switch (nmtState)
        {
            case ENmtStatus.NcsOPERATIONAL:
                NmtText = "OP";
                NmtColor = Brushes.Green;
                break;
            case ENmtStatus.NcsPREOPERATIONAL:
                NmtText = "PRE-OP";
                NmtColor = Brushes.Orange;
                break;
            case ENmtStatus.NcsSTOPPED:
                NmtText = "STOP";
                NmtColor = Brushes.Red;
                break;
            default:
                NmtText = nmtState.ToString();
                NmtColor = Brushes.Gray;
                break;
        }

        // 2. Mode of Operation Logic
        var mode = _device!.EposData.ModeOfOperationDisplay;
        switch (mode)
        {
            case EOperationMode.OmdProfilePositionMode:
                ModeShorthand = "PPM";
                ModeColor = Brushes.Blue;
                break;
            case EOperationMode.OmdProfileVelocityMode:
                ModeShorthand = "PVM";
                ModeColor = Brushes.YellowGreen;
                break;
            case EOperationMode.OmdHomingMode:
                ModeShorthand = "HM";
                ModeColor = Brushes.Magenta;
                break;
            case EOperationMode.OmdCyclicSynchronousPositionMode:
                ModeShorthand = "CSPM";
                ModeColor = Brushes.Coral;
                break;
            case EOperationMode.OmdCyclicSynchronousVelocityMode:
                ModeShorthand = "CSVM";
                ModeColor = Brushes.DarkOrange;
                break;
            case EOperationMode.OmdCyclicSyncronicTorqueMode:
                ModeShorthand = "CSTM";
                ModeColor = Brushes.RosyBrown;
                break;
            default:
                ModeShorthand = "Unknown";
                ModeColor = Brushes.Brown;
                break;
        }

        // 3. Statusword Logic (CiA 402)
        ushort sw = _device.EposData.Statusword;

        if ((sw & 0x0008) == 0x0008) // Bit 3 = Fault
        {
            StatusText = "Fault";
            StatusColor = Brushes.Red;
        }
        else if ((sw & 0x0437) == 0x0037) // Enable + Target NOT Reached
        {
            StatusText = "Movement";
            StatusColor = Brushes.Orange;
        }
        else if ((sw & 0x0437) == 0x0437) // Enable + Target Reached
        {
            StatusText = "Enable";
            StatusColor = Brushes.LimeGreen;
        }
        else if ((sw & 0x006F) == 0x0007) // Quick Stop Active
        {
            StatusText = "Quick stop";
            StatusColor = Brushes.Yellow;
        }
        else if ((sw & 0x004F) == 0x0040) // Switch On Disabled
        {
            StatusText = "Disable";
            StatusColor = Brushes.BlueViolet;
        }
        else
        {
            StatusText = "Ready"; // Alebo iný prechodný stav
            StatusColor = Brushes.Gray;
        }

        // 4. Remote Status (Bit 9)
        RemoteColor = _device.EposData.RemoteStatus ? Brushes.Green : Brushes.Red;
    }

    public void Dispose()
    {
        StopRefresh();
    }
}