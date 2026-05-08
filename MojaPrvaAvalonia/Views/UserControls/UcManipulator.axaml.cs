using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using MojaPrvaAvalonia.Models;

namespace MojaPrvaAvalonia.Views.UserControls;

public partial class UcManipulator : UserControl
{
    private frmManipulatorSetup? _setupWindow;

    public UcManipulator()
    {
        InitializeComponent();
    }

    private void BtnSetup_OnClick(object? sender, RoutedEventArgs e)
    {
        // Skontrolujeme, či okno už náhodou nie je otvorené
        if (_setupWindow != null)
        {
            _setupWindow.Activate(); // Ak áno, len ho prenesieme do popredia
            return;
        }

        if (DataContext is CManipulator manipulator)
        {
            _setupWindow = new frmManipulatorSetup(manipulator);
            
            // Keď sa okno zatvorí, vymažeme referenciu, aby sa dalo znova otvoriť
            _setupWindow.Closed += (s, args) => _setupWindow = null;
            
            // Pokúsime sa nájsť rodičovské okno pre nastavenie Ownera
            var parentWindow = VisualRoot as Window;
            if (parentWindow != null)
            {
                _setupWindow.Show(parentWindow);
            }
            else
            {
                _setupWindow.Show();
            }
        }
    }
}