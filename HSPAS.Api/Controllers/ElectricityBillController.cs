using HSPAS.Api.Services;
using HSPAS.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HSPAS.Api.Controllers;

[ApiController]
[Route("api/life/utility/electricity")]
public class ElectricityBillController : ControllerBase
{
    private readonly IElectricityBillService _svc;
    private readonly ILogger<ElectricityBillController> _logger;

    public ElectricityBillController(IElectricityBillService svc, ILogger<ElectricityBillController> logger)
    {
        _svc = svc;
        _logger = logger;
    }

    /// <summary>上傳台電 PDF 帳單並解析（僅解析，不儲存）</summary>
    [HttpPost("upload")]
    public IActionResult Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "請選擇 PDF 檔案" });

        var parser = new TaipowerBillParserService();
        using var stream = file.OpenReadStream();
        var result = parser.Parse(stream);

        if (!result.Success || result.Bill == null)
            return BadRequest(new { error = result.Error ?? "PDF 解析失敗" });

        var bill = result.Bill;

        // 僅回傳解析結果，不存入 DB，等使用者確認後再呼叫 save
        return Ok(new
        {
            bill.Address,
            bill.PowerNo,
            bill.BlackoutGroup,
            billingStartDate = bill.BillingStartDate.ToString("yyyy-MM-dd"),
            billingEndDate = bill.BillingEndDate.ToString("yyyy-MM-dd"),
            bill.BillingDays,
            bill.BillingPeriodText,
            readOrDebitDate = bill.ReadOrDebitDate.ToString("yyyy-MM-dd"),
            bill.Kwh,
            bill.KwhPerDay,
            bill.AvgPricePerKwh,
            bill.TotalAmount,
            bill.InvoiceAmount,
            bill.TariffType,
            bill.SharedMeterHouseholdCount,
            bill.InvoicePeriod,
            bill.InvoiceNo,
            bill.RawDetailJson,
        });
    }

    /// <summary>確認儲存電費紀錄（使用者確認解析結果後呼叫）</summary>
    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] ElecBillSaveRequest req, CancellationToken ct)
    {
        var saved = await _svc.SaveAsync(req, ct);
        return Ok(saved);
    }

    /// <summary>取得電費紀錄列表</summary>
    [HttpGet("period-records")]
    public async Task<IActionResult> GetList([FromQuery] int? year, [FromQuery] int? month, CancellationToken ct)
    {
        var list = await _svc.GetListAsync(year, month, ct);
        return Ok(list);
    }

    /// <summary>取得單筆電費紀錄明細</summary>
    [HttpGet("period-records/{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var dto = await _svc.GetByIdAsync(id, ct);
        if (dto == null) return NotFound(new { error = $"Id {id} 不存在" });
        return Ok(dto);
    }

    /// <summary>修改電費紀錄</summary>
    [HttpPut("period-records/{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] ElecBillUpdateRequest req, CancellationToken ct)
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

    /// <summary>刪除電費紀錄</summary>
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

    /// <summary>電費儀表板資料</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] int year, CancellationToken ct)
    {
        var data = await _svc.GetDashboardAsync(year, ct);
        return Ok(data);
    }
}
