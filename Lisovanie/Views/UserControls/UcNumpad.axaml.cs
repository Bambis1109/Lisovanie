using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace Lisovanie.Views.UserControls;

public partial class UcNumpad : UserControl
{
    private string _currentText = "0";
    private bool _mask;

    /// <summary>Používateľ stlačil OK.</summary>
    public event EventHandler? Confirmed;

    /// <summary>Používateľ stlačil X (zrušenie).</summary>
    public event EventHandler? Cancelled;

    public UcNumpad()
    {
        InitializeComponent();
        UpdateDisplay();
    }

    /// <summary>Aktuálne zadaný text (číslo alebo heslo).</summary>
    public string CurrentText => _currentText;

    /// <summary>Maskovaný režim (zobrazí • namiesto číslic, ako pre heslo).</summary>
    public bool Mask
    {
        get => _mask;
        set
        {
            _mask = value;
            if (value && _currentText == "0") _currentText = "";
            UpdateDisplay();
        }
    }

    public void SetTitle(string title) => TitleBlock.Text = title;

    public void Initialize(string title, string initialText)
    {
        TitleBlock.Text = title;
        _currentText = initialText;
        UpdateDisplay();
    }

    public void ClearInput()
    {
        _currentText = _mask ? "" : "0";
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        DisplayBlock.Text = _mask ? new string('•', _currentText.Length) : _currentText;
    }

    private void BtnNum_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Content is string val)
        {
            if (_mask)
            {
                if (val == ".") return;
                _currentText += val;
            }
            else if (_currentText == "0" && val != ".")
            {
                _currentText = val;
            }
            else
            {
                if (val == "." && _currentText.Contains(".")) return;
                _currentText += val;
            }
            UpdateDisplay();
        }
    }

    private void BtnDel_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_currentText.Length > 1)
        {
            _currentText = _currentText.Substring(0, _currentText.Length - 1);
            if (_currentText == "-") _currentText = _mask ? "" : "0";
        }
        else
        {
            _currentText = _mask ? "" : "0";
        }
        UpdateDisplay();
    }

    private void BtnClear_OnClick(object? sender, RoutedEventArgs e)
    {
        _currentText = _mask ? "" : "0";
        UpdateDisplay();
    }

    private void BtnSign_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_mask) return;
        if (_currentText.StartsWith("-"))
        {
            _currentText = _currentText.Substring(1);
        }
        else if (_currentText != "0")
        {
            _currentText = "-" + _currentText;
        }
        UpdateDisplay();
    }

    private void BtnOk_OnClick(object? sender, RoutedEventArgs e) => Confirmed?.Invoke(this, EventArgs.Empty);

    private void BtnCancel_OnClick(object? sender, RoutedEventArgs e) => Cancelled?.Invoke(this, EventArgs.Empty);
}
