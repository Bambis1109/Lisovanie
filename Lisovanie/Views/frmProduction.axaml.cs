using System;
using System.Globalization;
using System.IO;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Lisovanie.Models;
using Lisovanie.ViewModels;
using Serilog;

namespace Lisovanie.Views;

public partial class frmProduction : Window
{
    // Slovenská kultúra: desatinná čiarka, oddeľovač CSV ';' (kompatibilné s SK Excelom).
    private static readonly CultureInfo Sk = CultureInfo.GetCultureInfo("sk-SK");

    private ProductionViewModel? ViewModel => DataContext as ProductionViewModel;

    public frmProduction()
    {
        InitializeComponent();
    }

    public frmProduction(CProductionLogger logger)
    {
        InitializeComponent();
        DataContext = new ProductionViewModel(logger);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        ViewModel?.StartRefresh();
    }

    protected override void OnClosed(EventArgs e)
    {
        ViewModel?.StopRefresh();
        base.OnClosed(e);
    }

    private void BtnClose_OnClick(object? sender, RoutedEventArgs e) => Close();

    private async void BtnExport_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is null) return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export výrobných dát",
            SuggestedFileName = $"vyroba_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            FileTypeChoices =
            [
                new FilePickerFileType("CSV") { Patterns = ["*.csv"] },
                new FilePickerFileType("Všetky súbory") { Patterns = ["*"] }
            ]
        });

        if (file is null) return;

        try
        {
            var rows = await ViewModel.QueryForExportAsync();

            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream, new UTF8Encoding(true)); // UTF-8 s BOM

            await writer.WriteLineAsync(string.Join(';',
                "Cas", "Hmotnost[g]", "Sila", "Vzdialenost", "CasZhutnovania[ms]",
                "CasZotrvania[ms]", "Status"));

            foreach (var r in rows)
            {
                await writer.WriteLineAsync(string.Join(';',
                    r.TimestampLocal.ToString("dd.MM.yyyy HH:mm:ss", Sk),
                    r.Hmotnost.ToString("F3", Sk),
                    r.Sila.ToString("F1", Sk),
                    r.Vzdialenost.ToString("F3", Sk),
                    r.CasZhutnovaniaMs.ToString(Sk),
                    r.CasZotrvaniaMs.ToString(Sk),
                    r.Status.ToString()));
            }

            Log.Information("Export výroby: {Count} záznamov -> {File}", rows.Count, file.Name);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Export výrobných dát zlyhal");
        }
    }
}
