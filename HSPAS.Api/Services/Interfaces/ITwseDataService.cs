using HSPAS.Api.Entities;

namespace HSPAS.Api.Services.Interfaces;

/// <summary>盤後資料抓取服務介面（上市 TSE + 上櫃 OTC）</summary>
public interface ITwseDataService
{
    /// <summary>取得指定日期的上市（TSE）全市場盤後資料</summary>
    Task<List<DailyStockPrice>> FetchDailyPricesAsync(DateTime date, CancellationToken ct = default);

    /// <summary>取得指定日期的上櫃（OTC）全市場盤後資料</summary>
    Task<List<DailyStockPrice>> FetchOtcDailyPricesAsync(DateTime date, CancellationToken ct = default);
}
