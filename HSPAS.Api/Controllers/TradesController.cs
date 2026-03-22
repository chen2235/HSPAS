using HSPAS.Api.Entities;
using HSPAS.Api.Infrastructure;
using HSPAS.Api.Services;
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

    /// <summary>上傳與解析國泰證日對帳單 PDF</summary>
    [HttpPost("cathay-daily-statement/parse")]
    public async Task<IActionResult> ParseCathayStatement(
        IFormFile file,
        [FromForm] string? password,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "請選擇 PDF 檔案。" });

        const long maxSize = 5 * 1024 * 1024; // 5 MB
        if (file.Length > maxSize)
            return BadRequest(new { error = "檔案大小不可超過 5 MB。" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".pdf" && ext != ".jpg" && ext != ".jpeg" && ext != ".png")
            return BadRequest(new { error = "僅支援 PDF / JPG / PNG 格式。" });

        var pwd = string.IsNullOrWhiteSpace(password) ? "A120683373" : password;

        using var stream = file.OpenReadStream();
        var parser = new CathayStatementParserService();
        var result = parser.Parse(stream, pwd);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        // Resolve stockId from stockName via DB
        var resolvedItems = new List<object>();
        foreach (var item in result.Items)
        {
            var stockId = await ResolveStockId(item.StockName, ct);
            resolvedItems.Add(new
            {
                item.TradeDate,
                stockId,
                item.StockName,
                item.Action,
                item.Quantity,
                item.Price,
                item.Fee,
                item.Tax,
                item.OtherCost,
                item.NetAmount,
                item.CustomerReceivablePayableRaw
            });
        }

        return Ok(resolvedItems);
    }

    private async Task<string> ResolveStockId(string stockName, CancellationToken ct)
    {
        // Look up stockId by stockName in DailyStockPrices
        var fromPrice = await _db.DailyStockPrices
            .Where(d => d.StockName == stockName)
            .OrderByDescending(d => d.TradeDate)
            .Select(d => d.StockId)
            .FirstOrDefaultAsync(ct);
        if (!string.IsNullOrEmpty(fromPrice)) return fromPrice;

        // Look up in TradeRecords
        var fromTrade = await _db.TradeRecords
            .Where(t => t.StockName == stockName)
            .Select(t => t.StockId)
            .FirstOrDefaultAsync(ct);
        if (!string.IsNullOrEmpty(fromTrade)) return fromTrade;

        // Look up in EtfInfos
        var fromEtf = await _db.EtfInfos
            .Where(e => e.EtfName == stockName)
            .Select(e => e.EtfId)
            .FirstOrDefaultAsync(ct);
        if (!string.IsNullOrEmpty(fromEtf)) return fromEtf;

        return "";
    }

    public class BatchTradeItem
    {
        public string TradeDate { get; set; } = "";
        public string StockId { get; set; } = "";
        public string StockName { get; set; } = "";
        public string Action { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Fee { get; set; }
        public decimal Tax { get; set; }
        public decimal? OtherCost { get; set; }
        public string? Note { get; set; }
    }

    public class BatchCreateRequest
    {
        public List<BatchTradeItem> Items { get; set; } = new();
    }

    /// <summary>批次新增多筆交易紀錄</summary>
    [HttpPost("batch")]
    public async Task<IActionResult> BatchCreate([FromBody] BatchCreateRequest req, CancellationToken ct)
    {
        if (req.Items == null || req.Items.Count == 0)
            return BadRequest(new { error = "交易明細不可為空。" });

        var successCount = 0;
        var errors = new List<object>();

        foreach (var (item, idx) in req.Items.Select((v, i) => (v, i)))
        {
            if (!DateTime.TryParse(item.TradeDate, out var td))
            {
                errors.Add(new { index = idx, error = $"交易日期格式錯誤：{item.TradeDate}" });
                continue;
            }
            if (string.IsNullOrWhiteSpace(item.StockId))
            {
                errors.Add(new { index = idx, error = "股票代號不可為空" });
                continue;
            }
            if (item.Quantity <= 0)
            {
                errors.Add(new { index = idx, error = "股數必須大於 0" });
                continue;
            }

            var other = item.OtherCost ?? 0m;
            decimal netAmount = item.Action.ToUpper() switch
            {
                "BUY" => -(item.Price * item.Quantity + item.Fee + item.Tax + other),
                "SELL" => +(item.Price * item.Quantity - item.Fee - item.Tax - other),
                "DIVIDEND" => +(item.Price * item.Quantity),
                _ => 0m
            };

            var entity = new TradeRecord
            {
                TradeDate = td,
                StockId = item.StockId,
                StockName = item.StockName,
                Action = item.Action.ToUpper(),
                Quantity = item.Quantity,
                Price = item.Price,
                Fee = item.Fee,
                Tax = item.Tax,
                OtherCost = item.OtherCost,
                NetAmount = netAmount,
                Note = item.Note
            };

            _db.TradeRecords.Add(entity);
            successCount++;
        }

        if (successCount > 0)
            await _db.SaveChangesAsync(ct);

        return Ok(new { successCount, errorCount = errors.Count, errors });
    }
}
