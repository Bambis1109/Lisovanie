using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Lisovanie.Models;      // Pridané: aby sme videli triedu CMainProgram
using Lisovanie.ViewModels;
using Lisovanie.Views;

namespace Lisovanie;

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
        // Set the global MainProgram instance here so that CPlc and other classes can access it statically
        typeof(Program).GetProperty(nameof(Program.MainProgram))?.SetValue(null, _mainProgram);

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