using HSPAS.Api.Services.Interfaces;

namespace HSPAS.Api.Services.Interfaces;

public interface IHealthReportOcrService
{
    /// <summary>解析健檢報告影像，回傳辨識出的檢驗數值</summary>
    Task<OcrParseResult> ParseImageAsync(Stream imageStream, string fileName, CancellationToken ct = default);
}

public class OcrParseResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string RawText { get; set; } = string.Empty;
    public string? DetectedReportDate { get; set; }
    public string? DetectedHospitalName { get; set; }
    public QuarterReportValues Values { get; set; } = new();
    public QuarterReportFlags Flags { get; set; } = new();
}
