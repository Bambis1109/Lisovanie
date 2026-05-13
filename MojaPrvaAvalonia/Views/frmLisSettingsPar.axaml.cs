using Avalonia.Controls;
using Avalonia.Interactivity;
using MojaPrvaAvalonia.Models;

namespace MojaPrvaAvalonia.Views;

public partial class FrmLisSettingsPar : Window
{
    private CLis? _lis;

    public FrmLisSettingsPar()
    {
        InitializeComponent();
    }

    public FrmLisSettingsPar(CLis lis)
    {
        InitializeComponent();
        _lis = lis;
        DataContext = lis.ParametersLis;
    }

    private void BtnClose_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnSave_OnClick(object? sender, RoutedEventArgs e)
    {
        _lis?.SaveParameters();
    }

    private void BtnLoad_OnClick(object? sender, RoutedEventArgs e)
    {
        _lis?.LoadParameters();
    }

    private void BtnRecalculate_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CParametersLis p)
        {
            p.ParLis.RecalculateCalibration();
        }
    }
}