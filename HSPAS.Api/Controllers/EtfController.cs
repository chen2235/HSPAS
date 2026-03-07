using HSPAS.Api.Entities;
using HSPAS.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HSPAS.Api.Controllers;

[ApiController]
[Route("api/etf")]
public class EtfController : ControllerBase
{
    private readonly HspasDbContext _db;

    public EtfController(HspasDbContext db) => _db = db;

    [HttpGet("list")]
    public async Task<IActionResult> GetList(CancellationToken ct)
    {
        var items = await _db.EtfInfos.Where(e => e.IsActive).OrderBy(e => e.EtfId).ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("daily")]
    public async Task<IActionResult> GetDaily([FromQuery] string date, CancellationToken ct)
    {
        if (!DateTime.TryParse(date, out var d)) return BadRequest(new { error = "Invalid date." });
        var etfIds = await _db.EtfInfos.Where(e => e.IsActive).Select(e => e.EtfId).ToListAsync(ct);
        var prices = await _db.DailyStockPrices
            .Where(p => p.TradeDate == d.Date && etfIds.Contains(p.StockId))
            .OrderBy(p => p.StockId)
            .ToListAsync(ct);
        return Ok(new { date = d.ToString("yyyy-MM-dd"), totalCount = prices.Count, items = prices });
    }

    [HttpGet("{etfId}/history")]
    public async Task<IActionResult> GetHistory(string etfId, [FromQuery] string from, [FromQuery] string to, CancellationToken ct)
    {
        if (!DateTime.TryParse(from, out var f) || !DateTime.TryParse(to, out var t))
            return BadRequest(new { error = "Invalid date." });
        var items = await _db.DailyStockPrices
            .Where(d => d.StockId == etfId && d.TradeDate >= f.Date && d.TradeDate <= t.Date)
            .OrderBy(d => d.TradeDate).ToListAsync(ct);
        return Ok(new { etfId, from = f.ToString("yyyy-MM-dd"), to = t.ToString("yyyy-MM-dd"), totalCount = items.Count, items });
    }
}
