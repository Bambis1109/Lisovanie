using Avalonia.Controls;
using Avalonia.Interactivity;
using Lisovanie.Models;
using System;
using System.Threading.Tasks;

namespace Lisovanie.Views;

public partial class FrmManipulatorSettingsPar : Window
{
    private CControlManipulator? _controlManipulator;

    public FrmManipulatorSettingsPar()
    {
        InitializeComponent();
    }

    public FrmManipulatorSettingsPar(CControlManipulator controlManipulator)
    {
        InitializeComponent();
        _controlManipulator = controlManipulator;
        DataContext = controlManipulator.deltaRobot.ParametersDelta;
    }

    private void BtnClose_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnSave_OnClick(object? sender, RoutedEventArgs e)
    {
        _controlManipulator?.SaveParameters();
    }

    private void BtnLoad_OnClick(object? sender, RoutedEventArgs e)
    {
        _controlManipulator?.LoadParameters();
    }

    private async Task EditValueIntAsync(string title, int initialValue, Action<int> setter)
    {
        var numpad = new NumpadWindow(title, initialValue);
        var result = await numpad.ShowDialog<bool>(this);
        if (result)
        {
            setter((int)numpad.ResultValue);
        }
    }

    // --- Matica OK ---
    private async void BtnOkXfirst_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParameters p) await EditValueIntAsync("Matica OK - X prvý", p.MatrixOkXfirst, v => p.MatrixOkXfirst = v); }
    private async void BtnOkYfirst_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParameters p) await EditValueIntAsync("Matica OK - Y prvý", p.MatrixOkYfirst, v => p.MatrixOkYfirst = v); }
    private async void BtnOkXdelta_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParameters p) await EditValueIntAsync("Matica OK - X delta", p.MatrixOkXdelta, v => p.MatrixOkXdelta = v); }
    private async void BtnOkYdelta_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParameters p) await EditValueIntAsync("Matica OK - Y delta", p.MatrixOkYdelta, v => p.MatrixOkYdelta = v); }
    private async void BtnOkXnum_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParameters p) await EditValueIntAsync("Matica OK - Počet X", p.MatrixOkXnum, v => p.MatrixOkXnum = v); }
    private async void BtnOkYnum_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParameters p) await EditValueIntAsync("Matica OK - Počet Y", p.MatrixOkYnum, v => p.MatrixOkYnum = v); }

    // --- Matica NOK ---
    private async void BtnNokXfirst_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParameters p) await EditValueIntAsync("Matica NOK - X prvý", p.MatrixNokXfirst, v => p.MatrixNokXfirst = v); }
    private async void BtnNokYfirst_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParameters p) await EditValueIntAsync("Matica NOK - Y prvý", p.MatrixNokYfirst, v => p.MatrixNokYfirst = v); }
    private async void BtnNokXdelta_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParameters p) await EditValueIntAsync("Matica NOK - X delta", p.MatrixNokXdelta, v => p.MatrixNokXdelta = v); }
    private async void BtnNokYdelta_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParameters p) await EditValueIntAsync("Matica NOK - Y delta", p.MatrixNokYdelta, v => p.MatrixNokYdelta = v); }
    private async void BtnNokXnum_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParameters p) await EditValueIntAsync("Matica NOK - Počet X", p.MatrixNokXnum, v => p.MatrixNokXnum = v); }
    private async void BtnNokYnum_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParameters p) await EditValueIntAsync("Matica NOK - Počet Y", p.MatrixNokYnum, v => p.MatrixNokYnum = v); }

    // --- Delta ---
    private async void BtnOffsetArm_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParameters p) await EditValueIntAsync("OffsetArm", p.OffsetArm, v => p.OffsetArm = v); }
    private async void BtnOffsetSystem_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParameters p) await EditValueIntAsync("OffsetSystem", p.OffsetSystem, v => p.OffsetSystem = v); }
}
