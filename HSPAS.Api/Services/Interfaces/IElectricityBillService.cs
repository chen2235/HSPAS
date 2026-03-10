namespace HSPAS.Api.Services.Interfaces;

public interface IElectricityBillService
{
    Task<ElecBillDto> SaveAsync(ElecBillSaveRequest req, CancellationToken ct = default);
    Task<List<ElecBillListItem>> GetListAsync(int? year, int? month, CancellationToken ct = default);
    Task<ElecBillDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ElecBillDto> UpdateAsync(long id, ElecBillUpdateRequest req, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task<List<ElecDashboardMonth>> GetDashboardAsync(int year, CancellationToken ct = default);
}

// DTO: Save request (from PDF parse or manual)
public class ElecBillSaveRequest
{
    public string Address { get; set; } = string.Empty;
    public string PowerNo { get; set; } = string.Empty;
    public string? BlackoutGroup { get; set; }
    public DateTime BillingStartDate { get; set; }
    public DateTime BillingEndDate { get; set; }
    public int BillingDays { get; set; }
    public string? BillingPeriodText { get; set; }
    public DateTime ReadOrDebitDate { get; set; }
    public int Kwh { get; set; }
    public decimal? KwhPerDay { get; set; }
    public decimal? AvgPricePerKwh { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal? InvoiceAmount { get; set; }
    public string? TariffType { get; set; }
    public int? SharedMeterHouseholdCount { get; set; }
    public string? InvoicePeriod { get; set; }
    public string? InvoiceNo { get; set; }
    public string? RawDetailJson { get; set; }
    public string? Remark { get; set; }
}

// DTO: Update request
public class ElecBillUpdateRequest
{
    public DateTime? BillingStartDate { get; set; }
    public DateTime? BillingEndDate { get; set; }
    public int? BillingDays { get; set; }
    public DateTime? ReadOrDebitDate { get; set; }
    public int? Kwh { get; set; }
    public decimal? KwhPerDay { get; set; }
    public decimal? AvgPricePerKwh { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? InvoicePeriod { get; set; }
    public string? InvoiceNo { get; set; }
    public string? Remark { get; set; }
}

// DTO: Full record
public class ElecBillDto
{
    public long Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public string PowerNo { get; set; } = string.Empty;
    public string? BlackoutGroup { get; set; }
    public string BillingStartDate { get; set; } = string.Empty;
    public string BillingEndDate { get; set; } = string.Empty;
    public int BillingDays { get; set; }
    public string? BillingPeriodText { get; set; }
    public string ReadOrDebitDate { get; set; } = string.Empty;
    public int Kwh { get; set; }
    public decimal? KwhPerDay { get; set; }
    public decimal? AvgPricePerKwh { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal? InvoiceAmount { get; set; }
    public string? TariffType { get; set; }
    public int? SharedMeterHouseholdCount { get; set; }
    public string? InvoicePeriod { get; set; }
    public string? InvoiceNo { get; set; }
    public string? RawDetailJson { get; set; }
    public string? Remark { get; set; }
    public string CreateTime { get; set; } = string.Empty;
}

// DTO: List item
public class ElecBillListItem
{
    public long Id { get; set; }
    public string BillingEndDate { get; set; } = string.Empty;
    public string BillingPeriodText { get; set; } = string.Empty;
    public string ReadOrDebitDate { get; set; } = string.Empty;
    public int Kwh { get; set; }
    public decimal? KwhPerDay { get; set; }
    public decimal? AvgPricePerKwh { get; set; }
    public decimal TotalAmount { get; set; }
    public string? InvoicePeriod { get; set; }
    public string? InvoiceNo { get; set; }
    public string? Remark { get; set; }
}

// DTO: Dashboard monthly aggregation
public class ElecDashboardMonth
{
    public int Month { get; set; }
    public int KwhTotal { get; set; }
    public decimal AmountTotal { get; set; }
    public int BillCount { get; set; }
    public List<string> Remarks { get; set; } = new();
}
