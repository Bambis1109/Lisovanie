using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MojaPrvaAvalonia.Models;

namespace MojaPrvaAvalonia.Views;

public partial class frmLisSetup : Window
{
    public frmLisSetup()
    {
        InitializeComponent();
    }

    public frmLisSetup(CLis lis)
    {
        InitializeComponent();
        DataContext = lis;
    }

    private void BtnExit_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}