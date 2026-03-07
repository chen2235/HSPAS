using HSPAS.Api.Entities;

namespace HSPAS.Api.Services.Interfaces;

/// <summary>每日行情相關業務邏輯</summary>
public interface IDailyPriceService
{
    Task<List<string>> GetAvailableDatesAsync(CancellationToken ct = default);
    Task<List<DailyStockPrice>> GetByDateAsync(DateTime date, CancellationToken ct = default);
    Task<List<DailyStockPrice>> GetStockHistoryAsync(string stockId, DateTime from, DateTime to, CancellationToken ct = default);
}
