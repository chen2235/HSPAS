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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // DailyStockPrice 複合主鍵
        modelBuilder.Entity<DailyStockPrice>()
            .HasKey(d => new { d.TradeDate, d.StockId });

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
    }
}
