namespace HSPAS.Api.Services.Interfaces;

/// <summary>技術指標計算服務</summary>
public interface ITechnicalIndicatorService
{
    List<decimal?> CalculateMovingAverage(List<decimal?> closePrices, int window);
    List<decimal?> CalculateRsi(List<decimal?> closePrices, int period = 14);
}
