using HSPAS.Api.Entities;
using HSPAS.Api.Infrastructure;
using HSPAS.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HSPAS.Api.Services;

public class ElectricityBillService : IElectricityBillService
{
    private readonly HspasDbContext _db;
    private readonly ILogger<ElectricityBillService> _logger;

    public ElectricityBillService(HspasDbContext db, ILogger<ElectricityBillService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ElecBillDto> SaveAsync(ElecBillSaveRequest req, CancellationToken ct = default)
    {
        // Check for duplicate by PowerNo + BillingEndDate
        var existing = await _db.LifeElectricityBillPeriods
            .FirstOrDefaultAsync(e => e.PowerNo == req.PowerNo && e.BillingEndDate == req.BillingEndDate, ct);

        if (existing != null)
        {
            // Update existing record
            existing.Address = req.Address;
            existing.BlackoutGroup = req.BlackoutGroup;
            existing.BillingStartDate = req.BillingStartDate;
            existing.BillingDays = req.BillingDays;
            existing.BillingPeriodText = req.BillingPeriodText;
            existing.ReadOrDebitDate = req.ReadOrDebitDate;
            existing.Kwh = req.Kwh;
            existing.KwhPerDay = req.KwhPerDay;
            existing.AvgPricePerKwh = req.AvgPricePerKwh;
            existing.TotalAmount = req.TotalAmount;
            existing.InvoiceAmount = req.InvoiceAmount;
            existing.TariffType = req.TariffType;
            existing.SharedMeterHouseholdCount = req.SharedMeterHouseholdCount;
            existing.InvoicePeriod = req.InvoicePeriod;
            existing.InvoiceNo = req.InvoiceNo;
            existing.RawDetailJson = req.RawDetailJson;
            if (req.Remark != null) existing.Remark = req.Remark;
            existing.UpdateTime = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return MapToDto(existing);
        }

        var entity = new LifeElectricityBillPeriod
        {
            Address = req.Address,
            PowerNo = req.PowerNo,
            BlackoutGroup = req.BlackoutGroup,
            BillingStartDate = req.BillingStartDate,
            BillingEndDate = req.BillingEndDate,
            BillingDays = req.BillingDays,
            BillingPeriodText = req.BillingPeriodText,
            ReadOrDebitDate = req.ReadOrDebitDate,
            Kwh = req.Kwh,
            KwhPerDay = req.KwhPerDay,
            AvgPricePerKwh = req.AvgPricePerKwh,
            TotalAmount = req.TotalAmount,
            InvoiceAmount = req.InvoiceAmount,
            TariffType = req.TariffType,
            SharedMeterHouseholdCount = req.SharedMeterHouseholdCount,
            InvoicePeriod = req.InvoicePeriod,
            InvoiceNo = req.InvoiceNo,
            RawDetailJson = req.RawDetailJson,
            Remark = req.Remark,
        };
        _db.LifeElectricityBillPeriods.Add(entity);
        await _db.SaveChangesAsync(ct);
        return MapToDto(entity);
    }

    public async Task<List<ElecBillListItem>> GetListAsync(int? year, int? month, CancellationToken ct = default)
    {
        var q = _db.LifeElectricityBillPeriods.AsQueryable();
        if (year.HasValue)
            q = q.Where(e => e.BillingEndDate.Year == year.Value);
        if (month.HasValue)
            q = q.Where(e => e.BillingEndDate.Month == month.Value);

        var list = await q.OrderByDescending(e => e.BillingEndDate).ToListAsync(ct);
        return list.Select(e => new ElecBillListItem
        {
            Id = e.Id,
            BillingEndDate = e.BillingEndDate.ToString("yyyy-MM-dd"),
            BillingPeriodText = e.BillingPeriodText ?? $"{e.BillingStartDate:yyyy/MM/dd} ~ {e.BillingEndDate:yyyy/MM/dd}",
            ReadOrDebitDate = e.ReadOrDebitDate.ToString("yyyy/MM/dd"),
            Kwh = e.Kwh,
            KwhPerDay = e.KwhPerDay,
            AvgPricePerKwh = e.AvgPricePerKwh,
            TotalAmount = e.TotalAmount,
            InvoicePeriod = e.InvoicePeriod,
            InvoiceNo = e.InvoiceNo,
            Remark = e.Remark,
        }).ToList();
    }

    public async Task<ElecBillDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var e = await _db.LifeElectricityBillPeriods.FindAsync(new object[] { id }, ct);
        return e == null ? null : MapToDto(e);
    }

