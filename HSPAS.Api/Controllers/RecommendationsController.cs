using HSPAS.Api.Infrastructure;
using HSPAS.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HSPAS.Api.Controllers;

/// <summary>智慧選股建議 API</summary>
[ApiController]
[Route("api/recommendations")]
public class RecommendationsController : ControllerBase
{
    private readonly HspasDbContext _db;
    private readonly IPortfolioService _portfolio;
    private readonly ITechnicalIndicatorService _ti;

    public RecommendationsController(HspasDbContext db, IPortfolioService portfolio, ITechnicalIndicatorService ti)
    {
        _db = db;
        _portfolio = portfolio;
        _ti = ti;
    }

    [HttpGet("stocks")]
    public async Task<IActionResult> Get([FromQuery] string scope = "holding", CancellationToken ct = default)
    {
        // 取得要分析的標的清單
        List<string> stockIds;
        if (scope == "holding")
        {
            var holdings = await _portfolio.GetHoldingsAsync(ct);
            stockIds = holdings.Items.Select(h => h.StockId).ToList();
        }
        else
        {
            // all：取最近交易日的所有標的（取前 200 依成交量排序）
            var latestDate = await _db.DailyStockPrices.MaxAsync(d => (DateTime?)d.TradeDate, ct);
            if (!latestDate.HasValue) return Ok(new { generatedAt = DateTime.Now, scope, longTermCandidates = Array.Empty<object>(), shortTermCandidates = Array.Empty<object>() });
            stockIds = await _db.DailyStockPrices
                .Where(d => d.TradeDate == latestDate.Value && d.TradeVolume > 0)
                .OrderByDescending(d => d.TradeVolume)
                .Take(200)
                .Select(d => d.StockId)
                .ToListAsync(ct);
        }

        var longTerm = new List<object>();
        var shortTerm = new List<object>();

        foreach (var sid in stockIds)
        {
            var prices = await _db.DailyStockPrices
                .Where(d => d.StockId == sid)
                .OrderBy(d => d.TradeDate)
                .Select(d => new { d.ClosePrice, d.StockName, d.TradeVolume })
                .ToListAsync(ct);

            if (prices.Count < 60) continue;

            var closes = prices.Select(p => p.ClosePrice).ToList();
            var name = prices.Last().StockName ?? sid;
            var lastClose = closes.Last() ?? 0;

            var ma60 = _ti.CalculateMovingAverage(closes, 60);
            var ma20 = _ti.CalculateMovingAverage(closes, 20);
            var ma5 = _ti.CalculateMovingAverage(closes, 5);
            var rsi = _ti.CalculateRsi(closes, 14);

            var lastMa60 = ma60.Last();
            var lastMa20 = ma20.Last();
            var lastMa5 = ma5.Last();
            var lastRsi = rsi.Last();

            // 長期：在季線上方且趨勢偏多
            if (lastClose > 0 && lastMa60.HasValue && lastClose > lastMa60 && lastMa20.HasValue && lastClose > lastMa20)
            {
                var tags = new List<string> { "trend_up", "quarterly_MA_support" };
                if (sid.StartsWith("00")) tags.Insert(0, "ETF");
                longTerm.Add(new { stockId = sid, stockName = name, reason = "股價在季線與月線上方，中長期趨勢偏多", tags });
            }

            // 短期：5日均線突破20日、RSI > 50、成交量放大
            if (lastMa5.HasValue && lastMa20.HasValue && lastMa5 > lastMa20 && lastRsi.HasValue && lastRsi > 50)
            {
                var recentVol = prices.Skip(prices.Count - 5).Average(p => p.TradeVolume ?? 0);
                var prevVol = prices.Skip(prices.Count - 25).Take(20).Average(p => p.TradeVolume ?? 0);
                if (prevVol > 0 && recentVol > prevVol * 1.2)
                {
                    shortTerm.Add(new { stockId = sid, stockName = name, reason = "短期均線突破、成交量放大、RSI 偏強",
                        tags = new[] { "short_term", "volume_surge", "ma_breakout" } });
                }
            }
        }

        return Ok(new { generatedAt = DateTime.Now, scope, longTermCandidates = longTerm, shortTermCandidates = shortTerm });
    }
}
