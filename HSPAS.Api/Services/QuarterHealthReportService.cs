using HSPAS.Api.Entities;
using HSPAS.Api.Infrastructure;
using HSPAS.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HSPAS.Api.Services;

public class QuarterHealthReportService : IQuarterHealthReportService
{
    private readonly HspasDbContext _db;
    private readonly ILogger<QuarterHealthReportService> _logger;

    public QuarterHealthReportService(HspasDbContext db, ILogger<QuarterHealthReportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<QuarterReportUploadResult> UploadAndSaveAsync(
        DateTime reportDate, string hospitalName,
        QuarterReportValues values, QuarterReportFlags flags,
        string? sourceFileName, string? sourceFilePath, string? ocrJsonRaw,
        CancellationToken ct = default)
    {
        var report = new QuarterHealthReport
        {
            ReportDate = reportDate.Date,
            HospitalName = hospitalName,
            SourceFileName = sourceFileName,
            SourceFilePath = sourceFilePath,
            OcrJsonRaw = ocrJsonRaw
        };
        _db.QuarterHealthReports.Add(report);
        await _db.SaveChangesAsync(ct);

        var detail = new QuarterHealthReportDetail
        {
            ReportId = report.Id,
            TCholesterol = values.TCholesterol,
            Triglyceride = values.Triglyceride,
            HDL = values.HDL,
            SGPT_ALT = values.SGPT_ALT,
            Creatinine = values.Creatinine,
            UricAcid = values.UricAcid,
            MDRD_EGFR = values.MDRD_EGFR,
            CKDEPI_EGFR = values.CKDEPI_EGFR,
            AcSugar = values.AcSugar,
            Hba1c = values.Hba1c,
            TriglycerideHigh = flags.TriglycerideHigh,
            HDLLow = flags.HDLLow,
            AcSugarHigh = flags.AcSugarHigh,
            Hba1cHigh = flags.Hba1cHigh
        };
        _db.QuarterHealthReportDetails.Add(detail);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("健檢報告已儲存: ReportId={Id}, Date={Date}", report.Id, reportDate.ToString("yyyy-MM-dd"));

        return new QuarterReportUploadResult
        {
            ReportId = report.Id,
            ReportDate = reportDate.ToString("yyyy-MM-dd"),
            HospitalName = hospitalName,
            Values = values,
            Flags = flags
        };
    }

    public async Task<QuarterReportDto?> GetByIdAsync(long reportId, CancellationToken ct = default)
    {
        var report = await _db.QuarterHealthReports
            .Include(r => r.Detail)
            .FirstOrDefaultAsync(r => r.Id == reportId, ct);

        if (report == null) return null;

        return MapToDto(report);
    }

    public async Task<List<QuarterReportListItem>> GetListAsync(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        IQueryable<QuarterHealthReport> query = _db.QuarterHealthReports.Include(r => r.Detail);

        if (from.HasValue) query = query.Where(r => r.ReportDate >= from.Value.Date);
        if (to.HasValue) query = query.Where(r => r.ReportDate <= to.Value.Date);

        var reports = await query.OrderByDescending(r => r.ReportDate).ToListAsync(ct);

        return reports.Select(r => new QuarterReportListItem
        {
            ReportId = r.Id,
            ReportDate = r.ReportDate.ToString("yyyy-MM-dd"),
            HospitalName = r.HospitalName,
            Values = MapValues(r.Detail),
            Flags = MapFlags(r.Detail)
        }).ToList();
    }

    public async Task DeleteAsync(long reportId, CancellationToken ct = default)
    {
        var report = await _db.QuarterHealthReports
            .Include(r => r.Detail)
            .FirstOrDefaultAsync(r => r.Id == reportId, ct);

        if (report == null) return;

        if (report.Detail != null)
            _db.QuarterHealthReportDetails.Remove(report.Detail);

        _db.QuarterHealthReports.Remove(report);
        await _db.SaveChangesAsync(ct);
    }

    private static QuarterReportDto MapToDto(QuarterHealthReport r) => new()
    {
        ReportId = r.Id,
        ReportDate = r.ReportDate.ToString("yyyy-MM-dd"),
        HospitalName = r.HospitalName,
        SourceFileName = r.SourceFileName,
        Values = MapValues(r.Detail),
        Flags = MapFlags(r.Detail)
    };

    private static QuarterReportValues MapValues(QuarterHealthReportDetail? d) => d == null ? new() : new()
    {
        TCholesterol = d.TCholesterol,
        Triglyceride = d.Triglyceride,
        HDL = d.HDL,
        SGPT_ALT = d.SGPT_ALT,
        Creatinine = d.Creatinine,
        UricAcid = d.UricAcid,
        MDRD_EGFR = d.MDRD_EGFR,
        CKDEPI_EGFR = d.CKDEPI_EGFR,
        AcSugar = d.AcSugar,
        Hba1c = d.Hba1c
    };

    private static QuarterReportFlags MapFlags(QuarterHealthReportDetail? d) => d == null ? new() : new()
    {
        TriglycerideHigh = d.TriglycerideHigh,
        HDLLow = d.HDLLow,
        AcSugarHigh = d.AcSugarHigh,
        Hba1cHigh = d.Hba1cHigh
    };
}
