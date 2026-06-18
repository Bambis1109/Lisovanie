using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Specialized;
using Lisovanie.ViewModels;

namespace Lisovanie.Views;

public partial class MainWindow : Window
{
    private frmZoneSetup? _zoneSetupWindow;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is MainWindowViewModel vm)
        {
            vm.VypisLogov.CollectionChanged += (sender, args) =>
            {
                if (args.NewItems != null && args.NewItems.Count > 0)
                    LogListBox.ScrollIntoView(args.NewItems[0]);
            };
        }
    }

    private async void BtnMode_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        // V režime Spravca prepneme späť na Operator bez dialógu.
        if (vm.MainProgram.IsSpravca)
        {
            vm.MainProgram.IsSpravca = false;
            return;
        }

        // V režime Operator vyžiadame heslo.
        var dlg = new frmLogin(vm.MainProgram);
        var ok = await dlg.ShowDialog<bool>(this);
        if (ok)
        {
            vm.MainProgram.IsSpravca = true;
        }
    }

    private void BtnZoneSetup_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_zoneSetupWindow != null)
        {
            _zoneSetupWindow.Activate();
            return;
        }

        if (DataContext is MainWindowViewModel vm)
        {
            _zoneSetupWindow = new frmZoneSetup(vm);
            _zoneSetupWindow.Closed += (s, args) => _zoneSetupWindow = null;
            _zoneSetupWindow.Show(this);
        }
    }
}