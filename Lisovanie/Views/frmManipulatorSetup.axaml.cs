using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Lisovanie.Models;

namespace Lisovanie.Views;

public partial class frmManipulatorSetup : Window
{
    public frmManipulatorSetup()
    {
        InitializeComponent();
    }

    public frmManipulatorSetup(CControlManipulator controlManipulator)
    {
        InitializeComponent();
        DataContext = controlManipulator;
    }

    private void BtnClose_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}