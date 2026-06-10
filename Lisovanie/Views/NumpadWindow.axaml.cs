using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Globalization;

namespace Lisovanie.Views;

public partial class NumpadWindow : Window
{
    public double ResultValue { get; private set; }
    private string _currentText = "0";

    public NumpadWindow()
    {
        InitializeComponent();
        UpdateDisplay();
    }

    public NumpadWindow(string title, double initialValue)
    {
        InitializeComponent();
        TitleBlock.Text = title;
        _currentText = initialValue.ToString(CultureInfo.InvariantCulture);
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        DisplayBlock.Text = _currentText;
    }

    private void BtnNum_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Content is string val)
        {
            if (_currentText == "0" && val != ".")
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
            if (_currentText == "-") _currentText = "0";
        }
        else
        {
            _currentText = "0";
        }
        UpdateDisplay();
    }

    private void BtnClear_OnClick(object? sender, RoutedEventArgs e)
    {
        _currentText = "0";
        UpdateDisplay();
    }

    private void BtnSign_OnClick(object? sender, RoutedEventArgs e)
    {
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

    private void BtnOk_OnClick(object? sender, RoutedEventArgs e)
    {
        if (double.TryParse(_currentText, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
        {
            ResultValue = result;
            Close(true);
        }
    }

    private void BtnCancel_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}