using HSPAS.Api.Infrastructure;
using HSPAS.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HSPAS.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IPortfolioService _portfolio;
    private readonly HspasDbContext _db;

    public DashboardController(IPortfolioService portfolio, HspasDbContext db)
    {
        _portfolio = portfolio;
        _db = db;
    }

    [HttpGet("holdings")]
    public async Task<IActionResult> GetHoldings(CancellationToken ct)
    {
        var data = await _portfolio.GetHoldingsAsync(ct);
        return Ok(data);
    }

    private record BuyItem(string Source, string TradeDate, int Quantity, decimal Price, decimal Amount, string? Note, decimal Ratio);

    [HttpGet("holding/{stockId}/buy-distribution")]
    public async Task<IActionResult> GetBuyDistribution(string stockId, CancellationToken ct)
    {
        var manualBuys = await _db.TradeRecords
            .Where(t => t.StockId == stockId && t.Action == "BUY"
                && (t.Note == null || !t.Note.StartsWith("[DCA]")))
            .ToListAsync(ct);

        var dcaBuys = await _db.DcaExecutions
            .Where(e => e.StockId == stockId && e.Status == "SUCCESS")
            .ToListAsync(ct);

        var items = new List<BuyItem>();

        foreach (var b in manualBuys)
        {
            var amt = b.Price * b.Quantity + b.Fee + b.Tax + (b.OtherCost ?? 0);
            items.Add(new BuyItem("MANUAL", b.TradeDate.ToString("yyyy-MM-dd"), b.Quantity, b.Price, amt, b.Note, 0));
        }

        foreach (var d in dcaBuys)
        {
            var amt = d.Price * d.Quantity + d.Fee + d.Tax + (d.OtherCost ?? 0);
            items.Add(new BuyItem("DCA", d.TradeDate.ToString("yyyy-MM-dd"), d.Quantity, d.Price, amt, d.Note, 0));
        }

        var totalAmount = items.Sum(i => i.Amount);
        var result = items
            .Select(i => i with { Ratio = totalAmount > 0 ? i.Amount / totalAmount : 0 })
            .OrderByDescending(i => i.Amount)
            .ToList();

        return Ok(new
        {
            stockId,
            totalAmount,
            manualTotal = items.Where(i => i.Source == "MANUAL").Sum(i => i.Amount),
            dcaTotal = items.Where(i => i.Source == "DCA").Sum(i => i.Amount),
            items = result
        });
    }
}
