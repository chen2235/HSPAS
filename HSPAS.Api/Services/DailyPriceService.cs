using HSPAS.Api.Entities;
using HSPAS.Api.Infrastructure;
using HSPAS.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HSPAS.Api.Services;

/// <summary>每日行情業務邏輯：查詢 DB、單日/區間回補（上市 TSE + 上櫃 OTC）</summary>
public class DailyPriceService : IDailyPriceService
{
    private readonly HspasDbContext _db;
    private readonly ITwseDataService _twse;
    private readonly ILogger<DailyPriceService> _logger;

    public DailyPriceService(HspasDbContext db, ITwseDataService twse, ILogger<DailyPriceService> logger)
    {
        _db = db;
        _twse = twse;
        _logger = logger;
    }

    public async Task<List<string>> GetAvailableDatesAsync(CancellationToken ct = default)
    {
        return await _db.DailyStockPrices
            .Select(d => d.TradeDate)
            .Distinct()
            .OrderByDescending(d => d)
            .Select(d => d.ToString("yyyy-MM-dd"))
            .ToListAsync(ct);
    }

    public async Task<List<DailyStockPrice>> GetByDateAsync(DateTime date, CancellationToken ct = default)
    {
        var existing = await _db.DailyStockPrices
            .Where(d => d.TradeDate == date.Date)
            .OrderBy(d => d.StockId)
            .ToListAsync(ct);

        if (existing.Count > 0)
            return existing;

        // DB 無資料且為今天 → 即時回補
        if (date.Date == DateTime.Today)
        {
            _logger.LogInformation("No data in DB for today {Date}, triggering backfill...", date);
            var result = await BackfillOneDayAsync(date, ct);
            if (result.TseCount > 0 || result.OtcCount > 0)
            {
                return await _db.DailyStockPrices
                    .Where(d => d.TradeDate == date.Date)
                    .OrderBy(d => d.StockId)
                    .ToListAsync(ct);
            }
        }

        return new List<DailyStockPrice>();
    }

    public async Task<List<DailyStockPrice>> GetStockHistoryAsync(string stockId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await _db.DailyStockPrices
            .Where(d => d.StockId == stockId && d.TradeDate >= from.Date && d.TradeDate <= to.Date)
            .OrderBy(d => d.TradeDate)
            .ToListAsync(ct);
    }

    // ==================== Fetch ====================

    public async Task<List<DailyStockPrice>> FetchTseDailyAsync(DateTime date, CancellationToken ct = default)
    {
        return await _twse.FetchDailyPricesAsync(date, ct);
    }

    public async Task<List<DailyStockPrice>> FetchOtcDailyAsync(DateTime date, CancellationToken ct = default)
    {
        return await _twse.FetchOtcDailyPricesAsync(date, ct);
    }

    // ==================== 單日回補 ====================

    public async Task<BackfillOneDayResult> BackfillOneDayAsync(DateTime date, CancellationToken ct = default)
    {
        var dateStr = date.Date.ToString("yyyy-MM-dd");
        var result = new BackfillOneDayResult { Date = dateStr };
        var errors = new List<string>();

        // 1. 抓取上市（TSE）— MI_INDEX 支援歷史日期
        List<DailyStockPrice> tseData = new();
        try
        {
            tseData = await FetchTseDailyAsync(date, ct);
            _logger.LogInformation("Fetched {Count} TSE records for {Date}", tseData.Count, dateStr);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TSE fetch failed for {Date}", dateStr);
            errors.Add($"TSE 抓取失敗: {ex.Message}");
        }

        // 間隔 1 秒，避免連續請求被擋
        await Task.Delay(1000, ct);

        // 2. 抓取上櫃（OTC）— TPEx 逐日呼叫，內部已有日期比對過濾
        List<DailyStockPrice> otcData = new();
        try
        {
            otcData = await FetchOtcDailyAsync(date, ct);
            _logger.LogInformation("Fetched {Count} OTC records for {Date}", otcData.Count, dateStr);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OTC fetch failed for {Date}", dateStr);
            errors.Add($"OTC 抓取失敗: {ex.Message}");
        }

        // 合併並去重（以 TradeDate + StockId + MarketType 為 key，取第一筆）
        var allData = tseData.Concat(otcData)
            .GroupBy(d => new { d.TradeDate, d.StockId, d.MarketType })
            .Select(g => g.First())
            .ToList();

        result.TseCount = allData.Count(d => d.MarketType == "TSE");
        result.OtcCount = allData.Count(d => d.MarketType == "OTC");

        if (allData.Count == 0)
        {
            result.Status = errors.Count > 0 ? "FAILED" : "NO_DATA";
            result.Message = errors.Count > 0
                ? string.Join("; ", errors)
                : "該日無行情資料（假日或尚無資料）。";
            return result;
        }

        // 3. 先刪後插：刪除該日所有資料再重新寫入
        try
        {
            var existingCount = await _db.DailyStockPrices
                .Where(d => d.TradeDate == date.Date)
                .ExecuteDeleteAsync(ct);

            if (existingCount > 0)
                _logger.LogInformation("Deleted {Count} existing records for {Date}", existingCount, dateStr);

            _db.DailyStockPrices.AddRange(allData);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Saved {Count} records for {Date} (TSE:{Tse}, OTC:{Otc})",
                allData.Count, dateStr, result.TseCount, result.OtcCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DB save failed for {Date}", dateStr);
            _db.ChangeTracker.Clear();
            result.Status = "FAILED";
            result.Message = $"DB 儲存失敗: {ex.Message}";
            if (errors.Count > 0) result.Message += "; " + string.Join("; ", errors);
            return result;
        }

        // 4. 產生結果
        if (errors.Count > 0)
        {
            result.Status = "PARTIAL";
            result.Message = $"部分成功：TSE {result.TseCount} 筆, OTC {result.OtcCount} 筆。{string.Join("; ", errors)}";
        }
        else
        {
            result.Status = "SUCCESS";
            result.Message = $"TSE {result.TseCount} 筆, OTC {result.OtcCount} 筆。";
        }

        return result;
    }

    // ==================== 區間回補 ====================

    public async Task<BackfillRangeResult> BackfillRangeAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var rangeResult = new BackfillRangeResult
        {
            From = from.ToString("yyyy-MM-dd"),
            To = to.ToString("yyyy-MM-dd")
        };

        // 產生區間內所有日期（排除週六日）
        var workDays = new List<DateTime>();
        for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
        {
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                workDays.Add(d);
        }

        rangeResult.TotalDays = workDays.Count;

        foreach (var date in workDays)
        {
            var dayResult = await BackfillOneDayAsync(date, ct);
            rangeResult.Results.Add(dayResult);

            // 統計
            rangeResult.TotalTseCount += dayResult.TseCount;
            rangeResult.TotalOtcCount += dayResult.OtcCount;

            switch (dayResult.Status)
            {
                case "SUCCESS":
                case "PARTIAL":
                    rangeResult.SuccessDays++;
                    break;
                case "NO_DATA":
                    rangeResult.NoDataDays++;
                    break;
                default:
                    rangeResult.FailedDays++;
                    break;
            }

            // 每天之間間隔 2 秒，避免被來源網站擋
            if (date != workDays[^1])
                await Task.Delay(2000, ct);
        }

        return rangeResult;
    }
}
