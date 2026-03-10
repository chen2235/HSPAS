using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HSPAS.Api.Entities;

/// <summary>電費帳單期別</summary>
[Table("Life_ElectricityBillPeriod")]
public class LifeElectricityBillPeriod
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [MaxLength(200)]
    public string Address { get; set; } = null!;

    [MaxLength(20)]
    [Column(TypeName = "varchar(20)")]
    public string PowerNo { get; set; } = null!;

    [MaxLength(1)]
    [Column(TypeName = "char(1)")]
    public string? BlackoutGroup { get; set; }

    [Column(TypeName = "date")]
    public DateTime BillingStartDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime BillingEndDate { get; set; }

    public int BillingDays { get; set; }

    [MaxLength(100)]
    public string? BillingPeriodText { get; set; }

    [Column(TypeName = "date")]
    public DateTime ReadOrDebitDate { get; set; }

    public int Kwh { get; set; }

    [Column(TypeName = "decimal(9,2)")]
    public decimal? KwhPerDay { get; set; }

    [Column(TypeName = "decimal(9,4)")]
    public decimal? AvgPricePerKwh { get; set; }

    [Column(TypeName = "decimal(19,2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(19,2)")]
    public decimal? InvoiceAmount { get; set; }

    [MaxLength(100)]
    public string? TariffType { get; set; }

    public int? SharedMeterHouseholdCount { get; set; }

    [MaxLength(50)]
    public string? InvoicePeriod { get; set; }

    [MaxLength(20)]
    public string? InvoiceNo { get; set; }

    public string? RawDetailJson { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }

    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    public DateTime? UpdateTime { get; set; }
}
