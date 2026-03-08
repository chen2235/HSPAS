using HSPAS.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HSPAS.Api.Controllers;

/// <summary>歷史回補 API</summary>
[ApiController]
[Route("api/history")]
public class HistoryBackfillController : ControllerBase
{
    private readonly IDailyPriceService _svc;

    public HistoryBackfillController(IDailyPriceService svc) => _svc = svc;

    public record BackfillRequest(string From, string To);

    /// <summary>區間回補：逐日抓取上市+上櫃行情寫入 DB</summary>
    [HttpPost("backfill")]
    public async Task<IActionResult> Backfill([FromBody] BackfillRequest req, CancellationToken ct)
    {
        if (!DateTime.TryParse(req.From, out var from) || !DateTime.TryParse(req.To, out var to))
            return BadRequest(new { error = "日期格式錯誤，請使用 YYYY-MM-DD。" });

        if (from > to)
            return BadRequest(new { error = "起始日期不可大於結束日期。" });

        var result = await _svc.BackfillRangeAsync(from, to, ct);
        return Ok(result);
    }
}
