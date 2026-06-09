using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Specialized;
using MojaPrvaAvalonia.ViewModels;

namespace MojaPrvaAvalonia.Views;

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