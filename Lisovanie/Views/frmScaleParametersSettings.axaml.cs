using Avalonia.Controls;
using Avalonia.Interactivity;
using Lisovanie.Models;

namespace Lisovanie.Views;

public partial class frmScaleParametersSettings : Window
{
    public frmScaleParametersSettings()
    {
        InitializeComponent();
    }

    public frmScaleParametersSettings(CControlScales controlScales)
    {
        InitializeComponent();
        DataContext = controlScales;
    }

    private void BtnSave_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CControlScales scales)
            scales.SaveParametersCommand.Execute(null);
        Close();
    }

    private void BtnClose_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
