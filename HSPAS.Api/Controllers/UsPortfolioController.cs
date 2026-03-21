using HSPAS.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HSPAS.Api.Controllers;

[ApiController]
[Route("api/us/portfolio")]
public class UsPortfolioController : ControllerBase
{
    private readonly IUsPortfolioService _svc;

    public UsPortfolioController(IUsPortfolioService svc) => _svc = svc;

    [HttpGet("holdings")]
    public async Task<IActionResult> GetHoldings(CancellationToken ct)
    {
        var data = await _svc.GetHoldingsAsync(ct);
        return Ok(data);
    }

    [HttpGet("stock/{symbol}/unrealized")]
    public async Task<IActionResult> GetStockUnrealized(string symbol, CancellationToken ct)
    {
        var data = await _svc.GetStockUnrealizedAsync(symbol.ToUpper(), ct);
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
