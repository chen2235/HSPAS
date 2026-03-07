namespace HSPAS.Api.Services.Interfaces;

/// <summary>投資組合：持股、損益計算</summary>
public interface IPortfolioService
{
    Task<HoldingsSummary> GetHoldingsAsync(CancellationToken ct = default);
    Task<StockUnrealized?> GetStockUnrealizedAsync(string stockId, CancellationToken ct = default);
    Task<PortfolioUnrealizedSummary> GetUnrealizedSummaryAsync(CancellationToken ct = default);
}

public class HoldingItem
{
    public string StockId { get; set; } = "";
    public string StockName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal? LastClosePrice { get; set; }
    public decimal MarketValue { get; set; }
    public decimal WeightRatio { get; set; }
}

public class HoldingsSummary
{
    public decimal TotalMarketValue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalUnrealizedPnL { get; set; }
    public decimal TotalUnrealizedReturn { get; set; }
    public List<HoldingItem> Items { get; set; } = new();
}

public class StockUnrealized
{
    public string StockId { get; set; } = "";
    public string StockName { get; set; } = "";
    public int CurrentQty { get; set; }
    public decimal AvgCost { get; set; }
    public decimal? LastClosePrice { get; set; }
    public decimal MarketValue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal UnrealizedReturn { get; set; }
}

public class PortfolioUnrealizedSummary
{
    public decimal TotalCost { get; set; }
    public decimal TotalMarketValue { get; set; }
    public decimal TotalUnrealizedPnL { get; set; }
    public decimal TotalUnrealizedReturn { get; set; }
}
