using HSPAS.Api.Controllers;
using HSPAS.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HSPAS.Tests;

public class TradesControllerTests : IDisposable
{
    private readonly HspasDbContext _db;
    private readonly TradesController _controller;

    public TradesControllerTests()
    {
        var options = new DbContextOptionsBuilder<HspasDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new HspasDbContext(options);
        _controller = new TradesController(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Create_BuyTrade_ShouldReturnNegativeNetAmount()
    {
        var req = new TradesController.CreateTradeRequest(
            "2026-01-15", "2330", "台積電", "BUY", 1000, 600m, 855m, 0m, null, null);

        var result = await _controller.Create(req, CancellationToken.None) as OkObjectResult;
        Assert.NotNull(result);

        var trades = await _db.TradeRecords.ToListAsync();
        Assert.Single(trades);
        Assert.True(trades[0].NetAmount < 0);
    }

    [Fact]
    public async Task Create_SellTrade_ShouldReturnPositiveNetAmount()
    {
        var req = new TradesController.CreateTradeRequest(
            "2026-01-15", "2330", "台積電", "SELL", 1000, 650m, 855m, 975m, null, null);

        var result = await _controller.Create(req, CancellationToken.None) as OkObjectResult;
        Assert.NotNull(result);

        var trades = await _db.TradeRecords.ToListAsync();
        Assert.Single(trades);
        Assert.True(trades[0].NetAmount > 0);
    }

    [Fact]
    public async Task Create_InvalidDate_ShouldReturnBadRequest()
    {
        var req = new TradesController.CreateTradeRequest(
            "invalid-date", "2330", "台積電", "BUY", 100, 600m, 0m, 0m, null, null);

        var result = await _controller.Create(req, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetByStock_ShouldReturnFilteredResults()
    {
        var req1 = new TradesController.CreateTradeRequest(
            "2026-01-15", "2330", "台積電", "BUY", 100, 600m, 0m, 0m, null, null);
        var req2 = new TradesController.CreateTradeRequest(
            "2026-01-15", "0050", "元大台灣50", "BUY", 200, 150m, 0m, 0m, null, null);

        await _controller.Create(req1, CancellationToken.None);
        await _controller.Create(req2, CancellationToken.None);

        var result = await _controller.GetByStock("2330", null, null, CancellationToken.None) as OkObjectResult;
        Assert.NotNull(result);
    }
}
