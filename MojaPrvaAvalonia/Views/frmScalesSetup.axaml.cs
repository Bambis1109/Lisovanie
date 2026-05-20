using Avalonia.Controls;
using MojaPrvaAvalonia.Models;

namespace MojaPrvaAvalonia.Views;

public partial class frmControlScalesSetup : Window
{
    public frmControlScalesSetup()
    {
        InitializeComponent();
    }

    public frmControlScalesSetup(CControlScales controlScales)
    {
        InitializeComponent();
        DataContext = controlScales;
    }

    private void BtnClose_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}