namespace HSPAS.Api.Services.Interfaces;

public interface IQuarterHealthReportService
{
    Task<QuarterReportUploadResult> UploadAndSaveAsync(
        DateTime reportDate, string hospitalName,
        QuarterReportValues values, QuarterReportFlags flags,
        string? sourceFileName, string? sourceFilePath, string? ocrJsonRaw,
        CancellationToken ct = default);

    Task<QuarterReportDto?> GetByIdAsync(long reportId, CancellationToken ct = default);

    Task<List<QuarterReportListItem>> GetListAsync(DateTime? from, DateTime? to, CancellationToken ct = default);

    Task DeleteAsync(long reportId, CancellationToken ct = default);
}

// DTO: 上傳結果
public class QuarterReportUploadResult
{
    public long ReportId { get; set; }
    public string ReportDate { get; set; } = string.Empty;
    public string HospitalName { get; set; } = string.Empty;
    public QuarterReportValues Values { get; set; } = new();
    public QuarterReportFlags Flags { get; set; } = new();
}

// DTO: 檢驗數值
public class QuarterReportValues
{
    public decimal? TCholesterol { get; set; }
    public decimal? Triglyceride { get; set; }
    public decimal? HDL { get; set; }
    public decimal? SGPT_ALT { get; set; }
    public decimal? Creatinine { get; set; }
    public decimal? UricAcid { get; set; }
    public decimal? MDRD_EGFR { get; set; }
    public decimal? CKDEPI_EGFR { get; set; }
    public decimal? AcSugar { get; set; }
    public decimal? Hba1c { get; set; }
}

// DTO: 異常旗標
public class QuarterReportFlags
{
    public bool? TriglycerideHigh { get; set; }
    public bool? HDLLow { get; set; }
    public bool? AcSugarHigh { get; set; }
    public bool? Hba1cHigh { get; set; }
}

// DTO: 單筆完整報告
public class QuarterReportDto
{
    public long ReportId { get; set; }
    public string ReportDate { get; set; } = string.Empty;
    public string? HospitalName { get; set; }
    public string? SourceFileName { get; set; }
    public QuarterReportValues Values { get; set; } = new();
    public QuarterReportFlags Flags { get; set; } = new();
}

// DTO: 列表項目
public class QuarterReportListItem
{
    public long ReportId { get; set; }
    public string ReportDate { get; set; } = string.Empty;
    public string? HospitalName { get; set; }
    public QuarterReportValues Values { get; set; } = new();
    public QuarterReportFlags Flags { get; set; } = new();
}
