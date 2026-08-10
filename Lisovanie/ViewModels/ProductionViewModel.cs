using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lisovanie.Models;
using Serilog;

namespace Lisovanie.ViewModels;

public partial class ProductionViewModel : ViewModelBase
{
    private const int LatestLimit = 200;

    private readonly CProductionLogger _logger;
    private readonly Random _rnd = new();

    /// <summary>Posledné výrobky pre tabuľku (najnovšie navrchu).</summary>
    public ObservableCollection<CProductionRecord> LatestRecords { get; } = new();

    /// <summary>Možnosti filtra statusu (index 0 = všetky).</summary>
    public string[] StatusFilterOptions { get; } = { "Všetko", "OK", "NOK" };

    [ObservableProperty] private DateTimeOffset? _filterOd;
    [ObservableProperty] private DateTimeOffset? _filterDo;
    [ObservableProperty] private int _filterStatusIndex;

    public ProductionViewModel(CProductionLogger logger)
    {
        _logger = logger;
        // Default rozsah filtra = dnešný deň.
        FilterOd = DateTimeOffset.Now;
        FilterDo = DateTimeOffset.Now;
    }

    /// <summary>Načíta tabuľku podľa aktuálneho filtra Od/Do/Status (akcia Zobraziť/Obnoviť).</summary>
    [RelayCommand]
    public async Task RefreshAsync()
    {
        var rows = await _logger.QueryAsync(BuildFilter(), LatestLimit);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            LatestRecords.Clear();
            foreach (var r in rows)
                LatestRecords.Add(r);
        });
    }

    /// <summary>Testovací nástroj: vygeneruje 10 náhodných záznamov rozložených cez posledných
    /// 7 dní a zapíše ich do DB, potom obnoví tabuľku.</summary>
    [RelayCommand]
    public async Task GenerateRandomAsync()
    {
        for (var i = 0; i < 10; i++)
        {
            var record = new CProductionRecord
            {
                TimestampUtc     = DateTime.UtcNow.AddDays(-_rnd.NextDouble() * 7),
                Hmotnost         = 10 + _rnd.NextDouble() * 40,   // 10–50 g
                Sila             = 50 + _rnd.NextDouble() * 150,  // 50–200
                Vzdialenost      = 1 + _rnd.NextDouble() * 9,     // 1–10
                CasZhutnovaniaMs = _rnd.Next(200, 2001),          // 200–2000 ms
                CasZotrvaniaMs   = _rnd.Next(100, 1001),          // 100–1000 ms
                Status           = _rnd.Next(2) == 0 ? EnProduktLis.Ok : EnProduktLis.Nok,
                Metoda           = _rnd.Next(2) == 0
                    ? EnMetodaLisovania.Sila
                    : EnMetodaLisovania.Vzdialenost
            };
            await _logger.InsertAsync(record);
        }

        Log.Information("ProductionViewModel: vygenerovaných 10 náhodných záznamov.");
        await RefreshAsync();
    }

    /// <summary>Vymaže označené záznamy z DB a obnoví tabuľku. Volá sa z code-behind so
    /// SelectedItems z DataGridu.</summary>
    public async Task DeleteSelectedAsync(IEnumerable<CProductionRecord> items)
    {
        var ids = items.Select(r => r.Id).ToArray();
        if (ids.Length == 0) return;

        await _logger.DeleteByIdsAsync(ids);
        await RefreshAsync();
    }

    /// <summary>Zostaví filter z UI hodnôt (od = začiatok dňa, do = koniec dňa).</summary>
    public CProductionFilter BuildFilter() => new()
    {
        OdLocal = FilterOd?.LocalDateTime.Date,
        DoLocal = FilterDo?.LocalDateTime.Date.AddDays(1).AddTicks(-1),
        Status = FilterStatusIndex switch
        {
            1 => EnProduktLis.Ok,
            2 => EnProduktLis.Nok,
            _ => null
        }
    };

    /// <summary>Dotiahne záznamy podľa filtra pre export.</summary>
    public Task<IReadOnlyList<CProductionRecord>> QueryForExportAsync() => _logger.QueryAsync(BuildFilter());
}
