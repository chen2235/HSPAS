using HSPAS.Api.Services.Interfaces;

namespace HSPAS.Api.Services;

/// <summary>MA 與 RSI 技術指標計算</summary>
public class TechnicalIndicatorService : ITechnicalIndicatorService
{
    public List<decimal?> CalculateMovingAverage(List<decimal?> closePrices, int window)
    {
        var result = new List<decimal?>();
        for (int i = 0; i < closePrices.Count; i++)
        {
            if (i < window - 1)
            {
                result.Add(null);
                continue;
            }
            decimal sum = 0;
            int count = 0;
            for (int j = i - window + 1; j <= i; j++)
            {
                if (closePrices[j].HasValue) { sum += closePrices[j]!.Value; count++; }
            }
            result.Add(count == window ? Math.Round(sum / count, 4) : null);
        }
        return result;
    }

    public List<decimal?> CalculateRsi(List<decimal?> closePrices, int period = 14)
    {
        var result = new List<decimal?>();
        if (closePrices.Count < 2) return closePrices.Select(_ => (decimal?)null).ToList();

        var gains = new List<decimal>();
        var losses = new List<decimal>();

        for (int i = 1; i < closePrices.Count; i++)
        {
            if (closePrices[i].HasValue && closePrices[i - 1].HasValue)
            {
                var diff = closePrices[i]!.Value - closePrices[i - 1]!.Value;
                gains.Add(diff > 0 ? diff : 0);
                losses.Add(diff < 0 ? -diff : 0);
            }
            else
            {
                gains.Add(0);
                losses.Add(0);
            }
        }

        result.Add(null); // 第一筆沒有 RSI
        decimal avgGain = 0, avgLoss = 0;

        for (int i = 0; i < gains.Count; i++)
        {
            if (i < period - 1)
            {
                result.Add(null);
                continue;
            }
            if (i == period - 1)
            {
                avgGain = gains.Take(period).Sum() / period;
                avgLoss = losses.Take(period).Sum() / period;
            }
            else
            {
                avgGain = (avgGain * (period - 1) + gains[i]) / period;
                avgLoss = (avgLoss * (period - 1) + losses[i]) / period;
            }

            if (avgLoss == 0)
                result.Add(100m);
            else
                result.Add(Math.Round(100m - (100m / (1m + avgGain / avgLoss)), 2));
        }
        return result;
    }
}
