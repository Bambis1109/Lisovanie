using Avalonia.Controls;
using MojaPrvaAvalonia.Models;

namespace MojaPrvaAvalonia.Views;

public partial class frmScalesSetup : Window
{
    public frmScalesSetup()
    {
        InitializeComponent();
    }

    public frmScalesSetup(CScales scales)
    {
        InitializeComponent();
        DataContext = scales;
    }

    private void BtnClose_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}