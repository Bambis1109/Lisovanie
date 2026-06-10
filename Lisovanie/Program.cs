using System;
using Avalonia;
using Serilog;
using Serilog.Core;
using Lisovanie.Logging; // Nezabudni pridať cestu k nášmu novému Sinku
using Lisovanie.Models;

namespace Lisovanie;

class Program
{
    // 1. Vytvoríme statickú premennú pre náš Sink.
    // Vďaka tomu si náš ViewModel bude vedieť neskôr vytiahnuť tie "LogEvents" a zobraziť ich.
    public static ObservableCollectionSink UiSink { get; private set; }

    // Sprístupníme MainProgram globálne, aby k nemu mali prístup PLCčka pre kontrolu hardvérového stavu.
    public static CMainProgram MainProgram { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        // Tvoja pôvodná šablóna z WinForms
        string outputTemplate = "[{Timestamp:HH:mm:ss.fff}][{Level:u3}][{Name}][{Message}][{Measure}]{NewLine}{Exception}";

        // Inicializácia nášho UI Sinku pre zoznam v aplikácii
        UiSink = new ObservableCollectionSink(outputTemplate);        
        var levelSwitch = new LoggingLevelSwitch(Serilog.Events.LogEventLevel.Verbose);
        Serilog.Debugging.SelfLog.Enable(Console.Error);

        // 2. Samotné nastavenie loggera (presne ako si mal ty, plus Console)
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            .WriteTo.Console(outputTemplate: outputTemplate) // Pre vývoj v Rideri
            // Pozor na zmenu lomítka: v Linuxe sa používajú dopredné lomítka "/"
            .WriteTo.File("logs/myapp.txt", outputTemplate: outputTemplate, rollingInterval: RollingInterval.Day) 
            .WriteTo.Sink(UiSink) // Prepojenie na našu UI kolekciu
            .CreateLogger();

        try
        {
            Log.Information("Systém logovania úspešne naštartovaný.");
            
            // Toto je pôvodný kód Avalonie, ktorý spúšťa okno
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Kritická chyba: Aplikácia nečakane spadla!");
        }
        finally
        {
            // Dôležité: Zabezpečí, že pred vypnutím sa dopíšu všetky logy do súboru
            Log.CloseAndFlush();
        }
    }

    // Nemeň metódu BuildAvaloniaApp(), nechaj ju tak, ako je
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}