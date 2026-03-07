using HSPAS.Api.Entities;
using HSPAS.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HSPAS.Api.Controllers;

[ApiController]
[Route("api/trades")]
public class TradesController : ControllerBase
{
    private readonly HspasDbContext _db;

    public TradesController(HspasDbContext db) => _db = db;

    public record CreateTradeRequest(
        string TradeDate, string StockId, string StockName, string Action,
        int Quantity, decimal Price, decimal Fee, decimal Tax,
        decimal? OtherCost, string? Note);

    public record UpdateTradeRequest(
        string? TradeDate, string? StockName, string? Action,
        int? Quantity, decimal? Price, decimal? Fee, decimal? Tax,
        decimal? OtherCost, string? Note);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTradeRequest req, CancellationToken ct)
    {
        if (!DateTime.TryParse(req.TradeDate, out var tradeDate))
            return BadRequest(new { error = "交易日期格式錯誤。" });

        var other = req.OtherCost ?? 0m;
        decimal netAmount = req.Action.ToUpper() switch
        {
            "BUY" => -(req.Price * req.Quantity + req.Fee + req.Tax + other),
            "SELL" => +(req.Price * req.Quantity - req.Fee - req.Tax - other),
            "DIVIDEND" => +(req.Price * req.Quantity),
            _ => 0m
        };

        var entity = new TradeRecord
        {
            TradeDate = tradeDate,
            StockId = req.StockId,
            StockName = req.StockName,
            Action = req.Action.ToUpper(),
            Quantity = req.Quantity,
            Price = req.Price,
            Fee = req.Fee,
            Tax = req.Tax,
            OtherCost = req.OtherCost,
            NetAmount = netAmount,
            Note = req.Note
        };

        _db.TradeRecords.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new { entity.Id, entity.NetAmount });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateTradeRequest req, CancellationToken ct)
    {
        var entity = await _db.TradeRecords.FindAsync(new object[] { id }, ct);
        if (entity == null) return NotFound();

        if (req.TradeDate != null && DateTime.TryParse(req.TradeDate, out var td)) entity.TradeDate = td;
        if (req.StockName != null) entity.StockName = req.StockName;
        if (req.Action != null) entity.Action = req.Action.ToUpper();
        if (req.Quantity.HasValue) entity.Quantity = req.Quantity.Value;
        if (req.Price.HasValue) entity.Price = req.Price.Value;
        if (req.Fee.HasValue) entity.Fee = req.Fee.Value;
        if (req.Tax.HasValue) entity.Tax = req.Tax.Value;
        if (req.OtherCost.HasValue) entity.OtherCost = req.OtherCost.Value;
        if (req.Note != null) entity.Note = req.Note;

        // 重算 NetAmount
        var other = entity.OtherCost ?? 0m;
        entity.NetAmount = entity.Action switch
        {
            "BUY" => -(entity.Price * entity.Quantity + entity.Fee + entity.Tax + other),
            "SELL" => +(entity.Price * entity.Quantity - entity.Fee - entity.Tax - other),
            "DIVIDEND" => +(entity.Price * entity.Quantity),
            _ => 0m
        };

        await _db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var entity = await _db.TradeRecords.FindAsync(new object[] { id }, ct);
        if (entity == null) return NotFound();
        _db.TradeRecords.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(new { message = "已刪除" });
    }

    /// <summary>查詢全部交易紀錄（可選篩選條件）</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? stockId,
        [FromQuery] string? from,
        [FromQuery] string? to,
        CancellationToken ct)
    {
        IQueryable<TradeRecord> query = _db.TradeRecords;

        if (!string.IsNullOrWhiteSpace(stockId))
            query = query.Where(t => t.StockId == stockId);
        if (DateTime.TryParse(from, out var f))
            query = query.Where(t => t.TradeDate >= f.Date);
        if (DateTime.TryParse(to, out var t2))
            query = query.Where(t => t.TradeDate <= t2.Date);

        var items = await query.OrderByDescending(t => t.TradeDate).ThenBy(t => t.StockId).ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("{stockId}")]
    public async Task<IActionResult> GetByStock(
        string stockId,
        [FromQuery] string? from,
        [FromQuery] string? to,
        CancellationToken ct)
    {
        var query = _db.TradeRecords.Where(t => t.StockId == stockId);

        if (DateTime.TryParse(from, out var f))
            query = query.Where(t => t.TradeDate >= f.Date);
        if (DateTime.TryParse(to, out var t2))
            query = query.Where(t => t.TradeDate <= t2.Date);

        var items = await query.OrderByDescending(t => t.TradeDate).ToListAsync(ct);
        return Ok(items);
    }

    /// <summary>依股票代號查詢名稱（從最新行情資料取得）</summary>
    [HttpGet("stock-name/{stockId}")]
    public async Task<IActionResult> GetStockName(string stockId, CancellationToken ct)
    {
        // 先查交易紀錄中是否已有
        var fromTrade = await _db.TradeRecords
            .Where(t => t.StockId == stockId)
            .Select(t => t.StockName)
            .FirstOrDefaultAsync(ct);
        if (!string.IsNullOrEmpty(fromTrade))
            return Ok(new { stockName = fromTrade });

        // 再查每日行情
        var fromPrice = await _db.DailyStockPrices
            .Where(d => d.StockId == stockId)
            .OrderByDescending(d => d.TradeDate)
            .Select(d => d.StockName)
            .FirstOrDefaultAsync(ct);
        if (!string.IsNullOrEmpty(fromPrice))
            return Ok(new { stockName = fromPrice });

        // 再查 ETF
        var fromEtf = await _db.EtfInfos
            .Where(e => e.EtfId == stockId)
            .Select(e => e.EtfName)
            .FirstOrDefaultAsync(ct);
        if (!string.IsNullOrEmpty(fromEtf))
            return Ok(new { stockName = fromEtf });

        return Ok(new { stockName = "" });
    }
}
