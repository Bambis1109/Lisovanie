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

    private void BtnClose_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void BtnSettings_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is CLis lis)
        {
            var frm = new FrmLisSettingsPar(lis);
            frm.ShowDialog(this);
        }
    }
}