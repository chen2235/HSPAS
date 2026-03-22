using HSPAS.Api.Entities;
using HSPAS.Api.Infrastructure;
using HSPAS.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HSPAS.Api.Controllers;

[ApiController]
[Route("api/us/trades")]
public class UsTradesController : ControllerBase
{
    private readonly HspasDbContext _db;

    public UsTradesController(HspasDbContext db) => _db = db;

    public record CreateUsTradeRequest(
        string TradeDate, string StockSymbol, string StockName, string Action,
        decimal Quantity, decimal Price, decimal Fee, decimal Tax,
        string? Market, string? Currency, string? SettlementDate,
        string? SettlementCurrency, decimal? ExchangeRate, decimal? NetAmountTwd,
        string? TradeRef, string? Note);

    public record UpdateUsTradeRequest(
        string? TradeDate, string? StockName, string? Action,
        decimal? Quantity, decimal? Price, decimal? Fee, decimal? Tax,
        string? SettlementDate, string? SettlementCurrency,
        decimal? ExchangeRate, decimal? NetAmountTwd, string? Note);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUsTradeRequest req, CancellationToken ct)
    {
        if (!DateTime.TryParse(req.TradeDate, out var tradeDate))
            return BadRequest(new { error = "交易日期格式錯誤。" });

        var amount = req.Quantity * req.Price;
        decimal netAmount = req.Action.ToUpper() switch
        {
            "BUY" => -(amount + req.Fee + req.Tax),
            "SELL" => +(amount - req.Fee - req.Tax),
            "DIVIDEND" => +amount,
            _ => 0m
        };

        var entity = new UsTradeRecord
        {
            TradeDate = tradeDate,
            SettlementDate = DateTime.TryParse(req.SettlementDate, out var sd) ? sd : null,
            StockSymbol = req.StockSymbol.ToUpper().Trim(),
            StockName = req.StockName.Trim(),
            Market = req.Market ?? "美國",
            Action = req.Action.ToUpper(),
            Currency = req.Currency ?? "USD",
            Quantity = req.Quantity,
            Price = req.Price,
            Amount = amount,
            Fee = req.Fee,
            Tax = req.Tax,
            NetAmount = netAmount,
            SettlementCurrency = req.SettlementCurrency,
            ExchangeRate = req.ExchangeRate,
            NetAmountTwd = req.NetAmountTwd,
            TradeRef = req.TradeRef,
            Note = req.Note
        };

        _db.UsTradeRecords.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new { entity.Id, entity.NetAmount });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? symbol,
        [FromQuery] string? from,
        [FromQuery] string? to,
        CancellationToken ct)
    {
        IQueryable<UsTradeRecord> query = _db.UsTradeRecords;

        if (!string.IsNullOrWhiteSpace(symbol))
            query = query.Where(t => t.StockSymbol == symbol.ToUpper().Trim());
        if (DateTime.TryParse(from, out var f))
            query = query.Where(t => t.TradeDate >= f.Date);
        if (DateTime.TryParse(to, out var t2))
            query = query.Where(t => t.TradeDate <= t2.Date);

        var items = await query.OrderByDescending(t => t.TradeDate).ThenBy(t => t.StockSymbol).ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 10, CancellationToken ct = default)
    {
        var items = await _db.UsTradeRecords
            .OrderByDescending(t => t.TradeDate)
            .ThenByDescending(t => t.Id)
            .Take(count)
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateUsTradeRequest req, CancellationToken ct)
    {
        var entity = await _db.UsTradeRecords.FindAsync(new object[] { id }, ct);
        if (entity == null) return NotFound();

        if (req.TradeDate != null && DateTime.TryParse(req.TradeDate, out var td)) entity.TradeDate = td;
        if (req.StockName != null) entity.StockName = req.StockName;
        if (req.Action != null) entity.Action = req.Action.ToUpper();
        if (req.Quantity.HasValue) entity.Quantity = req.Quantity.Value;
        if (req.Price.HasValue) entity.Price = req.Price.Value;
        if (req.Fee.HasValue) entity.Fee = req.Fee.Value;
        if (req.Tax.HasValue) entity.Tax = req.Tax.Value;
        if (req.SettlementDate != null) entity.SettlementDate = DateTime.TryParse(req.SettlementDate, out var sd) ? sd : null;
        if (req.SettlementCurrency != null) entity.SettlementCurrency = req.SettlementCurrency;
        if (req.ExchangeRate.HasValue) entity.ExchangeRate = req.ExchangeRate.Value;
        if (req.NetAmountTwd.HasValue) entity.NetAmountTwd = req.NetAmountTwd.Value;
        if (req.Note != null) entity.Note = req.Note;

        // Recalculate
        entity.Amount = entity.Quantity * entity.Price;
        entity.NetAmount = entity.Action switch
        {
            "BUY" => -(entity.Amount + entity.Fee + entity.Tax),
            "SELL" => +(entity.Amount - entity.Fee - entity.Tax),
            "DIVIDEND" => +entity.Amount,
            _ => 0m
        };

        await _db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var entity = await _db.UsTradeRecords.FindAsync(new object[] { id }, ct);
        if (entity == null) return NotFound();
        _db.UsTradeRecords.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(new { message = "已刪除" });
    }

    /// <summary>Upload and parse Cathay US stock daily statement PDF</summary>
    [HttpPost("cathay-statement/parse")]
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
        var parser = new UsCathayStatementParserService();
        var result = await Task.Run(() => parser.Parse(stream, pwd), ct);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(result.Items);
    }

    public class UsBatchTradeItem
    {
        public string TradeDate { get; set; } = "";
        public string StockSymbol { get; set; } = "";
        public string StockName { get; set; } = "";
        public string Action { get; set; } = "";
        public string? Market { get; set; }
        public string? Currency { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public decimal Tax { get; set; }
        public string? SettlementDate { get; set; }
        public string? SettlementCurrency { get; set; }
        public decimal? ExchangeRate { get; set; }
        public decimal? NetAmountTwd { get; set; }
        public string? TradeRef { get; set; }
        public string? Note { get; set; }
    }

    public class UsBatchCreateRequest
    {
        public List<UsBatchTradeItem> Items { get; set; } = new();
    }

    [HttpPost("batch")]
    public async Task<IActionResult> BatchCreate([FromBody] UsBatchCreateRequest req, CancellationToken ct)
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
            if (string.IsNullOrWhiteSpace(item.StockSymbol))
            {
                errors.Add(new { index = idx, error = "股票代號不可為空" });
                continue;
            }

            var amount = item.Amount > 0 ? item.Amount : item.Quantity * item.Price;
            decimal netAmount = item.Action.ToUpper() switch
            {
                "BUY" => -(amount + item.Fee + item.Tax),
                "SELL" => +(amount - item.Fee - item.Tax),
                "DIVIDEND" => +amount,
                _ => 0m
            };

            var entity = new UsTradeRecord
            {
                TradeDate = td,
                SettlementDate = DateTime.TryParse(item.SettlementDate, out var sd) ? sd : null,
                StockSymbol = item.StockSymbol.ToUpper().Trim(),
                StockName = item.StockName,
                Market = item.Market ?? "美國",
                Action = item.Action.ToUpper(),
                Currency = item.Currency ?? "USD",
                Quantity = item.Quantity,
                Price = item.Price,
                Amount = amount,
                Fee = item.Fee,
                Tax = item.Tax,
                NetAmount = netAmount,
                SettlementCurrency = item.SettlementCurrency,
                ExchangeRate = item.ExchangeRate,
                NetAmountTwd = item.NetAmountTwd,
                TradeRef = item.TradeRef,
                Note = item.Note
            };

            _db.UsTradeRecords.Add(entity);
            successCount++;
        }

        if (successCount > 0)
            await _db.SaveChangesAsync(ct);

        return Ok(new { successCount, errorCount = errors.Count, errors });
    }
}
