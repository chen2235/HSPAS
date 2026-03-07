using HSPAS.Api.Infrastructure;
using HSPAS.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HSPAS.Api.Services;

/// <summary>投資組合計算：持股、市值、未實現損益</summary>
public class PortfolioService : IPortfolioService
{
    private readonly HspasDbContext _db;

    public PortfolioService(HspasDbContext db) => _db = db;

    public async Task<HoldingsSummary> GetHoldingsAsync(CancellationToken ct = default)
    {
        // 計算每檔持股：BUY/DIVIDEND 加、SELL 減
        var trades = await _db.TradeRecords.ToListAsync(ct);
        var grouped = trades.GroupBy(t => t.StockId).Select(g =>
        {
            var buys = g.Where(t => t.Action == "BUY").Sum(t => t.Quantity);
            var sells = g.Where(t => t.Action == "SELL").Sum(t => t.Quantity);
            var qty = buys - sells;
            var totalCost = g.Where(t => t.Action == "BUY")
                .Sum(t => t.Price * t.Quantity + t.Fee + t.Tax + (t.OtherCost ?? 0));
            return new { StockId = g.Key, StockName = g.First().StockName, Qty = qty, TotalCost = totalCost };
        }).Where(x => x.Qty > 0).ToList();

        if (grouped.Count == 0) return new HoldingsSummary();

        // 取最新收盤價
        var latestDate = await _db.DailyStockPrices.MaxAsync(d => (DateTime?)d.TradeDate, ct);
        var latestPrices = latestDate.HasValue
            ? await _db.DailyStockPrices.Where(d => d.TradeDate == latestDate.Value)
                .ToDictionaryAsync(d => d.StockId, d => d.ClosePrice, ct)
            : new Dictionary<string, decimal?>();

        var items = grouped.Select(g =>
        {
            var close = latestPrices.GetValueOrDefault(g.StockId);
            var mv = (close ?? 0) * g.Qty;
            return new HoldingItem
            {
                StockId = g.StockId,
                StockName = g.StockName,
                Quantity = g.Qty,
                LastClosePrice = close,
                MarketValue = mv
            };
        }).OrderByDescending(x => x.MarketValue).ToList();

        var totalMV = items.Sum(x => x.MarketValue);
        foreach (var i in items) i.WeightRatio = totalMV > 0 ? i.MarketValue / totalMV : 0;

        var totalCostAll = grouped.Sum(g => g.TotalCost);
        return new HoldingsSummary
        {
            TotalMarketValue = totalMV,
            TotalCost = totalCostAll,
            TotalUnrealizedPnL = totalMV - totalCostAll,
            TotalUnrealizedReturn = totalCostAll > 0 ? (totalMV - totalCostAll) / totalCostAll : 0,
            Items = items
        };
    }

    public async Task<StockUnrealized?> GetStockUnrealizedAsync(string stockId, CancellationToken ct = default)
    {
        var trades = await _db.TradeRecords.Where(t => t.StockId == stockId).ToListAsync(ct);
        if (trades.Count == 0) return null;

        var buys = trades.Where(t => t.Action == "BUY").ToList();
        var sells = trades.Where(t => t.Action == "SELL").ToList();
        var qty = buys.Sum(t => t.Quantity) - sells.Sum(t => t.Quantity);
        if (qty <= 0) return null;

        var totalCost = buys.Sum(t => t.Price * t.Quantity + t.Fee + t.Tax + (t.OtherCost ?? 0));
        var avgCost = totalCost / qty;

        var latest = await _db.DailyStockPrices
            .Where(d => d.StockId == stockId)
            .OrderByDescending(d => d.TradeDate)
            .FirstOrDefaultAsync(ct);

        var close = latest?.ClosePrice ?? 0;
        var mv = close * qty;
        return new StockUnrealized
        {
            StockId = stockId,
            StockName = trades.First().StockName,
            CurrentQty = qty,
            AvgCost = avgCost,
            LastClosePrice = close,
            MarketValue = mv,
            TotalCost = totalCost,
            UnrealizedPnL = mv - totalCost,
            UnrealizedReturn = totalCost > 0 ? (mv - totalCost) / totalCost : 0
        };
    }

    public async Task<PortfolioUnrealizedSummary> GetUnrealizedSummaryAsync(CancellationToken ct = default)
    {
        var h = await GetHoldingsAsync(ct);
        return new PortfolioUnrealizedSummary
        {
            TotalCost = h.TotalCost,
            TotalMarketValue = h.TotalMarketValue,
            TotalUnrealizedPnL = h.TotalUnrealizedPnL,
            TotalUnrealizedReturn = h.TotalUnrealizedReturn
        };
    }
}
