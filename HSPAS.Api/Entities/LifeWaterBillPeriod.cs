using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HSPAS.Api.Entities;

/// <summary>水費帳單期別</summary>
[Table("Life_WaterBillPeriod")]
public class LifeWaterBillPeriod
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [MaxLength(200)]
    public string WaterAddress { get; set; } = null!;

    [MaxLength(20)]
    [Column(TypeName = "varchar(20)")]
    public string WaterNo { get; set; } = null!;

    [MaxLength(30)]
    [Column(TypeName = "varchar(30)")]
    public string MeterNo { get; set; } = null!;

    [Column(TypeName = "date")]
    public DateTime BillingStartDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime BillingEndDate { get; set; }

    public int? BillingDays { get; set; }

    [MaxLength(100)]
    public string? BillingPeriodText { get; set; }

    public int? TotalUsage { get; set; }

    public int CurrentUsage { get; set; }

    public int CurrentMeterReading { get; set; }

    public int PreviousMeterReading { get; set; }

    [Column(TypeName = "decimal(19,2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(19,2)")]
    public decimal? InvoiceAmount { get; set; }

    [MaxLength(100)]
    public string? TariffType { get; set; }

    public string? RawDetailJson { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }

    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    public DateTime? UpdateTime { get; set; }
}
