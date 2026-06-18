using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Lisovanie.Models;
using System;
using System.Linq;

namespace Lisovanie.Views;

public partial class frmChangePassword : Window
{
    private readonly CMainProgram? _mainProgram;
    private bool _oldVerified;

    public frmChangePassword()
    {
        InitializeComponent();
    }

    public frmChangePassword(CMainProgram mainProgram)
    {
        InitializeComponent();
        _mainProgram = mainProgram;
        Pad.Mask = true;
        Pad.SetTitle("Staré heslo");
    }

    // 1. fáza – overenie starého hesla
    private void VerifyOld()
    {
        if (_mainProgram != null && Pad.CurrentText == _mainProgram.ParametersMain.Password)
        {
            _oldVerified = true;
            ErrorText.Foreground = Brushes.LightGreen;
            ErrorText.Text = "Zadajte nové heslo";
            Pad.SetTitle("Nové heslo (4 číslice)");
            Pad.ClearInput();
        }
        else
        {
            ErrorText.Foreground = Brushes.OrangeRed;
            ErrorText.Text = "Nesprávne staré heslo";
            Pad.ClearInput();
        }
    }

    // 2. fáza – uloženie nového hesla
    private void SaveNew()
    {
        var text = Pad.CurrentText;
        if (text.Length != 4 || !text.All(char.IsDigit))
        {
            ErrorText.Foreground = Brushes.OrangeRed;
            ErrorText.Text = "Zadajte 4 číslice";
            return;
        }

        if (_mainProgram != null)
        {
            _mainProgram.ParametersMain.Password = text;
            _mainProgram.SaveParametersMain();
        }

        Close(true);
    }

    private void BtnSave_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!_oldVerified)
        {
            ErrorText.Foreground = Brushes.OrangeRed;
            ErrorText.Text = "Najprv overte staré heslo (OK)";
            return;
        }

        SaveNew();
    }

    private void Pad_OnConfirmed(object? sender, EventArgs e)
    {
        if (!_oldVerified)
            VerifyOld();
        else
            SaveNew();
    }

    private void Pad_OnCancelled(object? sender, EventArgs e) => Close(false);

    private void BtnCancel_OnClick(object? sender, RoutedEventArgs e) => Close(false);
}
