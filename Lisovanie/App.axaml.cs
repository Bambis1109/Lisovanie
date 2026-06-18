using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
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
            // 2. Najprv rýchlo zobrazíme úvodný (splash) dialóg.
            var splash = new SplashWindow();
            splash.Show();

            // 3. Stavbu hlavného okna odložíme, aby sa splash stihol vykresliť.
            //    Po otvorení MainWindow splash zatvoríme a zaregistrujeme aktivátor
            //    pre prípad spustenia ďalšej inštancie.
            Dispatcher.UIThread.Post(() =>
            {
                var mainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(_mainProgram),
                };

                mainWindow.Opened += (_, _) =>
                {
                    SingleInstance.RegisterActivator(mainWindow);
                    splash.Close();
                };

                desktop.MainWindow = mainWindow;
                mainWindow.Show();
            }, DispatcherPriority.Background);
        }

        base.OnFrameworkInitializationCompleted();
    }
}