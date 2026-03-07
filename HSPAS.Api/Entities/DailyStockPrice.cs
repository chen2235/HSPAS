using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HSPAS.Api.Entities;

/// <summary>每日個股行情資料（複合主鍵：TradeDate + StockId）</summary>
[Table("DailyStockPrice")]
public class DailyStockPrice
{
    [Column(TypeName = "date")]
    public DateTime TradeDate { get; set; }

    [MaxLength(10)]
    public string StockId { get; set; } = string.Empty;

    [MaxLength(50)]
    public string StockName { get; set; } = string.Empty;

    public long? TradeVolume { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal? TradeValue { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal? OpenPrice { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal? HighPrice { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal? LowPrice { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal? ClosePrice { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal? PriceChange { get; set; }

    public int? Transaction { get; set; }

    public DateTime CreateTime { get; set; } = DateTime.UtcNow;
}
