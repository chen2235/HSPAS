using HSPAS.Api.Entities;

namespace HSPAS.Api.Services.Interfaces;

/// <summary>TWSE 盤後資料抓取服務介面（方便未來抽換資料來源）</summary>
public interface ITwseDataService
{
    /// <summary>取得指定日期的全市場盤後資料</summary>
    Task<List<DailyStockPrice>> FetchDailyPricesAsync(DateTime date, CancellationToken ct = default);
}
