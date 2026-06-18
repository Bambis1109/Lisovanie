using Avalonia.Controls;
using System;
using System.Globalization;

namespace Lisovanie.Views;

public partial class NumpadWindow : Window
{
    public double ResultValue { get; private set; }

    public NumpadWindow()
    {
        InitializeComponent();
    }

    public NumpadWindow(string title, double initialValue)
    {
        InitializeComponent();
        Pad.Initialize(title, initialValue.ToString(CultureInfo.InvariantCulture));
    }

    private void OnConfirmed(object? sender, EventArgs e)
    {
        if (double.TryParse(Pad.CurrentText, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
        {
            ResultValue = result;
            Close(true);
        }
    }

    private void OnCancelled(object? sender, EventArgs e)
    {
        Close(false);
    }
}
