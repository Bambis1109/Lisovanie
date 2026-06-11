using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
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
    private readonly record struct LogRecord(DateTime Timestamp, string Message, bool IsDevice);

    private SerialPort? _port;
    private CancellationTokenSource? _cts;
    private Task? _readTask;

    private volatile bool _hexMode;
    private volatile bool _showTimestamp = true;
    private bool _programmingScroll;

    private readonly Channel<LogRecord> _uiChannel = Channel.CreateUnbounded<LogRecord>(
        new UnboundedChannelOptions { SingleWriter = false, SingleReader = true });
    private readonly List<LogRecord> _allRecords = new();
    private readonly object _recordsLock = new();
    private readonly ObservableCollection<string> _displayLines = new();
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
        LstReceived.ItemsSource = _displayLines;
        LstReceived.AddHandler(ScrollViewer.ScrollChangedEvent, OnListScrolled, RoutingStrategies.Bubble);

        _flushTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _flushTimer.Tick += FlushUiChannel;
        _flushTimer.Start();

        CbxBaud.ItemsSource = _baudRates;
        CbxBaud.Text = "2000000";
        CbxModule.ItemsSource = new List<string>(ModuleCommands.Keys);
        CbxModule.SelectedIndex = 0;
        RefreshPorts();
    }

    // ─── Pomocné metódy ───────────────────────────────────────────────────────

    private void AddRecord(LogRecord r)
    {
        lock (_recordsLock)
        {
            _allRecords.Add(r);
            _uiChannel.Writer.TryWrite(r);
        }
    }

    private void AppendLine(string text, bool isDevice = false)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => AppendLine(text, isDevice));
            return;
        }
        AddRecord(new LogRecord(DateTime.Now, text, isDevice));
    }

    // ─── Čítacia slučka (background task) ────────────────────────────────────

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        var port = _port!;
        var buffer = new byte[65536];
        var lineBuilder = new StringBuilder();
        bool prevHex = _hexMode;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                int n = await port.BaseStream.ReadAsync(buffer, 0, buffer.Length, ct);
                if (n == 0) continue;

                var now = DateTime.Now;
                bool hex = _hexMode;

                if (hex != prevHex)
                {
                    lineBuilder.Clear();
                    prevHex = hex;
                }

                if (hex)
                {
                    AddRecord(new LogRecord(now,
                        BitConverter.ToString(buffer, 0, n).Replace("-", " "), IsDevice: true));
                    continue;
                }

                for (int i = 0; i < n; i++)
                {
                    byte b = buffer[i];
                    if (b == '\n')
                    {
                        var line = lineBuilder.ToString();
                        lineBuilder.Clear();
                        if (line.Length > 0)
                            AddRecord(new LogRecord(now, line, IsDevice: true));
                    }
                    else if (b != '\r')  // CR ignorovať vždy
                    {
                        lineBuilder.Append((char)b);
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (IOException) { }
        catch (ObjectDisposedException) { }
    }

    // ─── Scroll ───────────────────────────────────────────────────────────────

    private void OnListScrolled(object? sender, ScrollChangedEventArgs e)
    {
        // Ignorovať scrolly spôsobené kódom (RemoveAt + Add + ScrollIntoView)
        if (_programmingScroll) return;
        if (e.ExtentDelta.Y == 0 && e.OffsetDelta.Y < 0)
            ChkAutoScroll.IsChecked = false;
    }

    // ─── UI flush (DispatcherTimer tick) ─────────────────────────────────────

    private void FlushUiChannel(object? sender, EventArgs e)
    {
        var batch = new List<LogRecord>(256);
        while (_uiChannel.Reader.TryRead(out var r)) batch.Add(r);
        if (batch.Count == 0) return;

        const int MaxPerTick = 200;
        const int MaxUiLines = 2000;

        int skipped = Math.Max(0, batch.Count - MaxPerTick);
        int startIdx = skipped;
        int newCount = (batch.Count - startIdx) + (skipped > 0 ? 1 : 0);

        _programmingScroll = true;
        try
        {
            int toRemove = (_displayLines.Count + newCount) - MaxUiLines;
            for (int i = 0; i < toRemove && _displayLines.Count > 0; i++)
                _displayLines.RemoveAt(0);

            if (skipped > 0)
                _displayLines.Add($"[… preskočených {skipped} riadkov, všetky sú v pamäti]");

            bool showTs = _showTimestamp;
            for (int i = startIdx; i < batch.Count; i++)
            {
                var rec = batch[i];
                var prefix = showTs ? $"[{rec.Timestamp:HH:mm:ss.fff}] " : string.Empty;
                _displayLines.Add(prefix + rec.Message);
            }

            if (ChkAutoScroll.IsChecked == true && _displayLines.Count > 0)
                LstReceived.ScrollIntoView(_displayLines[^1]);
        }
        finally
        {
            _programmingScroll = false;
        }
    }

    // ─── Porty ────────────────────────────────────────────────────────────────

    private void RefreshPorts()
    {
        var ports = SerialPort.GetPortNames();
        CbxPort.ItemsSource = ports;
        if (ports.Length > 0)
            CbxPort.SelectedIndex = 0;
    }

    private void BtnRefreshPorts_OnClick(object? sender, RoutedEventArgs e) => RefreshPorts();

    // ─── Pripojenie / odpojenie ───────────────────────────────────────────────

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
                ReadBufferSize = 1_048_576,
                ReadTimeout    = SerialPort.InfiniteTimeout,
                WriteTimeout   = 500
            };
            _port.Open();
            _cts = new CancellationTokenSource();
            _readTask = Task.Run(() => ReadLoopAsync(_cts.Token));
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
        _cts?.Cancel();
        try { _port.Close(); } catch { /* ignorované */ }
        _port.Dispose();
        _port = null;
        _cts?.Dispose();
        _cts = null;
        _readTask = null;
        BtnConnect.Content = "Pripojiť";
        BtnConnect.Background = Avalonia.Media.Brushes.SteelBlue;
        AppendLine("[Odpojený]");
    }

    // ─── Checkboxy ────────────────────────────────────────────────────────────

    private void ChkHex_IsCheckedChanged(object? sender, RoutedEventArgs e)
        => _hexMode = ChkHex.IsChecked == true;

    private void ChkTimestamp_IsCheckedChanged(object? sender, RoutedEventArgs e)
        => _showTimestamp = ChkTimestamp.IsChecked == true;

    // ─── Moduly a príkazy ─────────────────────────────────────────────────────

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

    // ─── Odosielanie ─────────────────────────────────────────────────────────

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

    // ─── Tlačidlá pracujúce s dátami ─────────────────────────────────────────

    private void BtnClear_OnClick(object? sender, RoutedEventArgs e)
    {
        lock (_recordsLock) _allRecords.Clear();
        while (_uiChannel.Reader.TryRead(out _)) { }
        _displayLines.Clear();
    }

    private async void BtnSaveLog_OnClick(object? sender, RoutedEventArgs e)
    {
        List<LogRecord> snapshot;
        lock (_recordsLock) snapshot = new List<LogRecord>(_allRecords);
        if (snapshot.Count == 0) return;

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
            foreach (var rec in snapshot)
                await writer.WriteLineAsync($"[{rec.Timestamp:HH:mm:ss.fff}] {rec.Message}");
            AppendLine($"[Log uložený: {file.Name}]");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Save log failed");
            AppendLine($"[CHYBA uloženia: {ex.Message}]");
        }
    }

    private async void BtnProcessForAi_OnClick(object? sender, RoutedEventArgs e)
    {
        List<LogRecord> snapshot;
        lock (_recordsLock) snapshot = _allRecords.Where(r => r.IsDevice).ToList();

        if (snapshot.Count == 0)
        {
            AppendLine("[Terminál je prázdny]");
            return;
        }

        var messages = snapshot.Select(r => r.Message);
        List<string>? result;
        try
        {
            result = await Task.Run(() => LogToCsvConverter.ProcessLines(messages));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ProcessForAi failed");
            AppendLine($"[CHYBA spracovania: {ex.Message}]");
            return;
        }

        if (result == null || result.Count == 0)
        {
            AppendLine("[Žiadne kompletné dávky na spracovanie]");
            return;
        }

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
            await writer.WriteAsync(string.Join(Environment.NewLine, result));
            AppendLine($"[AI log uložený: {file.Name}  ({result.Count} riadkov)]");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "AI log save failed");
            AppendLine($"[CHYBA uloženia: {ex.Message}]");
        }
    }

    // ─── Lifecycle ────────────────────────────────────────────────────────────

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
