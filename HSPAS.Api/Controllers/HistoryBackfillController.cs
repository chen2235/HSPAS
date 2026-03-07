using HSPAS.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HSPAS.Api.Controllers;

/// <summary>歷史回補 API</summary>
[ApiController]
[Route("api/history")]
public class HistoryBackfillController : ControllerBase
{
    private readonly IBackfillService _svc;

    public HistoryBackfillController(IBackfillService svc) => _svc = svc;

    public record BackfillRequest(string From, string To, bool DryRun = false);

    /// <summary>發起歷史回補</summary>
    [HttpPost("backfill")]
    public async Task<IActionResult> Backfill([FromBody] BackfillRequest req, CancellationToken ct)
    {
        if (!DateTime.TryParse(req.From, out var from) || !DateTime.TryParse(req.To, out var to))
            return BadRequest(new { error = "Invalid date format. Use YYYY-MM-DD." });

        if (from > to)
            return BadRequest(new { error = "from must be <= to." });

        var result = await _svc.ExecuteAsync(from, to, req.DryRun, ct);
        return Ok(result);
    }
}
