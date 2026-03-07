using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HSPAS.Api.Entities;

/// <summary>定期定額執行紀錄</summary>
[Table("DcaExecution")]
public class DcaExecution
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public long PlanId { get; set; }

    [Column(TypeName = "date")]
    public DateTime TradeDate { get; set; }

    [MaxLength(10)]
    public string StockId { get; set; } = string.Empty;

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal Fee { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal Tax { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal? OtherCost { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal NetAmount { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = string.Empty; // SUCCESS, FAILED, PARTIAL

    [MaxLength(200)]
    public string? Note { get; set; }

    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(PlanId))]
    public DcaPlan Plan { get; set; } = null!;
}
