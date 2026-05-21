// ==========================================
// Súbor: MojaPrvaAvalonia\Views\frmDeviceScaleSettings.axaml.cs
// ==========================================

using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using EposCmd.Net;
using MojaPrvaAvalonia.Models;
using MojaPrvaAvalonia.ViewModels;

namespace MojaPrvaAvalonia.Views;

public partial class frmDeviceScaleSettings : Window
{
    public frmDeviceScaleSettings()
    {
        InitializeComponent();
    }

    // ZMENA: Pridaný parameter CControlScales
    public frmDeviceScaleSettings(CControlScales? controlScales, CDeviceScale deviceScale)
    {
        InitializeComponent();
        
        // ZMENA: Odovzdanie controlScales do ViewModelu
        var vm = new UcDeviceScaleViewModel(controlScales, deviceScale, deviceScale.Name);
        
        vm.IsSetupVisible = false;
        vm.StartRefresh();
        DataContext = vm;
        
        Title = $"Scale Setup - {deviceScale.Name}";
    }

    private void BtnClose_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnAllParams_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is UcDeviceScaleViewModel vm && vm.Device != null)
        {
            var parameters = new DeviceParameters();
            var paramsVm = new ParametersViewModel(parameters, vm.Device);
            var window = new ParametersWindow
            {
                DataContext = paramsVm
            };
            window.ShowDialog(this);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (DataContext is UcDeviceScaleViewModel vm)
        {
            vm.StopRefresh();
        }
    }
}