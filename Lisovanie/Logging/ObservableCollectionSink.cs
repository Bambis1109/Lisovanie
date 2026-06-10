using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using System.IO;

namespace Lisovanie.Logging;

public class ObservableCollectionSink : ILogEventSink
{
    private readonly ITextFormatter _textFormatter;
    public ObservableCollection<string> LogEvents { get; }

    // Konštruktor, kde si nastavíme formát výpisu
    public ObservableCollectionSink(string outputTemplate)
    {
        _textFormatter = new MessageTemplateTextFormatter(outputTemplate, null);
        LogEvents = new ObservableCollection<string>();
    }

    // Táto metóda sa zavolá zakaždým, keď urobíš Log.Information() atď.
    public void Emit(LogEvent logEvent)
    {
        using var renderSpace = new StringWriter();
        _textFormatter.Format(logEvent, renderSpace);
        var message = renderSpace.ToString().TrimEnd();

        // Avalonia UI beží vo vlastnom vlákne. Keďže PLC a Serilog bežia na pozadí,
        // musíme aktualizáciu kolekcie poslať (Dispatch) do hlavného UI vlákna.
        Dispatcher.UIThread.Post(() =>
        {
            LogEvents.Add(message);

            // Voliteľné: Ak chceš udržať napr. len posledných 100 logov v pamäti (ako si mal 1000 vo WinForms)
            if (LogEvents.Count > 1000)
            {
                LogEvents.RemoveAt(0);
            }
        });
    }
}