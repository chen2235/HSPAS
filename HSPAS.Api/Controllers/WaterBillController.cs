using HSPAS.Api.Services;
using HSPAS.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HSPAS.Api.Controllers;

[ApiController]
[Route("api/life/utility/water")]
public class WaterBillController : ControllerBase
{
    private readonly IWaterBillService _svc;
    private readonly ILogger<WaterBillController> _logger;

    public WaterBillController(IWaterBillService svc, ILogger<WaterBillController> logger)
    {
        _svc = svc;
        _logger = logger;
    }

    /// <summary>上傳水費 PDF 帳單並解析（僅解析，不儲存）</summary>
    [HttpPost("upload")]
    public IActionResult Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "請選擇 PDF 檔案" });

        var parser = new TaiwaterBillParserService();
        using var stream = file.OpenReadStream();
        var result = parser.Parse(stream);

        if (!result.Success || result.Bill == null)
            return BadRequest(new { error = result.Error ?? "PDF 解析失敗" });

        var bill = result.Bill;

        return Ok(new
        {
            bill.WaterAddress,
            bill.WaterNo,
            bill.MeterNo,
            billingStartDate = bill.BillingStartDate.ToString("yyyy-MM-dd"),
            billingEndDate = bill.BillingEndDate.ToString("yyyy-MM-dd"),
            bill.BillingDays,
            bill.BillingPeriodText,
            bill.TotalUsage,
            bill.CurrentUsage,
            bill.CurrentMeterReading,
            bill.PreviousMeterReading,
            bill.TotalAmount,
            bill.RawDetailJson,
        });
    }

    /// <summary>確認儲存水費紀錄</summary>
    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] WaterBillSaveRequest req, CancellationToken ct)
    {
        var saved = await _svc.SaveAsync(req, ct);
        return Ok(saved);
    }

    /// <summary>取得水費紀錄列表</summary>
    [HttpGet("period-records")]
    public async Task<IActionResult> GetList([FromQuery] int? year, CancellationToken ct)
    {
        var list = await _svc.GetListAsync(year, ct);
        return Ok(list);
    }

    /// <summary>取得單筆水費紀錄明細</summary>
    [HttpGet("period-records/{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var dto = await _svc.GetByIdAsync(id, ct);
        if (dto == null) return NotFound(new { error = $"Id {id} 不存在" });
        return Ok(dto);
    }

    /// <summary>修改水費紀錄</summary>
    [HttpPut("period-records/{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] WaterBillUpdateRequest req, CancellationToken ct)
    {
        try
        {
            var dto = await _svc.UpdateAsync(id, req, ct);
            return Ok(dto);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = $"Id {id} 不存在" });
        }
    }

    /// <summary>刪除水費紀錄</summary>
    [HttpDelete("period-records/{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        try
        {
            await _svc.DeleteAsync(id, ct);
            return Ok(new { message = "已刪除" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = $"Id {id} 不存在" });
        }
    }

    /// <summary>水費儀表板資料</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] int year, CancellationToken ct)
    {
        var data = await _svc.GetDashboardAsync(year, ct);
        return Ok(data);
    }
}
