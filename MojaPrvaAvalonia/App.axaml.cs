using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using MojaPrvaAvalonia.Models;      // Pridané: aby sme videli triedu CMainProgram
using MojaPrvaAvalonia.ViewModels;
using MojaPrvaAvalonia.Views;

namespace MojaPrvaAvalonia;

public partial class App : Application
{
    // 1. Vytvoríme našu hlavnú logiku programu, ktorá žije na úrovni celej App
    private readonly CMainProgram _mainProgram = new CMainProgram();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 2. Odovzdáme (vložíme) náš _mainProgram priamo do ViewModelu pre hlavné okno
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(_mainProgram),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}