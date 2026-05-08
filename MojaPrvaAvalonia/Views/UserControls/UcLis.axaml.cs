using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using MojaPrvaAvalonia.Models;

namespace MojaPrvaAvalonia.Views.UserControls;

public partial class UcLis : UserControl
{
    private frmLisSetup? _setupWindow;

    public UcLis()
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

        if (DataContext is CLis lis)
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