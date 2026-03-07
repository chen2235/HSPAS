using HSPAS.Api.Infrastructure;
using HSPAS.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HSPAS.Api.Controllers;

/// <summary>季線風險警示 API</summary>
[ApiController]
[Route("api/alerts")]
public class AlertsController : ControllerBase
{
    private readonly HspasDbContext _db;
    private readonly IPortfolioService _portfolio;
    private readonly ITechnicalIndicatorService _ti;

    public AlertsController(HspasDbContext db, IPortfolioService portfolio, ITechnicalIndicatorService ti)
    {
        _db = db;
        _portfolio = portfolio;
        _ti = ti;
    }

    [HttpGet("below-quarterly-ma")]
    public async Task<IActionResult> GetBelowQuarterlyMa([FromQuery] int days = 60, CancellationToken ct = default)
    {
        var holdings = await _portfolio.GetHoldingsAsync(ct);
        if (holdings.Items.Count == 0) return Ok(new { asOf = DateTime.Today.ToString("yyyy-MM-dd"), maDays = days, items = Array.Empty<object>() });

        var items = new List<object>();
        foreach (var h in holdings.Items)
        {
            var prices = await _db.DailyStockPrices
                .Where(d => d.StockId == h.StockId)
                .OrderBy(d => d.TradeDate)
                .Select(d => d.ClosePrice)
                .ToListAsync(ct);

            if (prices.Count < days) continue;

            var maValues = _ti.CalculateMovingAverage(prices, days);
            var lastMa = maValues.Last();
            var lastClose = prices.Last();

            if (lastClose.HasValue && lastMa.HasValue && lastClose < lastMa)
            {
                var diff = lastClose.Value - lastMa.Value;
                items.Add(new
                {
                    h.StockId,
                    h.StockName,
                    currentQty = h.Quantity,
                    lastClosePrice = lastClose,
                    ma = lastMa,
                    belowMa = true,
                    diff,
                    diffPercent = lastMa > 0 ? Math.Round(diff / lastMa.Value, 4) : 0
                });
            }
        }

        return Ok(new
        {
            asOf = DateTime.Today.ToString("yyyy-MM-dd"),
            maDays = days,
            items = items.OrderBy(i => ((dynamic)i).diffPercent)
        });
    }
}
