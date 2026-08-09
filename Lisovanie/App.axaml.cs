using Avalonia;
using Avalonia.Controls;
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
            // Medzi zavretím dialógu výberu receptu a otvorením hlavného okna by
            // predvolený režim ukončil aplikáciu, lebo nezostane otvorené žiadne okno.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 2. Najprv rýchlo zobrazíme úvodný (splash) dialóg.
            var splash = new SplashWindow();
            splash.Show();

            // 3. Výber receptu a stavbu hlavného okna odložíme, aby sa splash stihol vykresliť.
            //    Po otvorení MainWindow splash zatvoríme a zaregistrujeme aktivátor
            //    pre prípad spustenia ďalšej inštancie.
            Dispatcher.UIThread.Post(async () =>
            {
                var manager = _mainProgram.RecipeManager;
                manager.MigrateIfNeeded();

                // Splash je Topmost a bez dekorácií - inak by prekryl modálny dialóg.
                splash.Topmost = false;

                // Ak sa zvolený recept nepodarí načítať, ponúkneme výber znovu.
                while (true)
                {
                    var select = new frmRecipeSelect(manager, _mainProgram.ParametersMain.ActiveRecipe);
                    var chosen = await select.ShowDialog<string?>(splash);

                    if (string.IsNullOrEmpty(chosen))
                    {
                        splash.Close();
                        desktop.Shutdown();
                        return;
                    }

                    if (manager.Apply(chosen))
                    {
                        _mainProgram.ParametersMain.ActiveRecipe = chosen;
                        _mainProgram.SaveParametersMain();
                        break;
                    }
                }

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
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            }, DispatcherPriority.Background);
        }

        base.OnFrameworkInitializationCompleted();
    }
}