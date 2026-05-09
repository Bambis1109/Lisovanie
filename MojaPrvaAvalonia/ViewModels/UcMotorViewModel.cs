using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using EposCmd.Net;
using MojaPrvaAvalonia.Models;
using Serilog;

namespace MojaPrvaAvalonia.ViewModels;

public partial class UcMotorViewModel : ObservableObject, IDisposable
{
    private readonly CDeviceEpos4? _device;
    private DispatcherTimer? _refreshTimer;
    
    [ObservableProperty]
    private CDataMotor _motorData;

    [ObservableProperty] private string _modeShorthand = "Unknown";
    [ObservableProperty] private string _statusText = "Unknown";
    [ObservableProperty] private IBrush _modeColor = Brushes.Brown;
    [ObservableProperty] private IBrush _statusColor = Brushes.Brown;
    [ObservableProperty] private IBrush _remoteColor = Brushes.Red;

    public UcMotorViewModel(CDeviceEpos4? device, string motorName)
    {
        _device = device;
        MotorData = new CDataMotor { MotorName = motorName };
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
        
        Log.Debug($"UcMotorViewModel [{MotorData.MotorName}]: Refresh started.");
    }

    public void StopRefresh()
    {
        if (_refreshTimer != null)
        {
            _refreshTimer.Stop();
            _refreshTimer.Tick -= OnRefreshTick;
            _refreshTimer = null;
            Log.Debug($"UcMotorViewModel [{MotorData.MotorName}]: Refresh stopped.");
        }
    }

    private void OnRefreshTick(object? sender, EventArgs e)
    {
        if (_device?.Operation == null) return;

        try
        {
            // Pull data from the thread-safe CDataCO object
            MotorData.NodeId = _device.NodeId;
            MotorData.ActualGearPosition = _device.Data.PositionActualGear;
            MotorData.ActualPositionSensor2Float = _device.Data.PositionActualSensor2Float;
            MotorData.ActualVelocity = _device.Data.VelocityActual;
            MotorData.ActualCurrent = _device.Data.CurrentActualAveragePercentage;
            MotorData.ActualAnalog1 = _device.Data.AnalogInput1Weight;

            UpdateStatusAndColors();
        }
        catch (Exception ex)
        {
            Log.Error($"UcMotorViewModel [{MotorData.MotorName}] Refresh Error: {ex.Message}");
        }
    }

    private void UpdateStatusAndColors()
    {
        if (_device == null) return;

        // 1. Mode of Operation Logic
        var mode = _device.Data.ModeOfOperationDisplay;
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

        // 2. Statusword Logic
        ushort sw = _device.Data.Statusword;
        bool foundStatus = false;

        if ((sw & 0x0437) == 0x0037)
        {
            StatusText = "Movement";
            StatusColor = Brushes.Orange;
            foundStatus = true;
        }
        else if ((sw & 0x006f) == 0x0008)
        {
            StatusText = "Fault";
            StatusColor = Brushes.Red;
            foundStatus = true;
        }
        else if ((sw & 0x006f) == 0x0007)
        {
            StatusText = "Quick stop";
            StatusColor = Brushes.Yellow;
            foundStatus = true;
        }
        else if ((sw & 0x0040) == 0x0040)
        {
            StatusText = "Disable";
            StatusColor = Brushes.BlueViolet;
            foundStatus = true;
        }
        else if ((sw & 0x0437) == 0x0437)
        {
            StatusText = "Enable";
            StatusColor = Brushes.LimeGreen;
            foundStatus = true;
        }

        if (!foundStatus)
        {
            StatusText = "Unknown";
            StatusColor = Brushes.Brown;
            ModeColor = Brushes.Brown;
            RemoteColor = Brushes.Brown;
        }
        else
        {
            // 3. Remote Status
            RemoteColor = _device.Data.RemoteStatus ? Brushes.Green : Brushes.Red;
        }
    }

    public void Dispose()
    {
        StopRefresh();
    }
}
