using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HSPAS.Api.Entities;

/// <summary>一般交易紀錄（買進/賣出/股利）</summary>
[Table("TradeRecord")]
public class TradeRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Column(TypeName = "date")]
    public DateTime TradeDate { get; set; }

    [MaxLength(10)]
    public string StockId { get; set; } = string.Empty;

    [MaxLength(50)]
    public string StockName { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Action { get; set; } = string.Empty; // BUY, SELL, DIVIDEND

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

    [MaxLength(200)]
    public string? Note { get; set; }

    public DateTime CreateTime { get; set; } = DateTime.UtcNow;
}
