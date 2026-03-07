using HSPAS.Api.Entities;
using HSPAS.Api.Infrastructure;
using HSPAS.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace HSPAS.Tests;

public class PortfolioServiceTests : IDisposable
{
    private readonly HspasDbContext _db;
    private readonly PortfolioService _svc;

    public PortfolioServiceTests()
    {
        var options = new DbContextOptionsBuilder<HspasDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new HspasDbContext(options);
        _svc = new PortfolioService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetHoldings_NoTrades_ShouldReturnEmpty()
    {
        var result = await _svc.GetHoldingsAsync();
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalMarketValue);
    }

    [Fact]
    public async Task GetHoldings_WithBuyTrade_ShouldReturnHolding()
    {
        _db.TradeRecords.Add(new TradeRecord
        {
            TradeDate = new DateTime(2026, 1, 1),
            StockId = "2330",
            StockName = "台積電",
            Action = "BUY",
            Quantity = 1000,
            Price = 600m,
            Fee = 855m,
            Tax = 0m,
            NetAmount = -(600m * 1000 + 855m)
        });
        _db.DailyStockPrices.Add(new DailyStockPrice
        {
            TradeDate = new DateTime(2026, 3, 1),
            StockId = "2330",
            StockName = "台積電",
            ClosePrice = 650m
        });
        await _db.SaveChangesAsync();

        var result = await _svc.GetHoldingsAsync();
        Assert.Single(result.Items);
        Assert.Equal("2330", result.Items[0].StockId);
        Assert.Equal(1000, result.Items[0].Quantity);
        Assert.Equal(650000m, result.Items[0].MarketValue);
    }

    [Fact]
    public async Task GetHoldings_BuyAndSell_ShouldNetQuantity()
    {
        _db.TradeRecords.Add(new TradeRecord
        {
            TradeDate = new DateTime(2026, 1, 1),
            StockId = "2330", StockName = "台積電", Action = "BUY",
            Quantity = 1000, Price = 600m, Fee = 0m, Tax = 0m, NetAmount = -600000m
        });
        _db.TradeRecords.Add(new TradeRecord
        {
            TradeDate = new DateTime(2026, 2, 1),
            StockId = "2330", StockName = "台積電", Action = "SELL",
            Quantity = 500, Price = 650m, Fee = 0m, Tax = 0m, NetAmount = 325000m
        });
        _db.DailyStockPrices.Add(new DailyStockPrice
        {
            TradeDate = new DateTime(2026, 3, 1),
            StockId = "2330", StockName = "台積電", ClosePrice = 700m
        });
        await _db.SaveChangesAsync();

        var result = await _svc.GetHoldingsAsync();
        Assert.Single(result.Items);
        Assert.Equal(500, result.Items[0].Quantity);
    }

    [Fact]
    public async Task GetHoldings_AllSold_ShouldReturnEmpty()
    {
        _db.TradeRecords.Add(new TradeRecord
        {
            TradeDate = new DateTime(2026, 1, 1),
            StockId = "2330", StockName = "台積電", Action = "BUY",
            Quantity = 1000, Price = 600m, Fee = 0m, Tax = 0m, NetAmount = -600000m
        });
        _db.TradeRecords.Add(new TradeRecord
        {
            TradeDate = new DateTime(2026, 2, 1),
            StockId = "2330", StockName = "台積電", Action = "SELL",
            Quantity = 1000, Price = 650m, Fee = 0m, Tax = 0m, NetAmount = 650000m
        });
        await _db.SaveChangesAsync();

        var result = await _svc.GetHoldingsAsync();
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetStockUnrealized_NoTrades_ShouldReturnNull()
    {
        var result = await _svc.GetStockUnrealizedAsync("9999");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetStockUnrealized_ShouldCalculateCorrectly()
    {
        _db.TradeRecords.Add(new TradeRecord
        {
            TradeDate = new DateTime(2026, 1, 1),
            StockId = "2330", StockName = "台積電", Action = "BUY",
            Quantity = 1000, Price = 600m, Fee = 855m, Tax = 0m, NetAmount = -600855m
        });
        _db.DailyStockPrices.Add(new DailyStockPrice
        {
            TradeDate = new DateTime(2026, 3, 1),
            StockId = "2330", StockName = "台積電", ClosePrice = 650m
        });
        await _db.SaveChangesAsync();

        var result = await _svc.GetStockUnrealizedAsync("2330");
        Assert.NotNull(result);
        Assert.Equal(1000, result.CurrentQty);
        Assert.Equal(600855m, result.TotalCost); // 600*1000 + 855
        Assert.Equal(650000m, result.MarketValue); // 650*1000
        Assert.Equal(650000m - 600855m, result.UnrealizedPnL);
    }

    [Fact]
    public async Task GetUnrealizedSummary_ShouldReturnPortfolioTotals()
    {
        _db.TradeRecords.Add(new TradeRecord
        {
            TradeDate = new DateTime(2026, 1, 1),
            StockId = "2330", StockName = "台積電", Action = "BUY",
            Quantity = 100, Price = 600m, Fee = 0m, Tax = 0m, NetAmount = -60000m
        });
        _db.DailyStockPrices.Add(new DailyStockPrice
        {
            TradeDate = new DateTime(2026, 3, 1),
            StockId = "2330", StockName = "台積電", ClosePrice = 650m
        });
        await _db.SaveChangesAsync();

        var result = await _svc.GetUnrealizedSummaryAsync();
        Assert.Equal(60000m, result.TotalCost);
        Assert.Equal(65000m, result.TotalMarketValue);
        Assert.Equal(5000m, result.TotalUnrealizedPnL);
    }
}
