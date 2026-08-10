using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Serilog;

namespace Lisovanie.Models;

/// <summary>
/// Filter pre dotaz/export výrobných záznamov.
/// </summary>
public class CProductionFilter
{
    public DateTime? OdLocal { get; set; }
    public DateTime? DoLocal { get; set; }
    /// <summary>null = všetky statusy.</summary>
    public EnProduktLis? Status { get; set; }
}

/// <summary>
/// Trvalé ukladanie výrobných dát do SQLite. Zápis je neblokujúci cez Channel,
/// aby sa PLC vlákno (AboveNormal) nikdy nezdržalo na I/O.
/// </summary>
public class CProductionLogger : IDisposable
{
    private readonly string _connectionString;
    private readonly Channel<CProductionRecord> _channel =
        Channel.CreateUnbounded<CProductionRecord>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    private Task? _writerTask;
    private bool _initialized;

    public string DbPath { get; }

    public CProductionLogger()
    {
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        Directory.CreateDirectory(dir);
        DbPath = Path.Combine(dir, "Production.db");
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    /// <summary>Vytvorí schému (ak treba) a spustí background zapisovač.</summary>
    public void Init()
    {
        if (_initialized) return;

        try
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            conn.Execute("PRAGMA journal_mode=WAL;");
            conn.Execute(@"
                CREATE TABLE IF NOT EXISTS ProductionRecord (
                    Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                    TimestampUtc     TEXT    NOT NULL,
                    Hmotnost         REAL    NOT NULL,
                    Sila             REAL    NOT NULL,
                    Vzdialenost      REAL    NOT NULL,
                    CasZhutnovaniaMs INTEGER NOT NULL,
                    CasZotrvaniaMs   INTEGER NOT NULL,
                    Status           INTEGER NOT NULL,
                    Metoda           INTEGER NOT NULL DEFAULT 0
                );");
            conn.Execute("CREATE INDEX IF NOT EXISTS IX_ProductionRecord_Timestamp ON ProductionRecord(TimestampUtc);");
            conn.Execute("CREATE INDEX IF NOT EXISTS IX_ProductionRecord_Status ON ProductionRecord(Status);");

            EnsureColumns(conn);

            _initialized = true;
            _writerTask = Task.Run(ProcessQueueAsync);
            Log.Information("CProductionLogger inicializovaný: {DbPath}", DbPath);
        }
        catch (Exception ex)
        {
            Log.Error("Chyba pri inicializácii CProductionLogger: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Doplní stĺpce, ktoré pribudli až po vytvorení databázy - CREATE TABLE IF NOT EXISTS
    /// na existujúcej tabuľke nič nepridá a INSERT by potom zlyhal.
    /// </summary>
    private static void EnsureColumns(SqliteConnection conn)
    {
        var existing = conn.Query<string>("SELECT name FROM pragma_table_info('ProductionRecord');")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!existing.Contains("Metoda"))
        {
            conn.Execute("ALTER TABLE ProductionRecord ADD COLUMN Metoda INTEGER NOT NULL DEFAULT 0;");
            Log.Warning("CProductionLogger: do tabuľky ProductionRecord doplnený stĺpec Metoda.");
        }
    }

    /// <summary>Neblokujúce zaradenie záznamu na zápis. Volá ho PLC vlákno.</summary>
    public void Enqueue(CProductionRecord record)
    {
        if (!_channel.Writer.TryWrite(record))
            Log.Warning("CProductionLogger: záznam sa nepodarilo zaradiť na zápis.");
    }

    private async Task ProcessQueueAsync()
    {
        try
        {
            await foreach (var record in _channel.Reader.ReadAllAsync())
            {
                try
                {
                    await using var conn = new SqliteConnection(_connectionString);
                    await conn.OpenAsync();
                    await conn.ExecuteAsync(@"
                        INSERT INTO ProductionRecord
                            (TimestampUtc, Hmotnost, Sila, Vzdialenost, CasZhutnovaniaMs, CasZotrvaniaMs, Status, Metoda)
                        VALUES
                            (@TimestampUtc, @Hmotnost, @Sila, @Vzdialenost, @CasZhutnovaniaMs, @CasZotrvaniaMs, @Status, @Metoda);",
                        new
                        {
                            TimestampUtc = record.TimestampUtc.ToString("O"),
                            record.Hmotnost,
                            record.Sila,
                            record.Vzdialenost,
                            record.CasZhutnovaniaMs,
                            record.CasZotrvaniaMs,
                            Status = (int)record.Status,
                            Metoda = (int)record.Metoda
                        });
                }
                catch (Exception ex)
                {
                    Log.Error("CProductionLogger: zlyhal zápis záznamu: {Message}", ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("CProductionLogger: zapisovacia slučka skončila chybou: {Message}", ex.Message);
        }
    }

    /// <summary>Synchrónny (awaitable) zápis jedného záznamu – obíde Channel, aby volajúci
    /// hneď po dokončení videl záznam v DB (používa generátor testovacích dát).</summary>
    public async Task InsertAsync(CProductionRecord record)
    {
        if (!_initialized) return;
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await conn.ExecuteAsync(@"
            INSERT INTO ProductionRecord
                (TimestampUtc, Hmotnost, Sila, Vzdialenost, CasZhutnovaniaMs, CasZotrvaniaMs, Status, Metoda)
            VALUES
                (@TimestampUtc, @Hmotnost, @Sila, @Vzdialenost, @CasZhutnovaniaMs, @CasZotrvaniaMs, @Status, @Metoda);",
            new
            {
                TimestampUtc = record.TimestampUtc.ToString("O"),
                record.Hmotnost,
                record.Sila,
                record.Vzdialenost,
                record.CasZhutnovaniaMs,
                record.CasZotrvaniaMs,
                Status = (int)record.Status,
                Metoda = (int)record.Metoda
            });
    }

    /// <summary>Vymaže záznamy s danými Id. Vráti počet zmazaných riadkov.</summary>
    public async Task<int> DeleteByIdsAsync(IReadOnlyCollection<long> ids)
    {
        if (!_initialized || ids.Count == 0) return 0;
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        var deleted = await conn.ExecuteAsync(
            "DELETE FROM ProductionRecord WHERE Id IN @ids;", new { ids });
        Log.Information("CProductionLogger: zmazaných {Count} záznamov.", deleted);
        return deleted;
    }

    /// <summary>Posledných <paramref name="limit"/> záznamov (najnovšie navrchu). Pre UI tabuľku.</summary>
    public async Task<IReadOnlyList<CProductionRecord>> GetLatestAsync(int limit)
    {
        if (!_initialized) return Array.Empty<CProductionRecord>();
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        var rows = await conn.QueryAsync<CProductionRecord>(
            "SELECT * FROM ProductionRecord ORDER BY Id DESC LIMIT @limit;", new { limit });
        return rows.AsList();
    }

    /// <summary>Záznamy podľa filtra (rozsah lokálneho času + status). Pre export aj UI tabuľku.
    /// <paramref name="limit"/> = 0 znamená bez limitu.</summary>
    public async Task<IReadOnlyList<CProductionRecord>> QueryAsync(CProductionFilter filter, int limit = 0)
    {
        if (!_initialized) return Array.Empty<CProductionRecord>();

        var sql = new StringBuilder("SELECT * FROM ProductionRecord WHERE 1=1");
        var p = new DynamicParameters();

        if (filter.OdLocal.HasValue)
        {
            sql.Append(" AND TimestampUtc >= @od");
            p.Add("od", filter.OdLocal.Value.ToUniversalTime().ToString("O"));
        }
        if (filter.DoLocal.HasValue)
        {
            sql.Append(" AND TimestampUtc <= @do");
            p.Add("do", filter.DoLocal.Value.ToUniversalTime().ToString("O"));
        }
        if (filter.Status.HasValue)
        {
            sql.Append(" AND Status = @status");
            p.Add("status", (int)filter.Status.Value);
        }
        sql.Append(" ORDER BY Id DESC");
        if (limit > 0)
        {
            sql.Append(" LIMIT @limit");
            p.Add("limit", limit);
        }
        sql.Append(';');

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        var rows = await conn.QueryAsync<CProductionRecord>(sql.ToString(), p);
        return rows.AsList();
    }

    /// <summary>Ukončí príjem a počká na dopísanie frontu (volá sa pri shutdowne).</summary>
    public void Complete()
    {
        try
        {
            _channel.Writer.TryComplete();
            _writerTask?.Wait(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            Log.Warning("CProductionLogger: chyba pri ukončovaní: {Message}", ex.Message);
        }
    }

    public void Dispose() => Complete();
}
