using HSPAS.Api.Entities;
using HSPAS.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HSPAS.Api.Controllers;

[ApiController]
[Route("api/dca")]
public class DcaController : ControllerBase
{
    private readonly HspasDbContext _db;

    public DcaController(HspasDbContext db) => _db = db;

    public record CreatePlanRequest(string PlanName, string StockId, string StockName, string StartDate,
        string? EndDate, string CycleType, int CycleDay, decimal Amount, string? Note);

    public record UpdatePlanRequest(string? PlanName, bool? IsActive, string? EndDate, decimal? Amount, string? Note);

    [HttpPost("plans")]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanRequest req, CancellationToken ct)
    {
        if (!DateTime.TryParse(req.StartDate, out var start)) return BadRequest(new { error = "Invalid StartDate." });
        var entity = new DcaPlan
        {
            PlanName = req.PlanName, StockId = req.StockId, StockName = req.StockName,
            StartDate = start, EndDate = DateTime.TryParse(req.EndDate, out var e) ? e : null,
            CycleType = req.CycleType, CycleDay = req.CycleDay, Amount = req.Amount,
            IsActive = true, Note = req.Note
        };
        _db.DcaPlans.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(new { entity.Id });
    }

    [HttpPut("plans/{id}")]
    public async Task<IActionResult> UpdatePlan(long id, [FromBody] UpdatePlanRequest req, CancellationToken ct)
    {
        var plan = await _db.DcaPlans.FindAsync(new object[] { id }, ct);
        if (plan == null) return NotFound();
        if (req.PlanName != null) plan.PlanName = req.PlanName;
        if (req.IsActive.HasValue) plan.IsActive = req.IsActive.Value;
        if (req.EndDate != null && DateTime.TryParse(req.EndDate, out var ed)) plan.EndDate = ed;
        if (req.Amount.HasValue) plan.Amount = req.Amount.Value;
        if (req.Note != null) plan.Note = req.Note;
        await _db.SaveChangesAsync(ct);
        return Ok(plan);
    }

    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans(CancellationToken ct)
    {
        var plans = await _db.DcaPlans.OrderByDescending(p => p.IsActive).ThenByDescending(p => p.StartDate).ToListAsync(ct);
        return Ok(plans);
    }

    [HttpGet("plans/{id}")]
    public async Task<IActionResult> GetPlan(long id, CancellationToken ct)
    {
        var plan = await _db.DcaPlans.Include(p => p.Executions).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (plan == null) return NotFound();

        var executions = plan.Executions.Where(e => e.Status == "SUCCESS").ToList();
        var totalInvested = executions.Sum(e => Math.Abs(e.NetAmount));
        var totalQty = executions.Sum(e => e.Quantity);
        var avgCost = totalQty > 0 ? totalInvested / totalQty : 0;

        return Ok(new
        {
            plan.Id, plan.PlanName, plan.StockId, plan.StockName, plan.StartDate, plan.EndDate,
            plan.CycleType, plan.CycleDay, plan.Amount, plan.IsActive, plan.Note,
            performance = new { totalInvested, totalQty, avgCost }
        });
    }

    [HttpGet("plans/{id}/executions")]
    public async Task<IActionResult> GetExecutions(long id, CancellationToken ct)
    {
        var items = await _db.DcaExecutions.Where(e => e.PlanId == id)
            .OrderByDescending(e => e.TradeDate).ToListAsync(ct);
        return Ok(items);
    }

    public record CreateExecutionRequest(string TradeDate, int Quantity, decimal Price,
        decimal Fee, decimal Tax, decimal? OtherCost, string Status, string? Note);

    [HttpPost("plans/{id}/executions")]
    public async Task<IActionResult> CreateExecution(long id, [FromBody] CreateExecutionRequest req, CancellationToken ct)
    {
        var plan = await _db.DcaPlans.FindAsync(new object[] { id }, ct);
        if (plan == null) return NotFound();
        if (!DateTime.TryParse(req.TradeDate, out var tradeDate))
            return BadRequest(new { error = "Invalid TradeDate." });

        var other = req.OtherCost ?? 0m;
        var netAmount = -(req.Price * req.Quantity + req.Fee + req.Tax + other);

        var exec = new DcaExecution
        {
            PlanId = id,
            TradeDate = tradeDate,
            StockId = plan.StockId,
            Quantity = req.Quantity,
            Price = req.Price,
            Fee = req.Fee,
            Tax = req.Tax,
            OtherCost = req.OtherCost,
            NetAmount = netAmount,
            Status = req.Status.ToUpper(),
            Note = req.Note
        };
        _db.DcaExecutions.Add(exec);

        // 9.7: DCA 執行成功時同步寫入 TradeRecord（BUY 紀錄）
        if (exec.Status == "SUCCESS")
        {
            var trade = new TradeRecord
            {
                TradeDate = tradeDate,
                StockId = plan.StockId,
                StockName = plan.StockName,
                Action = "BUY",
                Quantity = req.Quantity,
                Price = req.Price,
                Fee = req.Fee,
                Tax = req.Tax,
                OtherCost = req.OtherCost,
                NetAmount = netAmount,
                Note = $"[DCA] {plan.PlanName} - {req.Note}"
            };
            _db.TradeRecords.Add(trade);
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { exec.Id, exec.Status });
    }
}
