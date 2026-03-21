using HSPAS.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace HSPAS.Api.Infrastructure;

/// <summary>HSPAS 資料庫上下文</summary>
public class HspasDbContext : DbContext
{
    public HspasDbContext(DbContextOptions<HspasDbContext> options) : base(options) { }

    public DbSet<DailyStockPrice> DailyStockPrices => Set<DailyStockPrice>();
    public DbSet<TradeRecord> TradeRecords => Set<TradeRecord>();
    public DbSet<DcaPlan> DcaPlans => Set<DcaPlan>();
    public DbSet<DcaExecution> DcaExecutions => Set<DcaExecution>();
    public DbSet<EtfInfo> EtfInfos => Set<EtfInfo>();
    public DbSet<MenuFunction> MenuFunctions => Set<MenuFunction>();
    public DbSet<QuarterHealthReport> QuarterHealthReports => Set<QuarterHealthReport>();
    public DbSet<QuarterHealthReportDetail> QuarterHealthReportDetails => Set<QuarterHealthReportDetail>();
    public DbSet<LifeElectricityBillPeriod> LifeElectricityBillPeriods => Set<LifeElectricityBillPeriod>();
    public DbSet<LifeWaterBillPeriod> LifeWaterBillPeriods => Set<LifeWaterBillPeriod>();
    public DbSet<UsTradeRecord> UsTradeRecords => Set<UsTradeRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // DailyStockPrice 複合主鍵
        modelBuilder.Entity<DailyStockPrice>()
            .HasKey(d => new { d.TradeDate, d.StockId, d.MarketType });

        modelBuilder.Entity<DailyStockPrice>()
            .Property(d => d.MarketType)
            .HasDefaultValue("TSE");

        modelBuilder.Entity<DailyStockPrice>()
            .Property(d => d.CreateTime)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // TradeRecord
        modelBuilder.Entity<TradeRecord>()
            .Property(t => t.CreateTime)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // DcaPlan
        modelBuilder.Entity<DcaPlan>()
            .Property(p => p.CreateTime)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // DcaExecution FK
        modelBuilder.Entity<DcaExecution>()
            .HasOne(e => e.Plan)
            .WithMany(p => p.Executions)
            .HasForeignKey(e => e.PlanId);

        modelBuilder.Entity<DcaExecution>()
            .Property(e => e.CreateTime)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // EtfInfo
        modelBuilder.Entity<EtfInfo>()
            .Property(e => e.CreateTime)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // MenuFunction
        modelBuilder.Entity<MenuFunction>()
            .HasIndex(m => m.FuncCode)
            .IsUnique();

        modelBuilder.Entity<MenuFunction>()
            .Property(m => m.CreateTime)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<MenuFunction>()
            .Property(m => m.IsActive)
            .HasDefaultValue(true);

        // QuarterHealthReport
        modelBuilder.Entity<QuarterHealthReport>()
            .Property(r => r.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // QuarterHealthReportDetail FK
        modelBuilder.Entity<QuarterHealthReportDetail>()
            .HasOne(d => d.Report)
            .WithOne(r => r.Detail)
            .HasForeignKey<QuarterHealthReportDetail>(d => d.ReportId);

        modelBuilder.Entity<QuarterHealthReportDetail>()
            .Property(d => d.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // LifeElectricityBillPeriod
        modelBuilder.Entity<LifeElectricityBillPeriod>()
            .Property(e => e.CreateTime)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<LifeElectricityBillPeriod>()
            .HasIndex(e => new { e.PowerNo, e.BillingEndDate });

        modelBuilder.Entity<LifeElectricityBillPeriod>()
            .HasIndex(e => new { e.PowerNo, e.ReadOrDebitDate });

        // LifeWaterBillPeriod
        modelBuilder.Entity<LifeWaterBillPeriod>()
            .Property(e => e.CreateTime)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<LifeWaterBillPeriod>()
            .HasIndex(e => new { e.WaterNo, e.BillingEndDate });

        // UsTradeRecord
        modelBuilder.Entity<UsTradeRecord>()
            .Property(e => e.CreateTime)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<UsTradeRecord>()
            .HasIndex(e => e.StockSymbol);

        modelBuilder.Entity<UsTradeRecord>()
            .HasIndex(e => e.TradeDate);
    }
}
