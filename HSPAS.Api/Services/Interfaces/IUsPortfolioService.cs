namespace HSPAS.Api.Services.Interfaces;

public interface IUsPortfolioService
{
    Task<UsHoldingsSummary> GetHoldingsAsync(CancellationToken ct = default);
    Task<UsStockUnrealized?> GetStockUnrealizedAsync(string symbol, CancellationToken ct = default);
    Task<UsPortfolioSummary> GetUnrealizedSummaryAsync(CancellationToken ct = default);
}

public class UsHoldingItem
{
    public string StockSymbol { get; set; } = "";
    public string StockName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal? LastPrice { get; set; }
    public decimal MarketValue { get; set; }
    public decimal WeightRatio { get; set; }
}

public class UsHoldingsSummary
{
    public decimal TotalMarketValue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalUnrealizedPnL { get; set; }
    public decimal TotalUnrealizedReturn { get; set; }
    public List<UsHoldingItem> Items { get; set; } = new();
}

public class UsStockUnrealized
{
    public string StockSymbol { get; set; } = "";
    public string StockName { get; set; } = "";
    public decimal CurrentQty { get; set; }
    public decimal AvgCost { get; set; }
    public decimal? LastPrice { get; set; }
    public decimal MarketValue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal UnrealizedReturn { get; set; }
}

public class UsPortfolioSummary
{
    public decimal TotalCost { get; set; }
    public decimal TotalMarketValue { get; set; }
    public decimal TotalUnrealizedPnL { get; set; }
    public decimal TotalUnrealizedReturn { get; set; }
}
