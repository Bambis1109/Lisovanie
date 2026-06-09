using Avalonia.Controls;
using Avalonia.Interactivity;
using MojaPrvaAvalonia.ViewModels;

namespace MojaPrvaAvalonia.Views;

public partial class frmZoneSetup : Window
{
    public frmZoneSetup()
    {
        InitializeComponent();
    }

    public frmZoneSetup(MainWindowViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void BtnClose_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
