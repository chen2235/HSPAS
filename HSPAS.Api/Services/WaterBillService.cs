using HSPAS.Api.Entities;
using HSPAS.Api.Infrastructure;
using HSPAS.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HSPAS.Api.Services;

public class WaterBillService : IWaterBillService
{
    private readonly HspasDbContext _db;
    private readonly ILogger<WaterBillService> _logger;

    public WaterBillService(HspasDbContext db, ILogger<WaterBillService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<WaterBillDto> SaveAsync(WaterBillSaveRequest req, CancellationToken ct = default)
    {
        // Check for duplicate by WaterNo + BillingEndDate
        var existing = await _db.LifeWaterBillPeriods
            .FirstOrDefaultAsync(e => e.WaterNo == req.WaterNo && e.BillingEndDate == req.BillingEndDate, ct);

        if (existing != null)
        {
            existing.WaterAddress = req.WaterAddress;
            existing.MeterNo = req.MeterNo;
            existing.BillingStartDate = req.BillingStartDate;
            existing.BillingDays = req.BillingDays;
            existing.BillingPeriodText = req.BillingPeriodText;
            existing.TotalUsage = req.TotalUsage;
            existing.CurrentUsage = req.CurrentUsage;
            existing.CurrentMeterReading = req.CurrentMeterReading;
            existing.PreviousMeterReading = req.PreviousMeterReading;
            existing.TotalAmount = req.TotalAmount;
            existing.TariffType = req.TariffType;
            existing.RawDetailJson = req.RawDetailJson;
            if (req.Remark != null) existing.Remark = req.Remark;
            existing.UpdateTime = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return MapToDto(existing);
        }

        var entity = new LifeWaterBillPeriod
        {
            WaterAddress = req.WaterAddress,
            WaterNo = req.WaterNo,
            MeterNo = req.MeterNo,
            BillingStartDate = req.BillingStartDate,
            BillingEndDate = req.BillingEndDate,
            BillingDays = req.BillingDays,
            BillingPeriodText = req.BillingPeriodText,
            TotalUsage = req.TotalUsage,
            CurrentUsage = req.CurrentUsage,
            CurrentMeterReading = req.CurrentMeterReading,
            PreviousMeterReading = req.PreviousMeterReading,
            TotalAmount = req.TotalAmount,
            TariffType = req.TariffType,
            RawDetailJson = req.RawDetailJson,
            Remark = req.Remark,
        };
        _db.LifeWaterBillPeriods.Add(entity);
        await _db.SaveChangesAsync(ct);
        return MapToDto(entity);
    }

    public async Task<List<WaterBillListItem>> GetListAsync(int? year, CancellationToken ct = default)
    {
        var q = _db.LifeWaterBillPeriods.AsQueryable();
        if (year.HasValue)
            q = q.Where(e => e.BillingEndDate.Year == year.Value);

        var list = await q.OrderByDescending(e => e.BillingEndDate).ToListAsync(ct);
        return list.Select(e => new WaterBillListItem
        {
            Id = e.Id,
            BillingEndDate = e.BillingEndDate.ToString("yyyy-MM-dd"),
            BillingPeriodText = e.BillingPeriodText ?? $"{e.BillingStartDate:yyyy/MM/dd} ~ {e.BillingEndDate:yyyy/MM/dd}",
            TotalUsage = e.TotalUsage,
            CurrentUsage = e.CurrentUsage,
            CurrentMeterReading = e.CurrentMeterReading,
            PreviousMeterReading = e.PreviousMeterReading,
            TotalAmount = e.TotalAmount,
            Remark = e.Remark,
        }).ToList();
    }

    public async Task<WaterBillDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var e = await _db.LifeWaterBillPeriods.FindAsync(new object[] { id }, ct);
        return e == null ? null : MapToDto(e);
    }

    public async Task<WaterBillDto> UpdateAsync(long id, WaterBillUpdateRequest req, CancellationToken ct = default)
    {
        var e = await _db.LifeWaterBillPeriods.FindAsync(new object[] { id }, ct)
            ?? throw new KeyNotFoundException($"Id {id} not found");

        if (req.BillingStartDate.HasValue) e.BillingStartDate = req.BillingStartDate.Value;
        if (req.BillingEndDate.HasValue) e.BillingEndDate = req.BillingEndDate.Value;
        if (req.BillingDays.HasValue) e.BillingDays = req.BillingDays.Value;
        if (req.TotalUsage.HasValue) e.TotalUsage = req.TotalUsage.Value;
        if (req.CurrentUsage.HasValue) e.CurrentUsage = req.CurrentUsage.Value;
        if (req.CurrentMeterReading.HasValue) e.CurrentMeterReading = req.CurrentMeterReading.Value;
        if (req.PreviousMeterReading.HasValue) e.PreviousMeterReading = req.PreviousMeterReading.Value;
        if (req.TotalAmount.HasValue) e.TotalAmount = req.TotalAmount.Value;
        if (req.Remark != null) e.Remark = req.Remark;
        e.UpdateTime = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return MapToDto(e);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var e = await _db.LifeWaterBillPeriods.FindAsync(new object[] { id }, ct)
            ?? throw new KeyNotFoundException($"Id {id} not found");
        _db.LifeWaterBillPeriods.Remove(e);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<WaterDashboardPeriod>> GetDashboardAsync(int year, CancellationToken ct = default)
    {
        var records = await _db.LifeWaterBillPeriods
            .Where(e => e.BillingEndDate.Year == year)
            .OrderBy(e => e.BillingEndDate)
            .ToListAsync(ct);

        return records.Select((r, idx) => new WaterDashboardPeriod
        {
            PeriodIndex = idx + 1,
            BillingStartDate = r.BillingStartDate.ToString("yyyy-MM-dd"),
            BillingEndDate = r.BillingEndDate.ToString("yyyy-MM-dd"),
            PeriodLabel = $"{r.BillingStartDate:MM/dd} ~ {r.BillingEndDate:MM/dd}",
            UsageTotal = r.TotalUsage ?? r.CurrentUsage,
            AmountTotal = r.TotalAmount,
            Remark = r.Remark,
        }).ToList();
    }

    private static WaterBillDto MapToDto(LifeWaterBillPeriod e) => new()
    {
        Id = e.Id,
        WaterAddress = e.WaterAddress,
        WaterNo = e.WaterNo,
        MeterNo = e.MeterNo,
        BillingStartDate = e.BillingStartDate.ToString("yyyy-MM-dd"),
        BillingEndDate = e.BillingEndDate.ToString("yyyy-MM-dd"),
        BillingDays = e.BillingDays,
        BillingPeriodText = e.BillingPeriodText,
        TotalUsage = e.TotalUsage,
        CurrentUsage = e.CurrentUsage,
        CurrentMeterReading = e.CurrentMeterReading,
        PreviousMeterReading = e.PreviousMeterReading,
        TotalAmount = e.TotalAmount,
        TariffType = e.TariffType,
        RawDetailJson = e.RawDetailJson,
        Remark = e.Remark,
        CreateTime = e.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
    };
}
