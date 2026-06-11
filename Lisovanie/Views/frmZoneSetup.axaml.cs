using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Lisovanie.Logging;
using Lisovanie.ViewModels;
using Serilog;

namespace Lisovanie.Views;

public partial class frmZoneSetup : Window
{
    private SerialPort? _port;
    private readonly StringBuilder _rxBuffer = new();
    private readonly ObservableCollection<string> _lines = new();
    private readonly List<string> _pendingLines = new();
    private DispatcherTimer? _flushTimer;

    private static readonly Dictionary<string, string[]> ModuleCommands = new()
    {
        ["System"]   = ["save", "load", "restart", "help", "list", "slovnik"],
        ["Master"]   = ["ma_init", "ma_prod", "ma_next", "ma_stop"],
        ["Zamok"]    = ["za_init", "za_odomkni", "za_zamkni", "za_vysyp_vlavo", "za_vysyp_pravo", "za_kalibruj"],
        ["Vaha"]     = ["va_init", "va_nula", "va_max", "va_tara"],
        ["Vyloznik"] = ["vy_init", "vy_vysun1", "vy_vysun2", "vy_vyloz1", "vy_vyloz2", "vy_vysyp", "vy_zasun"],
        ["Podavac"]  = ["po_init", "po_podaj", "po_podavaj", "po_velocity", "po_stop"],
        ["Davkovac"] = ["da_init", "da_tune", "da_prod", "da_stop", "da_vyklop"],
    };
    private readonly int[] _baudRates = [1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600, 2000000];

    public frmZoneSetup()
    {
        InitializeComponent();
        InitSniffer();
    }

    public frmZoneSetup(MainWindowViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        InitSniffer();
    }

    private void InitSniffer()
    {
        LstReceived.ItemsSource = _lines;

        _flushTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _flushTimer.Tick += FlushPending;
        _flushTimer.Start();

        CbxBaud.ItemsSource = _baudRates;
        CbxBaud.Text = "2000000";
        CbxModule.ItemsSource = new List<string>(ModuleCommands.Keys);
        CbxModule.SelectedIndex = 0;
        RefreshPorts();
    }

    private void RefreshPorts()
    {
        var ports = SerialPort.GetPortNames();
        CbxPort.ItemsSource = ports;
        if (ports.Length > 0)
            CbxPort.SelectedIndex = 0;
    }

    private void BtnRefreshPorts_OnClick(object? sender, RoutedEventArgs e) => RefreshPorts();

