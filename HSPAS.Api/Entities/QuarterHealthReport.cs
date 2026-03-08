using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HSPAS.Api.Entities;

/// <summary>每三個月健檢報告主表</summary>
[Table("QuarterHealthReport")]
public class QuarterHealthReport
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Column(TypeName = "date")]
    public DateTime ReportDate { get; set; }

    [MaxLength(100)]
    public string? HospitalName { get; set; }

    [MaxLength(260)]
    public string? SourceFileName { get; set; }

    [MaxLength(500)]
    public string? SourceFilePath { get; set; }

    public string? OcrJsonRaw { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public QuarterHealthReportDetail? Detail { get; set; }
}
