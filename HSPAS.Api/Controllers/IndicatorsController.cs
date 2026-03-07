using HSPAS.Api.Infrastructure;
using HSPAS.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HSPAS.Api.Controllers;

[ApiController]
[Route("api/indicators")]
public class IndicatorsController : ControllerBase
{
    private readonly HspasDbContext _db;
    private readonly ITechnicalIndicatorService _ti;

    public IndicatorsController(HspasDbContext db, ITechnicalIndicatorService ti)
    {
        _db = db;
        _ti = ti;
    }

    [HttpGet("{stockId}")]
    public async Task<IActionResult> Get(
        string stockId,
        [FromQuery] string from,
        [FromQuery] string to,
        [FromQuery] string? ma,
        [FromQuery] int rsiperiod = 14,
        CancellationToken ct = default)
    {
        if (!DateTime.TryParse(from, out var f) || !DateTime.TryParse(to, out var t))
            return BadRequest(new { error = "Invalid date." });

        var maPeriods = (ma ?? "5,20,60").Split(',').Select(s => int.TryParse(s.Trim(), out var v) ? v : 0).Where(v => v > 0).ToList();
        var maxWindow = Math.Max(maPeriods.DefaultIfEmpty(0).Max(), rsiperiod);

        // 取多一些資料來讓指標有足夠的前置資料
        var extendedFrom = f.AddDays(-maxWindow * 2);
        var prices = await _db.DailyStockPrices
            .Where(d => d.StockId == stockId && d.TradeDate >= extendedFrom && d.TradeDate <= t)
            .OrderBy(d => d.TradeDate)
            .ToListAsync(ct);

        var closes = prices.Select(p => p.ClosePrice).ToList();

        // 計算 MA
        var maResults = new Dictionary<int, List<decimal?>>();
        foreach (var p in maPeriods)
            maResults[p] = _ti.CalculateMovingAverage(closes, p);

        // 計算 RSI
        var rsiResults = _ti.CalculateRsi(closes, rsiperiod);

        // 只回傳 [from, to] 範圍的資料
        var items = new List<object>();
        for (int i = 0; i < prices.Count; i++)
        {
            if (prices[i].TradeDate < f) continue;
            var maDict = new Dictionary<string, decimal?>();
            foreach (var p in maPeriods)
                maDict[p.ToString()] = maResults[p][i];

            items.Add(new
            {
                date = prices[i].TradeDate.ToString("yyyy-MM-dd"),
                closePrice = prices[i].ClosePrice,
                ma = maDict,
                rsi = rsiResults[i]
            });
        }

        return Ok(new { stockId, from = f.ToString("yyyy-MM-dd"), to = t.ToString("yyyy-MM-dd"), maPeriods, rsiPeriod = rsiperiod, items });
    }
}