    private void CbxModule_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (CbxModule.SelectedItem is string module && ModuleCommands.TryGetValue(module, out var cmds))
        {
            CbxCommand.ItemsSource = cmds;
            CbxCommand.SelectedIndex = 0;
        }
    }

    private void BtnSendCommand_OnClick(object? sender, RoutedEventArgs e)
    {
        if (CbxCommand.SelectedItem is not string cmd) return;
        SendText(cmd);
    }

    private void BtnConnect_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_port is { IsOpen: true })
        {
            Disconnect();
            return;
        }

        var portName = CbxPort.SelectedItem as string;
        if (string.IsNullOrEmpty(portName)) return;
        if (!int.TryParse(CbxBaud.Text, out var baud) || baud <= 0)
        {
            AppendLine("[CHYBA: neplatný baud rate]");
            return;
        }

        try
        {
            _port = new SerialPort(portName, baud)
            {
                ReadTimeout = 500,
                WriteTimeout = 500
            };
            _port.DataReceived += OnDataReceived;
            _port.Open();
            BtnConnect.Content = "Odpojiť";
            BtnConnect.Background = Avalonia.Media.Brushes.DarkRed;
            AppendLine($"[Pripojený {portName} @ {baud}]");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "COM port open failed");
            AppendLine($"[CHYBA: {ex.Message}]");
        }
    }

    private void Disconnect()
    {
        if (_port == null) return;
        _port.DataReceived -= OnDataReceived;
        try { _port.Close(); } catch { /* ignored */ }
        _port.Dispose();
        _port = null;
        BtnConnect.Content = "Pripojiť";
        BtnConnect.Background = Avalonia.Media.Brushes.SteelBlue;
        AppendLine("[Odpojený]");
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_port is not { IsOpen: true }) return;
        try
        {
            var n = _port.BytesToRead;
            var buf = new byte[n];
            _port.Read(buf, 0, n);

            Dispatcher.UIThread.Post(() =>
            {
                if (ChkHex.IsChecked == true)
                {
                    AppendLine(BitConverter.ToString(buf).Replace("-", " "));
                    return;
                }

                _rxBuffer.Append(Encoding.ASCII.GetString(buf));
                var raw = _rxBuffer.ToString();
                var lines = raw.Split('\n');

                for (int i = 0; i < lines.Length - 1; i++)
                {
                    var line = lines[i].TrimEnd('\r');
                    if (line.Length > 0)
                        AppendLine(line);
                }

                _rxBuffer.Clear();
                _rxBuffer.Append(lines[^1]);
            });
        }
        catch (Exception ex)
        {
            AppendLine($"[RX CHYBA: {ex.Message}]");
        }
    }

    private void AppendLine(string text)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => AppendLine(text));
            return;
        }

        var timestamp = ChkTimestamp.IsChecked == true
            ? $"[{DateTime.Now:HH:mm:ss.fff}] "
            : string.Empty;
        _pendingLines.Add(timestamp + text);
    }

    private void FlushPending(object? sender, EventArgs e)
    {
        if (_pendingLines.Count == 0) return;
        foreach (var line in _pendingLines)
            _lines.Add(line);
        _pendingLines.Clear();

        if (ChkAutoScroll.IsChecked == true && _lines.Count > 0)
            LstReceived.ScrollIntoView(_lines[^1]);
    }

    private void BtnClear_OnClick(object? sender, RoutedEventArgs e)
    {
        _pendingLines.Clear();
        _lines.Clear();
    }

    private async void BtnSaveLog_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_lines.Count == 0) return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Uložiť log",
            SuggestedFileName = $"com_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
            FileTypeChoices =
            [
                new FilePickerFileType("Text") { Patterns = ["*.txt"] },
                new FilePickerFileType("Všetky súbory") { Patterns = ["*"] }
            ]
        });

        if (file is null) return;

        try
        {
            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream, Encoding.UTF8);
            foreach (var line in _lines)
                await writer.WriteLineAsync(line);
            AppendLine($"[Log uložený: {file.Name}]");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Save log failed");
            AppendLine($"[CHYBA uloženia: {ex.Message}]");
        }
    }

    private void BtnSend_OnClick(object? sender, RoutedEventArgs e)
    {
        var input = TxtSend.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(input)) return;

        if (ChkSendHex.IsChecked == true)
        {
            try
            {
                var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var bytes = new byte[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                    bytes[i] = Convert.ToByte(parts[i], 16);
                SendBytes(bytes);
            }
            catch (Exception ex)
            {
                AppendLine($"[TX CHYBA: {ex.Message}]");
            }
        }
        else
        {
            SendText(input);
        }
    }

    private void SendText(string text)
    {
        if (_port is not { IsOpen: true }) { AppendLine("[Nie je pripojené]"); return; }
        try
        {
            _port.Write(text);
            AppendLine($"[TX] {text}");
        }
        catch (Exception ex) { AppendLine($"[TX CHYBA: {ex.Message}]"); }
    }

    private void SendBytes(byte[] bytes)
    {
        if (_port is not { IsOpen: true }) { AppendLine("[Nie je pripojené]"); return; }
        try
        {
            _port.Write(bytes, 0, bytes.Length);
            AppendLine($"[TX HEX] {BitConverter.ToString(bytes).Replace("-", " ")}");
        }
        catch (Exception ex) { AppendLine($"[TX CHYBA: {ex.Message}]"); }
    }

    private async void BtnProcessForAi_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_lines.Count == 0)
        {
            AppendLine("[Terminál je prázdny]");
            return;
        }

        var messages = _lines
            .Select(l => StripTerminalPrefix(l.Trim()))
            .Where(m => m != null)
            .Select(m => m!);

        var result = LogToCsvConverter.ProcessLines(messages);
        if (result == null || result.Count == 0)
        {
            AppendLine("[Žiadne kompletné dávky na spracovanie]");
            return;
        }

        var content = string.Join(Environment.NewLine, result);

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Uložiť log pre AI",
            SuggestedFileName = $"ai_log_{DateTime.Now:yyyyMMdd_HHmmss}_AI_Optimized.txt",
            FileTypeChoices =
            [
                new FilePickerFileType("Text") { Patterns = ["*.txt"] },
                new FilePickerFileType("Všetky súbory") { Patterns = ["*"] }
            ]
        });

        if (file is null) return;

        try
        {
            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream, Encoding.UTF8);
            await writer.WriteAsync(content);
            AppendLine($"[AI log uložený: {file.Name}  ({result.Count} riadkov)]");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "AI log save failed");
            AppendLine($"[CHYBA uloženia: {ex.Message}]");
        }
    }

    // Odstraňuje terminálový prefix [HH:MM:SS.mmm] [ Nms] [LEVEL]
    // a vracia čistú správu zariadenia (rovnaký vstup ako ProcessLogFile).
    private static string? StripTerminalPrefix(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || !line.StartsWith('[')) return null;

        var i1 = line.IndexOf(']');
        if (i1 < 0) return null;
        var rest = line[(i1 + 1)..].TrimStart();

        if (!rest.StartsWith('[') || rest.StartsWith("[TX]")) return null;

        var i2 = rest.IndexOf(']');
        if (i2 < 0) return null;
        var rest2 = rest[(i2 + 1)..].TrimStart();

        if (!rest2.StartsWith('[')) return null;
        var i3 = rest2.IndexOf(']');
        if (i3 < 0) return null;

        var msg = rest2[(i3 + 1)..].TrimStart();
        return msg.Length > 0 ? msg : null;
    }

    private void BtnClose_OnClick(object? sender, RoutedEventArgs e)
    {
        Disconnect();
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _flushTimer?.Stop();
        Disconnect();
        base.OnClosed(e);
    }
}
