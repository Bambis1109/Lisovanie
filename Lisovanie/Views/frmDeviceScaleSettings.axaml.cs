// ==========================================
// Súbor: Lisovanie\Views\frmDeviceScaleSettings.axaml.cs
// ==========================================

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using EposCmd.Net;
using Lisovanie.Models;
using Lisovanie.ViewModels;
using Lisovanie.Logging;
using Serilog;

namespace Lisovanie.Views;

public partial class frmDeviceScaleSettings : Window
{
    public frmDeviceScaleSettings()
    {
        InitializeComponent();
    }

    // ZMENA: Pridaný parameter CControlScales
    public frmDeviceScaleSettings(CControlScales? controlScales, CDeviceScale deviceScale)
    {
        InitializeComponent();
        
        // ZMENA: Odovzdanie controlScales do ViewModelu
        var vm = new UcDeviceScaleViewModel(controlScales, deviceScale, deviceScale.Name);
        
        vm.IsSetupVisible = false;
        vm.StartRefresh();
        DataContext = vm;
        
        Title = $"Scale Setup - {deviceScale.Name}";
    }

    private void BtnClose_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void TabControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is TabControl tc &&
            tc.SelectedItem is TabItem { Header: "Vibro" } &&
            DataContext is UcDeviceScaleViewModel vm)
        {
            await vm.VibroLoadFromCanAsync();
        }
    }

    private void BtnAllParams_OnClick(object? sender, RoutedEventArgs e)
        => OpenParametersWindow(davkaOnly: false);

    private void BtnDavkaParams_OnClick(object? sender, RoutedEventArgs e)
        => OpenParametersWindow(davkaOnly: true);

    private void OpenParametersWindow(bool davkaOnly)
    {
        if (DataContext is UcDeviceScaleViewModel vm && vm.Device != null)
        {
            var parameters = new DeviceParameters();
            var paramsVm = new ParametersViewModel(parameters, vm.Device, davkaOnly);
            var window = new ParametersWindow
            {
                DataContext = paramsVm,
                Title = davkaOnly ? "Parametre dávky" : "Parametre zariadenia"
            };
            window.ShowDialog(this);
        }
    }

    private async void BtnProcessLog_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Načítať log z dávkovača",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Log súbory") { Patterns = new[] { "*.log", "*.txt" } },
                new FilePickerFileType("Všetky súbory") { Patterns = new[] { "*.*" } }
            }
        });

        if (files.Count > 0)
        {
            var filePath = files[0].Path.LocalPath;

            if (!File.Exists(filePath))
            {
                Log.Error("Súbor neexistuje: {FilePath}", filePath);
                return;
            }

            try
            {
                // Zavoláme našu konverznú metódu
                bool ok = LogToCsvConverter.ProcessLogFile(filePath);

                if (!ok)
                {
                    Log.Error("Spracovanie logu zlyhalo. Skontrolujte formát súboru: {FilePath}", filePath);
                }
                else
                {
                    string csvPath = Path.ChangeExtension(filePath, ".csv");
                    Log.Information("Log úspešne spracovaný a uložený ako: {CsvPath}", csvPath);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Kritická chyba pri spracovaní logu: {Message}", ex.Message);
            }
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (DataContext is UcDeviceScaleViewModel vm)
        {
            vm.StopRefresh();
        }
    }
}