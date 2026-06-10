using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using MojaPrvaAvalonia.ViewModels;
using Serilog;

namespace MojaPrvaAvalonia.Views;

public partial class frmZoneSetup : Window
{
    private SerialPort? _port;
    private readonly StringBuilder _rxBuffer = new();
    private readonly int[] _baudRates =[1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600, 2000000];

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
        CbxBaud.ItemsSource = _baudRates;
        CbxBaud.Text = "9600";
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
        Dispatcher.UIThread.Post(() =>
        {
            var timestamp = ChkTimestamp.IsChecked == true
                ? $"[{DateTime.Now:HH:mm:ss.fff}] "
                : string.Empty;

            var current = TxtReceived.Text ?? string.Empty;
            TxtReceived.Text = current + timestamp + text + "\n";

            if (ChkAutoScroll.IsChecked == true)
                TxtReceived.CaretIndex = TxtReceived.Text.Length;
        });
    }

    private void BtnClear_OnClick(object? sender, RoutedEventArgs e)
    {
        TxtReceived.Text = string.Empty;
    }

    private async void BtnSaveLog_OnClick(object? sender, RoutedEventArgs e)
    {
        var content = TxtReceived.Text;
        if (string.IsNullOrEmpty(content)) return;

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
            await writer.WriteAsync(content);
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
        if (_port is not { IsOpen: true })
        {
            AppendLine("[Nie je pripojené]");
            return;
        }

        var input = TxtSend.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(input)) return;

        try
        {
            if (ChkSendHex.IsChecked == true)
            {
                var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var bytes = new byte[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                    bytes[i] = Convert.ToByte(parts[i], 16);
                _port.Write(bytes, 0, bytes.Length);
                AppendLine($"[TX HEX] {BitConverter.ToString(bytes).Replace("-", " ")}");
            }
            else
            {
                _port.Write(input);
                AppendLine($"[TX] {input}");
            }
        }
        catch (Exception ex)
        {
            AppendLine($"[TX CHYBA: {ex.Message}]");
        }
    }

    private void BtnClose_OnClick(object? sender, RoutedEventArgs e)
    {
        Disconnect();
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        Disconnect();
        base.OnClosed(e);
    }
}
