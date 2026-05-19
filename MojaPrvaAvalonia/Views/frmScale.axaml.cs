using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using EposCmd.Net;
using MojaPrvaAvalonia.ViewModels;

namespace MojaPrvaAvalonia.Views;

public partial class frmScale : Window
{
    public frmScale()
    {
        InitializeComponent();
    }

    public frmScale(CDeviceScale deviceScale)
    {
        InitializeComponent();
        
        // Vytvoríme nový ViewModel pre túto inštanciu UcScale v okne.
        // Týmto sa pripájame na rovnakú fyzickú inštanciu CDeviceScale.
        var vm = new UcScaleViewModel(deviceScale, deviceScale.Name);
        
        // Skryjeme tlačidlo Setup, aby nevznikla nekonečná slučka vnorovaní
        vm.IsSetupVisible = false;
        
        // Spustíme obnovu dát z rovnakej inštancie CDeviceScale
        vm.StartRefresh();
        
        // Priradíme ViewModel do nášho UserControlu (MyUcScale)
        MyUcScale.DataContext = vm;
        
        Title = $"Scale Setup - {deviceScale.Name}";
    }

    private void BtnClose_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        // Zastavíme timer pre refresh pri zatvorení okna, 
        // aby sme neuvoľňovali zbytočne prostriedky.
        if (MyUcScale.DataContext is UcScaleViewModel vm)
        {
            vm.StopRefresh();
        }
    }
}