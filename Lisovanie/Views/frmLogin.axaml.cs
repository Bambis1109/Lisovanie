using Avalonia.Controls;
using Avalonia.Interactivity;
using Lisovanie.Models;
using System;

namespace Lisovanie.Views;

public partial class frmLogin : Window
{
    private readonly CMainProgram? _mainProgram;

    public frmLogin()
    {
        InitializeComponent();
    }

    public frmLogin(CMainProgram mainProgram)
    {
        InitializeComponent();
        _mainProgram = mainProgram;
        Pad.Mask = true;
        Pad.SetTitle("Zadajte heslo správcu");
    }

    private void Pad_OnConfirmed(object? sender, EventArgs e)
    {
        if (_mainProgram != null && Pad.CurrentText == _mainProgram.ParametersMain.Password)
        {
            Close(true);
        }
        else
        {
            ErrorText.Text = "Nesprávne heslo";
            Pad.ClearInput();
        }
    }

    private void Pad_OnCancelled(object? sender, EventArgs e) => Close(false);

    private void BtnCancel_OnClick(object? sender, RoutedEventArgs e) => Close(false);

    private async void BtnChange_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_mainProgram == null) return;
        var dlg = new frmChangePassword(_mainProgram);
        await dlg.ShowDialog<bool>(this);
    }
}
