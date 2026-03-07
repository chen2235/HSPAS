using HSPAS.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HSPAS.Api.Controllers;

/// <summary>每日行情 API：依日期查詢、個股歷史</summary>
[ApiController]
[Route("api/daily-prices")]
public class DailyPricesController : ControllerBase
{
    private readonly IDailyPriceService _svc;

    public DailyPricesController(IDailyPriceService svc) => _svc = svc;

    /// <summary>取得指定日期全市場行情</summary>
    [HttpGet("by-date")]
    public async Task<IActionResult> GetByDate([FromQuery] string date, CancellationToken ct)
    {
        if (!DateTime.TryParse(date, out var d))
            return BadRequest(new { error = "Invalid date format. Use YYYY-MM-DD." });

        var items = await _svc.GetByDateAsync(d, ct);
        return Ok(new
        {
            date = d.ToString("yyyy-MM-dd"),
            totalCount = items.Count,
            items = items.Select(i => new
            {
                i.StockId,
                i.StockName,
                i.TradeVolume,
                i.TradeValue,
                i.OpenPrice,
                i.HighPrice,
                i.LowPrice,
                i.ClosePrice,
                i.PriceChange,
                i.Transaction
            })
        });
    }

    /// <summary>取得個股歷史價量</summary>
    [HttpGet("{stockId}/history")]
    public async Task<IActionResult> GetStockHistory(
        string stockId,
        [FromQuery] string from,
        [FromQuery] string to,
        CancellationToken ct)
    {
        if (!DateTime.TryParse(from, out var f) || !DateTime.TryParse(to, out var t))
            return BadRequest(new { error = "Invalid date format. Use YYYY-MM-DD." });

        var items = await _svc.GetStockHistoryAsync(stockId, f, t, ct);
        return Ok(new
        {
            stockId,
            from = f.ToString("yyyy-MM-dd"),
            to = t.ToString("yyyy-MM-dd"),
            totalCount = items.Count,
            items = items.Select(i => new
            {
                date = i.TradeDate.ToString("yyyy-MM-dd"),
                i.StockName,
                i.TradeVolume,
                i.TradeValue,
                i.OpenPrice,
                i.HighPrice,
                i.LowPrice,
                i.ClosePrice,
                i.PriceChange,
                i.Transaction
            })
        });
    }
}
