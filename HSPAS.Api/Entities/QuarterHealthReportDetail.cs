using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HSPAS.Api.Entities;

/// <summary>每三個月健檢報告明細（固定欄位模式）</summary>
[Table("QuarterHealthReportDetail")]
public class QuarterHealthReportDetail
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public long ReportId { get; set; }

    // 脂質
    [Column(TypeName = "decimal(6,2)")]
    public decimal? TCholesterol { get; set; }      // mg/dL

    [Column(TypeName = "decimal(6,2)")]
    public decimal? Triglyceride { get; set; }       // mg/dL

    [Column(TypeName = "decimal(6,2)")]
    public decimal? HDL { get; set; }                // mg/dL

    // 肝功能
    [Column(TypeName = "decimal(6,2)")]
    public decimal? SGPT_ALT { get; set; }           // U/L

    // 腎功能
    [Column(TypeName = "decimal(6,2)")]
    public decimal? Creatinine { get; set; }         // mg/dL

    [Column(TypeName = "decimal(6,2)")]
    public decimal? UricAcid { get; set; }           // mg/dL

    [Column(TypeName = "decimal(6,2)")]
    public decimal? MDRD_EGFR { get; set; }          // mL/min/1.73m²

    [Column(TypeName = "decimal(6,2)")]
    public decimal? CKDEPI_EGFR { get; set; }        // mL/min/1.73m²

    // 血糖
    [Column(TypeName = "decimal(6,2)")]
    public decimal? AcSugar { get; set; }            // mg/dL

    [Column(TypeName = "decimal(4,2)")]
    public decimal? Hba1c { get; set; }              // %

    // 異常旗標
    public bool? TriglycerideHigh { get; set; }
    public bool? HDLLow { get; set; }
    public bool? AcSugarHigh { get; set; }
    public bool? Hba1cHigh { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("ReportId")]
    public QuarterHealthReport Report { get; set; } = null!;
}
