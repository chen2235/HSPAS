namespace HSPAS.Api.Services.Interfaces;

/// <summary>歷史回補服務介面</summary>
public interface IBackfillService
{
    Task<BackfillResult> ExecuteAsync(DateTime from, DateTime to, bool dryRun, CancellationToken ct = default);
}

public class BackfillResult
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public bool DryRun { get; set; }
    public List<BackfillDateResult> Results { get; set; } = new();
}

public class BackfillDateResult
{
    public string Date { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
