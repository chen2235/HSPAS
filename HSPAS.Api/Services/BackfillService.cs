using HSPAS.Api.Infrastructure;
using HSPAS.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HSPAS.Api.Services;

/// <summary>歷史回補服務：逐日檢查並補抓缺失的行情資料</summary>
public class BackfillService : IBackfillService
{
    private readonly HspasDbContext _db;
    private readonly ITwseDataService _twse;
    private readonly ILogger<BackfillService> _logger;

    public BackfillService(HspasDbContext db, ITwseDataService twse, ILogger<BackfillService> logger)
    {
        _db = db;
        _twse = twse;
        _logger = logger;
    }

    public async Task<BackfillResult> ExecuteAsync(DateTime from, DateTime to, bool dryRun, CancellationToken ct = default)
    {
        var result = new BackfillResult
        {
            From = from.ToString("yyyy-MM-dd"),
            To = to.ToString("yyyy-MM-dd"),
            DryRun = dryRun
        };

        // 產生區間內所有日期（排除週六日）
        var allDates = new List<DateTime>();
        for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
        {
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                allDates.Add(d);
        }

        // 查出 DB 中已有資料的日期
        var existingDates = await _db.DailyStockPrices
            .Where(p => p.TradeDate >= from.Date && p.TradeDate <= to.Date)
            .Select(p => p.TradeDate)
            .Distinct()
            .ToListAsync(ct);

        var existingSet = new HashSet<DateTime>(existingDates);

        foreach (var date in allDates)
        {
            var dateStr = date.ToString("yyyy-MM-dd");

            if (existingSet.Contains(date))
            {
                result.Results.Add(new BackfillDateResult
                {
                    Date = dateStr,
                    Status = "SKIPPED_ALREADY_EXISTS",
                    Message = "DB already has data."
                });
                continue;
            }

            if (dryRun)
            {
                result.Results.Add(new BackfillDateResult
                {
                    Date = dateStr,
                    Status = "MISSING",
                    Message = "No data in DB, would fetch."
                });
                continue;
            }

            // 實際抓取
            try
            {
                var data = await _twse.FetchDailyPricesAsync(date, ct);
                if (data.Count > 0)
                {
                    _db.DailyStockPrices.AddRange(data);
                    await _db.SaveChangesAsync(ct);
                    result.Results.Add(new BackfillDateResult
                    {
                        Date = dateStr,
                        Status = "SUCCESS",
                        Message = $"Imported {data.Count} rows."
                    });
                }
                else
                {
                    result.Results.Add(new BackfillDateResult
                    {
                        Date = dateStr,
                        Status = "NO_DATA",
                        Message = "TWSE returned no data (holiday or unavailable)."
                    });
                }

                // 避免頻繁請求被 TWSE 擋，間隔 3 秒
                await Task.Delay(3000, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backfill failed for {Date}", date);
                result.Results.Add(new BackfillDateResult
                {
                    Date = dateStr,
                    Status = "FAILED",
                    Message = ex.Message
                });
            }
        }

        return result;
    }
}
