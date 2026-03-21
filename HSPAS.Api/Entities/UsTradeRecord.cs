using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HSPAS.Api.Entities;

/// <summary>美股交易紀錄（複委託）</summary>
[Table("US_TradeRecord")]
public class UsTradeRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Column(TypeName = "date")]
    public DateTime TradeDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime? SettlementDate { get; set; }

    [MaxLength(20)]
    public string StockSymbol { get; set; } = string.Empty;

    [MaxLength(100)]
    public string StockName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Market { get; set; } = "美國";

    [MaxLength(10)]
    public string Action { get; set; } = string.Empty; // BUY, SELL, DIVIDEND

    [MaxLength(5)]
    public string Currency { get; set; } = "USD";

    [Column(TypeName = "decimal(19,6)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(19,6)")]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal Fee { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal Tax { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal NetAmount { get; set; }

    [MaxLength(5)]
    public string? SettlementCurrency { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal? ExchangeRate { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal? NetAmountTwd { get; set; }

    [MaxLength(20)]
    public string? TradeRef { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    public DateTime CreateTime { get; set; } = DateTime.UtcNow;
}
