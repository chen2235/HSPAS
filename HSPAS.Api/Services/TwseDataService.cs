using System.Globalization;
using System.Text;
using HSPAS.Api.Entities;
using HSPAS.Api.Services.Interfaces;

namespace HSPAS.Api.Services;

/// <summary>
/// 從 TWSE 抓取盤後資料的實作。
/// 當日資料使用 STOCK_DAY_ALL（CSV），歷史資料使用 STOCK_DAY（JSON by 個股月份）。
/// </summary>
public class TwseDataService : ITwseDataService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TwseDataService> _logger;

    // 全市場當日盤後 CSV
    private const string AllStockDayUrl = "https://www.twse.com.tw/exchangeReport/STOCK_DAY_ALL?response=open_data";

    // 歷史每月個股行情 JSON（date=yyyyMMdd 為該月任一日）
    private const string StockDayUrlTemplate = "https://www.twse.com.tw/exchangeReport/STOCK_DAY?response=json&date={0}&stockNo={1}";

    // 歷史全市場日成交（依日期查）
    private const string MiIndexUrlTemplate = "https://www.twse.com.tw/exchangeReport/MI_INDEX?response=json&date={0}&type=ALLBUT0999";

    public TwseDataService(IHttpClientFactory httpClientFactory, ILogger<TwseDataService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<DailyStockPrice>> FetchDailyPricesAsync(DateTime date, CancellationToken ct = default)
    {
        // 若為今天，使用 STOCK_DAY_ALL CSV（最快、最穩定）
        if (date.Date == DateTime.Today)
        {
            return await FetchTodayAllAsync(date, ct);
        }

        // 歷史日期使用 MI_INDEX
        return await FetchHistoryByMiIndexAsync(date, ct);
    }

    /// <summary>抓取當日全市場 CSV</summary>
    private async Task<List<DailyStockPrice>> FetchTodayAllAsync(DateTime date, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("TWSE");
        var csv = await client.GetStringAsync(AllStockDayUrl, ct);
        return ParseAllStockDayCsv(csv, date);
    }

    /// <summary>抓取歷史資料（MI_INDEX JSON）</summary>
    private async Task<List<DailyStockPrice>> FetchHistoryByMiIndexAsync(DateTime date, CancellationToken ct)
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

        // tables[8] 通常是每日收盤行情（欄位 9 或 fields 含有「證券代號」）
        // 不同日期 table index 可能不同，需找含有 "證券代號" 的那張
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
                CreateTime = DateTime.UtcNow
            };

            if (!string.IsNullOrEmpty(entity.StockId))
                results.Add(entity);
        }

        _logger.LogInformation("MI_INDEX parsed {Count} records for {Date}", results.Count, date);
        return results;
    }

    /// <summary>解析 STOCK_DAY_ALL CSV</summary>
    internal static List<DailyStockPrice> ParseAllStockDayCsv(string csv, DateTime date)
    {
        var results = new List<DailyStockPrice>();
        var lines = csv.Split('\n');

        // 跳過 header（第一行）
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            // CSV 欄位：證券代號,證券名稱,成交股數,成交金額,開盤價,最高價,最低價,收盤價,漲跌價差,成交筆數
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
                CreateTime = DateTime.UtcNow
            };

            if (!string.IsNullOrEmpty(entity.StockId))
                results.Add(entity);
        }

        return results;
    }

    /// <summary>處理 CSV 分割（考慮引號內的逗號）</summary>
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

    /// <summary>解析含千分位的整數</summary>
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

    /// <summary>解析含千分位的小數</summary>
    private static decimal? ParseDecimal(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim().Trim('"').Replace(",", "").Replace("X", "");
        if (s == "--" || s == "") return null;
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    /// <summary>MI_INDEX 的漲跌含正負號欄位</summary>
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
