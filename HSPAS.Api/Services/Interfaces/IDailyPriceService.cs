using HSPAS.Api.Entities;

namespace HSPAS.Api.Services.Interfaces;

/// <summary>每日行情相關業務邏輯</summary>
public interface IDailyPriceService
{
    Task<List<string>> GetAvailableDatesAsync(CancellationToken ct = default);
    Task<List<DailyStockPrice>> GetByDateAsync(DateTime date, CancellationToken ct = default);
    Task<List<DailyStockPrice>> GetStockHistoryAsync(string stockId, DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>取得指定日期的上市（TSE）行情</summary>
    Task<List<DailyStockPrice>> FetchTseDailyAsync(DateTime date, CancellationToken ct = default);

    /// <summary>取得指定日期的上櫃（OTC）行情</summary>
    Task<List<DailyStockPrice>> FetchOtcDailyAsync(DateTime date, CancellationToken ct = default);

    /// <summary>單日回補：抓取指定日期的上市+上櫃行情並寫入 DB（先刪後插）</summary>
    Task<BackfillOneDayResult> BackfillOneDayAsync(DateTime date, CancellationToken ct = default);

    /// <summary>區間回補：逐日執行 BackfillOneDayAsync，回傳每日結果</summary>
    Task<BackfillRangeResult> BackfillRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
}

/// <summary>單日回補結果</summary>
public class BackfillOneDayResult
{
    public string Date { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // SUCCESS, NO_DATA, PARTIAL, FAILED
    public int TseCount { get; set; }
    public int OtcCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>區間回補結果</summary>
public class BackfillRangeResult
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public int TotalDays { get; set; }
    public int SuccessDays { get; set; }
    public int NoDataDays { get; set; }
    public int FailedDays { get; set; }
    public int TotalTseCount { get; set; }
    public int TotalOtcCount { get; set; }
    public List<BackfillOneDayResult> Results { get; set; } = new();
}
