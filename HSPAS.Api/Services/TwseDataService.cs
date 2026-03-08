using System.Globalization;
using System.Text;
using HSPAS.Api.Entities;
using HSPAS.Api.Services.Interfaces;

namespace HSPAS.Api.Services;

/// <summary>
/// 從 TWSE（上市）與 TPEx（上櫃）抓取盤後資料。
/// 上市：STOCK_DAY_ALL（當日 CSV）、MI_INDEX（歷史 JSON）。
/// 上櫃：TPEx DAILY_CLOSE_quotes（CSV，支援日期參數）。
/// </summary>
public class TwseDataService : ITwseDataService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TwseDataService> _logger;

    // === 上市（TSE）===
    // 全市場當日盤後 CSV
    private const string AllStockDayUrl = "https://www.twse.com.tw/exchangeReport/STOCK_DAY_ALL?response=open_data";
    // 歷史全市場日成交（依日期查）
    private const string MiIndexUrlTemplate = "https://www.twse.com.tw/exchangeReport/MI_INDEX?response=json&date={0}&type=ALLBUT0999";

    // === 上櫃（OTC）===
    // 當日上櫃行情 CSV（不帶 d 參數 = 當日）
    private const string TpexTodayUrl = "https://www.tpex.org.tw/web/stock/aftertrading/DAILY_CLOSE_quotes/stk_quote_result.php?l=zh-tw&o=data";
    // 歷史上櫃行情 CSV（d=民國年/月/日）
    private const string TpexHistoryUrlTemplate = "https://www.tpex.org.tw/web/stock/aftertrading/DAILY_CLOSE_quotes/stk_quote_result.php?l=zh-tw&d={0}&o=data";

    public TwseDataService(IHttpClientFactory httpClientFactory, ILogger<TwseDataService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // ==================== 上市（TSE）====================

    public async Task<List<DailyStockPrice>> FetchDailyPricesAsync(DateTime date, CancellationToken ct = default)
    {
        List<DailyStockPrice> results;

        if (date.Date == DateTime.Today)
        {
            results = await FetchTseTodayAsync(date, ct);
        }
        else
        {
            results = await FetchTseHistoryAsync(date, ct);
        }

        // 標記市場別
        foreach (var r in results) r.MarketType = "TSE";
        return results;
    }

    private async Task<List<DailyStockPrice>> FetchTseTodayAsync(DateTime date, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("TWSE");
        var csv = await client.GetStringAsync(AllStockDayUrl, ct);
        return ParseTseCsv(csv, date);
    }

    private async Task<List<DailyStockPrice>> FetchTseHistoryAsync(DateTime date, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("TWSE");
        var url = string.Format(MiIndexUrlTemplate, date.ToString("yyyyMMdd"));

        var response = await client.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("MI_INDEX request failed for {Date}: {Status}", date, response.StatusCode);
            return new List<DailyStockPrice>();
        }

        var json = await response.Content.ReadFromJsonAsync<MiIndexResponse>(cancellationToken: ct);
        if (json?.Stat != "OK" || json.Tables == null)
        {
            _logger.LogWarning("MI_INDEX returned non-OK for {Date}: {Stat}", date, json?.Stat);
            return new List<DailyStockPrice>();
        }

        var table = json.Tables.FirstOrDefault(t =>
            t.Fields != null && t.Fields.Any(f => f.Contains("證券代號")));

        if (table?.Data == null)
        {
            _logger.LogWarning("MI_INDEX: no stock table found for {Date}", date);
            return new List<DailyStockPrice>();
        }

        var results = new List<DailyStockPrice>();
        foreach (var row in table.Data)
        {
            if (row.Count < 10) continue;

            var entity = new DailyStockPrice
            {
                TradeDate = date.Date,
                StockId = row[0]?.Trim() ?? "",
                StockName = row[1]?.Trim() ?? "",
                TradeVolume = ParseLong(row[2]),
                TradeValue = ParseDecimal(row[4]),
                OpenPrice = ParseDecimal(row[5]),
                HighPrice = ParseDecimal(row[6]),
                LowPrice = ParseDecimal(row[7]),
                ClosePrice = ParseDecimal(row[8]),
                PriceChange = ParseSignedChange(row[9], row[10]),
                Transaction = ParseInt(row[3]),
                MarketType = "TSE",
                CreateTime = DateTime.UtcNow
            };

            if (!string.IsNullOrEmpty(entity.StockId))
                results.Add(entity);
        }

        _logger.LogInformation("MI_INDEX parsed {Count} TSE records for {Date}", results.Count, date);
        return results;
    }

    // ==================== 上櫃（OTC）====================

    public async Task<List<DailyStockPrice>> FetchOtcDailyPricesAsync(DateTime date, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("TWSE");
            string csv;

            if (date.Date == DateTime.Today)
            {
                csv = await client.GetStringAsync(TpexTodayUrl, ct);
            }
            else
            {
                // 轉換為民國年日期格式：115/03/06
                var rocYear = date.Year - 1911;
                var rocDate = $"{rocYear}/{date:MM/dd}";
                var url = string.Format(TpexHistoryUrlTemplate, rocDate);
                csv = await client.GetStringAsync(url, ct);
            }

            var results = ParseTpexCsv(csv, date);
            if (results.Count == 0 && csv.Split('\n').Length > 2)
                _logger.LogWarning("TPEx returned data for different date (not {Date}), skipped.", date);
            else
                _logger.LogInformation("TPEx parsed {Count} OTC records for {Date}", results.Count, date);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TPEx fetch failed for {Date}", date);
            return new List<DailyStockPrice>();
        }
    }

    // ==================== CSV 解析 ====================

    /// <summary>解析 TWSE STOCK_DAY_ALL CSV（上市）</summary>
    internal static List<DailyStockPrice> ParseTseCsv(string csv, DateTime date)
    {
        var results = new List<DailyStockPrice>();
        var lines = csv.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var parts = SplitCsvLine(line);
            if (parts.Length < 10) continue;

            var entity = new DailyStockPrice
            {
                TradeDate = date.Date,
                StockId = parts[0].Trim().Trim('"'),
                StockName = parts[1].Trim().Trim('"'),
                TradeVolume = ParseLong(parts[2]),
                TradeValue = ParseDecimal(parts[3]),
                OpenPrice = ParseDecimal(parts[4]),
                HighPrice = ParseDecimal(parts[5]),
                LowPrice = ParseDecimal(parts[6]),
                ClosePrice = ParseDecimal(parts[7]),
                PriceChange = ParseDecimal(parts[8]),
                Transaction = ParseInt(parts[9]),
                MarketType = "TSE",
                CreateTime = DateTime.UtcNow
            };

            if (!string.IsNullOrEmpty(entity.StockId))
                results.Add(entity);
        }

        return results;
    }

    /// <summary>
    /// 解析 TPEx 上櫃行情 CSV。
    /// 欄位順序：資料日期,代號,名稱,收盤,漲跌,開盤,最高,最低,均價,成交股數,成交金額,成交筆數,...
    /// 日期格式為民國年 YYYMMDD（如 1150306）。
    /// </summary>
    internal static List<DailyStockPrice> ParseTpexCsv(string csv, DateTime date)
    {
        var results = new List<DailyStockPrice>();
        var lines = csv.Split('\n');

        // 跳過 header
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var parts = SplitCsvLine(line);
            if (parts.Length < 12) continue;

            var stockId = parts[1].Trim().Trim('"');
            var stockName = parts[2].Trim().Trim('"');

            // 跳過非股票代號（如小計列、空白列）
            if (string.IsNullOrEmpty(stockId) || stockId.Length < 3) continue;

            // 解析民國日期以確認交易日
            var tradeDate = ParseRocDate(parts[0].Trim().Trim('"'));
            if (tradeDate == null) tradeDate = date.Date;
            // 如果 CSV 回傳的日期與查詢日期不同，略過（TPEx 可能回傳最近交易日）
            if (tradeDate.Value.Date != date.Date) continue;

            var closePrice = ParseDecimal(parts[3]);
            var priceChange = ParseDecimal(parts[4]);
            var openPrice = ParseDecimal(parts[5]);
            var highPrice = ParseDecimal(parts[6]);
            var lowPrice = ParseDecimal(parts[7]);
            var tradeVolume = ParseLong(parts[9]);
            var tradeValue = ParseDecimal(parts[10]);
            var transaction = ParseInt(parts[11]);

            // 跳過無收盤價的資料（停牌等）
            if (closePrice == null && openPrice == null) continue;

            var entity = new DailyStockPrice
            {
                TradeDate = tradeDate.Value,
                StockId = stockId,
                StockName = stockName,
                TradeVolume = tradeVolume,
                TradeValue = tradeValue,
                OpenPrice = openPrice,
                HighPrice = highPrice,
                LowPrice = lowPrice,
                ClosePrice = closePrice,
                PriceChange = priceChange,
                Transaction = transaction,
                MarketType = "OTC",
                CreateTime = DateTime.UtcNow
            };

            results.Add(entity);
        }

        return results;
    }

    /// <summary>解析民國日期 YYYMMDD（如 1150306 → 2026/03/06）</summary>
    private static DateTime? ParseRocDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Replace("/", "").Replace("-", "");
        // 格式可能是 1150306（7 位）或 115/03/06
        if (s.Length >= 7)
        {
            if (int.TryParse(s.Substring(0, s.Length - 4), out var rocYear) &&
                int.TryParse(s.Substring(s.Length - 4, 2), out var month) &&
                int.TryParse(s.Substring(s.Length - 2, 2), out var day))
            {
                try { return new DateTime(rocYear + 1911, month, day); }
                catch { return null; }
            }
        }
        return null;
    }

    // ==================== 共用工具 ====================

    private static string[] SplitCsvLine(string line)
    {
        var parts = new List<string>();
        bool inQuotes = false;
        var current = new StringBuilder();

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                parts.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        parts.Add(current.ToString());
        return parts.ToArray();
    }

    private static long? ParseLong(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim().Trim('"').Replace(",", "");
        return long.TryParse(s, out var v) ? v : null;
    }

    private static int? ParseInt(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim().Trim('"').Replace(",", "");
        return int.TryParse(s, out var v) ? v : null;
    }

    private static decimal? ParseDecimal(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim().Trim('"').Replace(",", "").Replace("X", "");
        if (s == "--" || s == "" || s == "---" || s == "除息" || s == "除權") return null;
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    private static decimal? ParseSignedChange(string? sign, string? value)
    {
        var d = ParseDecimal(value);
        if (d == null) return null;
        if (sign != null && sign.Trim() == "-") d = -d;
        return d;
    }

    // MI_INDEX JSON 回應結構
    private class MiIndexResponse
    {
        public string? Stat { get; set; }
        public List<MiTable>? Tables { get; set; }
    }

    private class MiTable
    {
        public string? Title { get; set; }
        public List<string>? Fields { get; set; }
        public List<List<string>>? Data { get; set; }
    }
}
