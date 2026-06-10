using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using Lisovanie.Models;

namespace Lisovanie.Views.UserControls;

public partial class UcControlLis : UserControl
{
    private frmLisSetup? _setupWindow;

    public UcControlLis()
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

        if (DataContext is CControlLis lis)
        {
            _setupWindow = new frmLisSetup(lis);
            
            // Keď sa okno zatvorí, vymažeme referenciu, aby sa dalo znova otvoriť
            _setupWindow.Closed += (s, args) => _setupWindow = null;
            
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