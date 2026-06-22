using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
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
    private CancellationTokenSource? _refreshCts;

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
    }

    /// <summary>Spustí periodický refresh tabuľky posledných kusov.</summary>
    public void StartRefresh()
    {
        if (_refreshCts != null) return;
        _refreshCts = new CancellationTokenSource();
        var token = _refreshCts.Token;

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await RefreshAsync();
                }
                catch (Exception ex)
                {
                    Log.Error("ProductionViewModel refresh: {Message}", ex.Message);
                }

                try { await Task.Delay(1000, token); }
                catch (TaskCanceledException) { break; }
            }
        }, token);
    }

    public void StopRefresh()
    {
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _refreshCts = null;
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        var rows = await _logger.GetLatestAsync(LatestLimit);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            LatestRecords.Clear();
            foreach (var r in rows)
                LatestRecords.Add(r);
        });
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
