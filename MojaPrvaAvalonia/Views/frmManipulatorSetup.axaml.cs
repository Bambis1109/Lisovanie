using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MojaPrvaAvalonia.Models;

namespace MojaPrvaAvalonia.Views;

public partial class frmManipulatorSetup : Window
{
    public frmManipulatorSetup()
    {
        InitializeComponent();
    }

    public frmManipulatorSetup(CManipulator manipulator)
    {
        InitializeComponent();
        DataContext = manipulator;
    }

    private void BtnClose_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}