using HSPAS.Api.Infrastructure;
using HSPAS.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HSPAS.Api.Services;

public class UsPortfolioService : IUsPortfolioService
{
    private readonly HspasDbContext _db;

    public UsPortfolioService(HspasDbContext db) => _db = db;

    public async Task<UsHoldingsSummary> GetHoldingsAsync(CancellationToken ct = default)
    {
        var trades = await _db.UsTradeRecords.ToListAsync(ct);
        var grouped = trades.GroupBy(t => t.StockSymbol).Select(g =>
        {
            var buys = g.Where(t => t.Action == "BUY").Sum(t => t.Quantity);
            var sells = g.Where(t => t.Action == "SELL").Sum(t => t.Quantity);
            var qty = buys - sells;
            var totalCost = g.Where(t => t.Action == "BUY")
                .Sum(t => t.Amount + t.Fee + t.Tax);
            return new { Symbol = g.Key, Name = g.First().StockName, Qty = qty, TotalCost = totalCost };
        }).Where(x => x.Qty > 0).ToList();

        if (grouped.Count == 0) return new UsHoldingsSummary();

        // Get latest price from most recent trade for each symbol
        var latestPrices = new Dictionary<string, decimal>();
        foreach (var g in grouped)
        {
            var latestTrade = await _db.UsTradeRecords
                .Where(t => t.StockSymbol == g.Symbol && (t.Action == "BUY" || t.Action == "SELL"))
                .OrderByDescending(t => t.TradeDate)
                .ThenByDescending(t => t.Id)
                .FirstOrDefaultAsync(ct);
            if (latestTrade != null)
                latestPrices[g.Symbol] = latestTrade.Price;
        }

        var items = grouped.Select(g =>
        {
            var price = latestPrices.GetValueOrDefault(g.Symbol);
            var mv = price * g.Qty;
            return new UsHoldingItem
            {
                StockSymbol = g.Symbol,
                StockName = g.Name,
                Quantity = g.Qty,
                LastPrice = price > 0 ? price : null,
                MarketValue = mv
            };
        }).OrderByDescending(x => x.MarketValue).ToList();

        var totalMV = items.Sum(x => x.MarketValue);
        foreach (var i in items) i.WeightRatio = totalMV > 0 ? i.MarketValue / totalMV : 0;

        var totalCostAll = grouped.Sum(g => g.TotalCost);
        return new UsHoldingsSummary
        {
            TotalMarketValue = totalMV,
            TotalCost = totalCostAll,
            TotalUnrealizedPnL = totalMV - totalCostAll,
            TotalUnrealizedReturn = totalCostAll > 0 ? (totalMV - totalCostAll) / totalCostAll : 0,
            Items = items
        };
    }

    public async Task<UsStockUnrealized?> GetStockUnrealizedAsync(string symbol, CancellationToken ct = default)
    {
        var trades = await _db.UsTradeRecords.Where(t => t.StockSymbol == symbol).ToListAsync(ct);
        if (trades.Count == 0) return null;

        var buys = trades.Where(t => t.Action == "BUY").ToList();
        var sells = trades.Where(t => t.Action == "SELL").ToList();
        var qty = buys.Sum(t => t.Quantity) - sells.Sum(t => t.Quantity);
        if (qty <= 0) return null;

        var totalCost = buys.Sum(t => t.Amount + t.Fee + t.Tax);
        var avgCost = totalCost / qty;

        // Latest price from most recent trade
        var latestTrade = trades
            .Where(t => t.Action == "BUY" || t.Action == "SELL")
            .OrderByDescending(t => t.TradeDate)
            .ThenByDescending(t => t.Id)
            .FirstOrDefault();

        var price = latestTrade?.Price ?? 0;
        var mv = price * qty;
        return new UsStockUnrealized
        {
            StockSymbol = symbol,
            StockName = trades.First().StockName,
            CurrentQty = qty,
            AvgCost = avgCost,
            LastPrice = price > 0 ? price : null,
            MarketValue = mv,
            TotalCost = totalCost,
            UnrealizedPnL = mv - totalCost,
            UnrealizedReturn = totalCost > 0 ? (mv - totalCost) / totalCost : 0
        };
    }

    public async Task<UsPortfolioSummary> GetUnrealizedSummaryAsync(CancellationToken ct = default)
    {
        var h = await GetHoldingsAsync(ct);
        return new UsPortfolioSummary
        {
            TotalCost = h.TotalCost,
            TotalMarketValue = h.TotalMarketValue,
            TotalUnrealizedPnL = h.TotalUnrealizedPnL,
            TotalUnrealizedReturn = h.TotalUnrealizedReturn
        };
    }
}
