namespace HSPAS.Api.Services.Interfaces;

public interface IWaterBillService
{
    Task<WaterBillDto> SaveAsync(WaterBillSaveRequest req, CancellationToken ct = default);
    Task<List<WaterBillListItem>> GetListAsync(int? year, CancellationToken ct = default);
    Task<WaterBillDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<WaterBillDto> UpdateAsync(long id, WaterBillUpdateRequest req, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task<List<WaterDashboardPeriod>> GetDashboardAsync(int year, CancellationToken ct = default);
}

// DTO: Save request (from PDF parse or manual)
public class WaterBillSaveRequest
{
    public string WaterAddress { get; set; } = string.Empty;
    public string WaterNo { get; set; } = string.Empty;
    public string MeterNo { get; set; } = string.Empty;
    public DateTime BillingStartDate { get; set; }
    public DateTime BillingEndDate { get; set; }
    public int? BillingDays { get; set; }
    public string? BillingPeriodText { get; set; }
    public int? TotalUsage { get; set; }
    public int CurrentUsage { get; set; }
    public int CurrentMeterReading { get; set; }
    public int PreviousMeterReading { get; set; }
    public decimal TotalAmount { get; set; }
    public string? TariffType { get; set; }
    public string? RawDetailJson { get; set; }
    public string? Remark { get; set; }
}

// DTO: Update request
public class WaterBillUpdateRequest
{
    public DateTime? BillingStartDate { get; set; }
    public DateTime? BillingEndDate { get; set; }
    public int? BillingDays { get; set; }
    public int? TotalUsage { get; set; }
    public int? CurrentUsage { get; set; }
    public int? CurrentMeterReading { get; set; }
    public int? PreviousMeterReading { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? Remark { get; set; }
}

// DTO: Full record
public class WaterBillDto
{
    public long Id { get; set; }
    public string WaterAddress { get; set; } = string.Empty;
    public string WaterNo { get; set; } = string.Empty;
    public string MeterNo { get; set; } = string.Empty;
    public string BillingStartDate { get; set; } = string.Empty;
    public string BillingEndDate { get; set; } = string.Empty;
    public int? BillingDays { get; set; }
    public string? BillingPeriodText { get; set; }
    public int? TotalUsage { get; set; }
    public int CurrentUsage { get; set; }
    public int CurrentMeterReading { get; set; }
    public int PreviousMeterReading { get; set; }
    public decimal TotalAmount { get; set; }
    public string? TariffType { get; set; }
    public string? RawDetailJson { get; set; }
    public string? Remark { get; set; }
    public string CreateTime { get; set; } = string.Empty;
}

// DTO: List item
public class WaterBillListItem
{
    public long Id { get; set; }
    public string BillingEndDate { get; set; } = string.Empty;
    public string BillingPeriodText { get; set; } = string.Empty;
    public int? TotalUsage { get; set; }
    public int CurrentUsage { get; set; }
    public int CurrentMeterReading { get; set; }
    public int PreviousMeterReading { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Remark { get; set; }
}

// DTO: Dashboard period data
public class WaterDashboardPeriod
{
    public int PeriodIndex { get; set; }
    public string BillingStartDate { get; set; } = string.Empty;
    public string BillingEndDate { get; set; } = string.Empty;
    public string PeriodLabel { get; set; } = string.Empty;
    public int UsageTotal { get; set; }
    public decimal AmountTotal { get; set; }
    public string? Remark { get; set; }
}