    public async Task<ElecBillDto> UpdateAsync(long id, ElecBillUpdateRequest req, CancellationToken ct = default)
    {
        var e = await _db.LifeElectricityBillPeriods.FindAsync(new object[] { id }, ct)
            ?? throw new KeyNotFoundException($"Id {id} not found");

        if (req.BillingStartDate.HasValue) e.BillingStartDate = req.BillingStartDate.Value;
        if (req.BillingEndDate.HasValue) e.BillingEndDate = req.BillingEndDate.Value;
        if (req.BillingDays.HasValue) e.BillingDays = req.BillingDays.Value;
        if (req.ReadOrDebitDate.HasValue) e.ReadOrDebitDate = req.ReadOrDebitDate.Value;
        if (req.Kwh.HasValue) e.Kwh = req.Kwh.Value;
        if (req.KwhPerDay.HasValue) e.KwhPerDay = req.KwhPerDay.Value;
        if (req.AvgPricePerKwh.HasValue) e.AvgPricePerKwh = req.AvgPricePerKwh.Value;
        if (req.TotalAmount.HasValue) e.TotalAmount = req.TotalAmount.Value;
        if (req.InvoicePeriod != null) e.InvoicePeriod = req.InvoicePeriod;
        if (req.InvoiceNo != null) e.InvoiceNo = req.InvoiceNo;
        if (req.Remark != null) e.Remark = req.Remark;
        e.UpdateTime = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return MapToDto(e);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var e = await _db.LifeElectricityBillPeriods.FindAsync(new object[] { id }, ct)
            ?? throw new KeyNotFoundException($"Id {id} not found");
        _db.LifeElectricityBillPeriods.Remove(e);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<ElecDashboardMonth>> GetDashboardAsync(int year, CancellationToken ct = default)
    {
        // Pull records for the year, then group in memory to collect remarks
        var records = await _db.LifeElectricityBillPeriods
            .Where(e => e.BillingEndDate.Year == year)
            .Select(e => new { e.BillingEndDate.Month, e.Kwh, e.TotalAmount, e.Remark })
            .ToListAsync(ct);

        var data = records
            .GroupBy(e => e.Month)
            .Select(g => new ElecDashboardMonth
            {
                Month = g.Key,
                KwhTotal = g.Sum(e => e.Kwh),
                AmountTotal = g.Sum(e => e.TotalAmount),
                BillCount = g.Count(),
                Remarks = g.Where(e => !string.IsNullOrWhiteSpace(e.Remark))
                            .Select(e => e.Remark!)
                            .ToList(),
            })
            .OrderBy(m => m.Month)
            .ToList();
        return data;
    }

    private static ElecBillDto MapToDto(LifeElectricityBillPeriod e) => new()
    {
        Id = e.Id,
        Address = e.Address,
        PowerNo = e.PowerNo,
        BlackoutGroup = e.BlackoutGroup,
        BillingStartDate = e.BillingStartDate.ToString("yyyy-MM-dd"),
        BillingEndDate = e.BillingEndDate.ToString("yyyy-MM-dd"),
        BillingDays = e.BillingDays,
        BillingPeriodText = e.BillingPeriodText,
        ReadOrDebitDate = e.ReadOrDebitDate.ToString("yyyy-MM-dd"),
        Kwh = e.Kwh,
        KwhPerDay = e.KwhPerDay,
        AvgPricePerKwh = e.AvgPricePerKwh,
        TotalAmount = e.TotalAmount,
        InvoiceAmount = e.InvoiceAmount,
        TariffType = e.TariffType,
        SharedMeterHouseholdCount = e.SharedMeterHouseholdCount,
        InvoicePeriod = e.InvoicePeriod,
        InvoiceNo = e.InvoiceNo,
        RawDetailJson = e.RawDetailJson,
        Remark = e.Remark,
        CreateTime = e.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
    };
}
