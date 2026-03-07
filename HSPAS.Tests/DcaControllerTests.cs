using HSPAS.Api.Controllers;
using HSPAS.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HSPAS.Tests;

public class DcaControllerTests : IDisposable
{
    private readonly HspasDbContext _db;
    private readonly DcaController _controller;

    public DcaControllerTests()
    {
        var options = new DbContextOptionsBuilder<HspasDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new HspasDbContext(options);
        _controller = new DcaController(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreatePlan_ShouldSucceed()
    {
        var req = new DcaController.CreatePlanRequest(
            "存台積電", "2330", "台積電", "2026-01-01", null, "MONTHLY", 6, 3000m, null);

        var result = await _controller.CreatePlan(req, CancellationToken.None) as OkObjectResult;
        Assert.NotNull(result);

        var plans = await _db.DcaPlans.ToListAsync();
        Assert.Single(plans);
        Assert.Equal("存台積電", plans[0].PlanName);
        Assert.True(plans[0].IsActive);
    }

    [Fact]
    public async Task CreateExecution_Success_ShouldAlsoCreateTradeRecord()
    {
        // 先建立 plan
        var planReq = new DcaController.CreatePlanRequest(
            "存台積電", "2330", "台積電", "2026-01-01", null, "MONTHLY", 6, 3000m, null);
        var planResult = await _controller.CreatePlan(planReq, CancellationToken.None) as OkObjectResult;

        var plan = await _db.DcaPlans.FirstAsync();

        // 建立 SUCCESS execution
        var execReq = new DcaController.CreateExecutionRequest(
            "2026-02-06", 5, 600m, 4m, 0m, null, "SUCCESS", "二月扣款");
        var result = await _controller.CreateExecution(plan.Id, execReq, CancellationToken.None) as OkObjectResult;
        Assert.NotNull(result);

        // 驗證 DcaExecution 已建立
        var execs = await _db.DcaExecutions.ToListAsync();
        Assert.Single(execs);
        Assert.Equal("SUCCESS", execs[0].Status);

        // 驗證 TradeRecord 也同步建立
        var trades = await _db.TradeRecords.ToListAsync();
        Assert.Single(trades);
        Assert.Equal("BUY", trades[0].Action);
        Assert.Equal("2330", trades[0].StockId);
        Assert.StartsWith("[DCA]", trades[0].Note);
    }

    [Fact]
    public async Task CreateExecution_Failed_ShouldNotCreateTradeRecord()
    {
        var planReq = new DcaController.CreatePlanRequest(
            "存台積電", "2330", "台積電", "2026-01-01", null, "MONTHLY", 6, 3000m, null);
        await _controller.CreatePlan(planReq, CancellationToken.None);
        var plan = await _db.DcaPlans.FirstAsync();

        var execReq = new DcaController.CreateExecutionRequest(
            "2026-02-06", 0, 0m, 0m, 0m, null, "FAILED", "扣款失敗");
        await _controller.CreateExecution(plan.Id, execReq, CancellationToken.None);

        var trades = await _db.TradeRecords.ToListAsync();
        Assert.Empty(trades);
    }
}
