using HSPAS.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HSPAS.Api.Controllers;

[ApiController]
[Route("api/health/checkup/qtr")]
public class HealthCheckupQuarterController : ControllerBase
{
    private readonly IQuarterHealthReportService _svc;
    private readonly IHealthReportOcrService _ocr;
    private readonly IWebHostEnvironment _env;

    public HealthCheckupQuarterController(
        IQuarterHealthReportService svc,
        IHealthReportOcrService ocr,
        IWebHostEnvironment env)
    {
        _svc = svc;
        _ocr = ocr;
        _env = env;
    }

    // === Request / Response Records ===

    public record ManualUploadRequest(
        string ReportDate,
        string? HospitalName,
        QuarterReportValues Values,
        QuarterReportFlags Flags,
        string? SourceFileName,
        string? SourceFilePath,
        string? OcrJsonRaw);

    // === POST /api/health/checkup/qtr/upload — 上傳健檢報告 JPG + OCR 解析 ===
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile? file, [FromForm] string? reportDate,
        [FromForm] string? hospitalName, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "請選擇檔案。" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
            return BadRequest(new { error = "僅支援 JPG / PNG 格式。" });

        // 1. 儲存檔案
        var uploadDir = Path.Combine(_env.ContentRootPath, "data", "health", "qtr");
        Directory.CreateDirectory(uploadDir);

        var originalName = file.FileName;
        var safeName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
        var savedPath = Path.Combine(uploadDir, safeName);

        using (var stream = new FileStream(savedPath, FileMode.Create))
        {
            await file.CopyToAsync(stream, ct);
        }

        // 2. OCR 解析
        OcrParseResult ocrResult;
        using (var ocrStream = new FileStream(savedPath, FileMode.Open, FileAccess.Read))
        {
            ocrResult = await _ocr.ParseImageAsync(ocrStream, originalName, ct);
        }

        // 3. 回傳解析結果供前端確認
        return Ok(new
        {
            success = ocrResult.Success,
            message = ocrResult.Success
                ? "檔案已上傳並完成 OCR 解析，請確認以下數值後儲存。"
                : $"檔案已上傳，但 OCR 解析部分失敗：{ocrResult.ErrorMessage}。請手動輸入數值。",
            sourceFileName = originalName,
            sourceFilePath = savedPath,
            reportDate = ocrResult.DetectedReportDate ?? reportDate ?? "",
            hospitalName = ocrResult.DetectedHospitalName ?? hospitalName ?? "廖內科",
            values = ocrResult.Values,
            flags = ocrResult.Flags,
            rawText = ocrResult.RawText
        });
    }

    // === POST /api/health/checkup/qtr/manual — 確認數值並儲存報告 ===
    [HttpPost("manual")]
    public async Task<IActionResult> ManualSave([FromBody] ManualUploadRequest req, CancellationToken ct)
    {
        if (!DateTime.TryParse(req.ReportDate, out var reportDate))
            return BadRequest(new { error = "報告日期格式錯誤，請使用 YYYY-MM-DD 格式。" });

        var hospital = string.IsNullOrWhiteSpace(req.HospitalName) ? "廖內科" : req.HospitalName;

        // 自動判斷異常旗標
        var flags = req.Flags ?? new QuarterReportFlags();
        flags.TriglycerideHigh ??= req.Values.Triglyceride > 150;
        flags.HDLLow ??= req.Values.HDL < 40;
        flags.AcSugarHigh ??= req.Values.AcSugar > 100;
        flags.Hba1cHigh ??= req.Values.Hba1c > 5.6m;

        var result = await _svc.UploadAndSaveAsync(
            reportDate, hospital,
            req.Values, flags,
            req.SourceFileName, req.SourceFilePath, req.OcrJsonRaw, ct);

        return Ok(result);
    }

    // === GET /api/health/checkup/qtr/{reportId} — 取得單次報告明細 ===
    [HttpGet("{reportId:long}")]
    public async Task<IActionResult> GetById(long reportId, CancellationToken ct)
    {
        var dto = await _svc.GetByIdAsync(reportId, ct);
        if (dto == null) return NotFound(new { error = "找不到此報告。" });
        return Ok(dto);
    }

    // === GET /api/health/checkup/qtr/list — 歷史報告列表 ===
    [HttpGet("list")]
    public async Task<IActionResult> GetList([FromQuery] string? from, [FromQuery] string? to, CancellationToken ct)
    {
        DateTime? fromDate = null, toDate = null;
        if (!string.IsNullOrWhiteSpace(from) && DateTime.TryParse(from, out var f)) fromDate = f;
        if (!string.IsNullOrWhiteSpace(to) && DateTime.TryParse(to, out var t)) toDate = t;

        var list = await _svc.GetListAsync(fromDate, toDate, ct);
        return Ok(list);
    }

    // === DELETE /api/health/checkup/qtr/{reportId} — 刪除報告 ===
    [HttpDelete("{reportId:long}")]
    public async Task<IActionResult> Delete(long reportId, CancellationToken ct)
    {
        var existing = await _svc.GetByIdAsync(reportId, ct);
        if (existing == null) return NotFound(new { error = "找不到此報告。" });

        await _svc.DeleteAsync(reportId, ct);
        return Ok(new { message = "已刪除。" });
    }
}
