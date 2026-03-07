using HSPAS.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HSPAS.Api.Controllers;

/// <summary>日曆相關 API：可用日期清單</summary>
[ApiController]
[Route("api/calendar")]
public class CalendarController : ControllerBase
{
    private readonly IDailyPriceService _svc;

    public CalendarController(IDailyPriceService svc) => _svc = svc;

    /// <summary>取得所有有行情資料的交易日期</summary>
    [HttpGet("available-dates")]
    public async Task<IActionResult> GetAvailableDates(CancellationToken ct)
    {
        var dates = await _svc.GetAvailableDatesAsync(ct);
        return Ok(dates);
    }
}
