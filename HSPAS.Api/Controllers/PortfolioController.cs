using HSPAS.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HSPAS.Api.Controllers;

[ApiController]
[Route("api/portfolio")]
public class PortfolioController : ControllerBase
{
    private readonly IPortfolioService _svc;

    public PortfolioController(IPortfolioService svc) => _svc = svc;

    [HttpGet("stock/{stockId}/unrealized")]
    public async Task<IActionResult> GetStockUnrealized(string stockId, CancellationToken ct)
    {
        var data = await _svc.GetStockUnrealizedAsync(stockId, ct);
        if (data == null) return NotFound(new { error = "No holdings for this stock." });
        return Ok(data);
    }

    [HttpGet("unrealized-summary")]
    public async Task<IActionResult> GetUnrealizedSummary(CancellationToken ct)
    {
        var data = await _svc.GetUnrealizedSummaryAsync(ct);
        return Ok(data);
    }
}
