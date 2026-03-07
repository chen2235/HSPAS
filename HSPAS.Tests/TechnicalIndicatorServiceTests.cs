using HSPAS.Api.Services;

namespace HSPAS.Tests;

public class TechnicalIndicatorServiceTests
{
    private readonly TechnicalIndicatorService _svc = new();

    [Fact]
    public void CalculateMovingAverage_ShouldReturnNullForInsufficientData()
    {
        var closes = new List<decimal?> { 10m, 20m, 30m };
        var result = _svc.CalculateMovingAverage(closes, 5);

        Assert.Equal(3, result.Count);
        Assert.True(result.All(v => v == null));
    }

    [Fact]
    public void CalculateMovingAverage_ShouldReturnCorrectValues()
    {
        var closes = new List<decimal?> { 10m, 20m, 30m, 40m, 50m };
        var result = _svc.CalculateMovingAverage(closes, 3);

        Assert.Equal(5, result.Count);
        Assert.Null(result[0]);
        Assert.Null(result[1]);
        Assert.Equal(20m, result[2]); // (10+20+30)/3
        Assert.Equal(30m, result[3]); // (20+30+40)/3
        Assert.Equal(40m, result[4]); // (30+40+50)/3
    }

    [Fact]
    public void CalculateMovingAverage_ShouldHandleNullValues()
    {
        var closes = new List<decimal?> { 10m, null, 30m, 40m, 50m };
        var result = _svc.CalculateMovingAverage(closes, 3);

        Assert.Equal(5, result.Count);
    }

    [Fact]
    public void CalculateRsi_ShouldReturnCorrectLength()
    {
        var closes = Enumerable.Range(1, 20).Select(i => (decimal?)i * 10m).ToList();
        var result = _svc.CalculateRsi(closes, 14);

        Assert.Equal(20, result.Count);
    }

    [Fact]
    public void CalculateRsi_ShouldReturnNullForInsufficientData()
    {
        var closes = new List<decimal?> { 10m, 20m, 30m };
        var result = _svc.CalculateRsi(closes, 14);

        Assert.Equal(3, result.Count);
        Assert.True(result.All(v => v == null));
    }

    [Fact]
    public void CalculateRsi_AllGains_ShouldReturn100()
    {
        // All prices going up
        var closes = Enumerable.Range(1, 20).Select(i => (decimal?)(100m + i)).ToList();
        var result = _svc.CalculateRsi(closes, 14);

        var lastRsi = result.Last();
        Assert.NotNull(lastRsi);
        Assert.Equal(100m, lastRsi);
    }

    [Fact]
    public void CalculateRsi_AllLosses_ShouldReturn0()
    {
        var closes = Enumerable.Range(1, 20).Select(i => (decimal?)(200m - i)).ToList();
        var result = _svc.CalculateRsi(closes, 14);

        var lastRsi = result.Last();
        Assert.NotNull(lastRsi);
        Assert.Equal(0m, lastRsi);
    }
}
