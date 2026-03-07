using HSPAS.Api.Entities;
using HSPAS.Api.Infrastructure;
using HSPAS.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HSPAS.Api.Services;

/// <summary>每日行情業務邏輯：查詢 DB、觸發即時抓取</summary>
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
        // 先查 DB
        var existing = await _db.DailyStockPrices
            .Where(d => d.TradeDate == date.Date)
            .OrderBy(d => d.StockId)
            .ToListAsync(ct);

        if (existing.Count > 0)
            return existing;

        // DB 無資料且為今天 → 即時從 TWSE 抓取
        if (date.Date == DateTime.Today)
        {
            _logger.LogInformation("No data in DB for today {Date}, fetching from TWSE...", date);
            var fetched = await _twse.FetchDailyPricesAsync(date, ct);
            if (fetched.Count > 0)
            {
                _db.DailyStockPrices.AddRange(fetched);
                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("Saved {Count} records for {Date}", fetched.Count, date);
            }
            return fetched.OrderBy(d => d.StockId).ToList();
        }

        // 過去日期且 DB 無資料 → 回傳空
        return new List<DailyStockPrice>();
    }

    public async Task<List<DailyStockPrice>> GetStockHistoryAsync(string stockId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await _db.DailyStockPrices
            .Where(d => d.StockId == stockId && d.TradeDate >= from.Date && d.TradeDate <= to.Date)
            .OrderBy(d => d.TradeDate)
            .ToListAsync(ct);
    }
}
