using HSPAS.Api.Infrastructure;
using HSPAS.Api.Services;
using HSPAS.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- EF Core ---
builder.Services.AddDbContext<HspasDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- HttpClient for TWSE ---
builder.Services.AddHttpClient("TWSE", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "HSPAS/1.0");
});

// --- DI: Services ---
builder.Services.AddScoped<ITwseDataService, TwseDataService>();
builder.Services.AddScoped<IDailyPriceService, DailyPriceService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddSingleton<ITechnicalIndicatorService, TechnicalIndicatorService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IQuarterHealthReportService, QuarterHealthReportService>();
builder.Services.AddScoped<IHealthReportOcrService, HealthReportOcrService>();
builder.Services.AddScoped<IElectricityBillService, ElectricityBillService>();
builder.Services.AddScoped<IWaterBillService, WaterBillService>();

// --- Controllers & Swagger ---
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// --- Auto-migrate on startup (dev only) ---
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<HspasDbContext>();
    db.Database.Migrate();
}

// --- Middleware ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // 開發環境下禁止快取靜態檔案，確保每次都載入最新版本
        if (app.Environment.IsDevelopment())
        {
            ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            ctx.Context.Response.Headers["Pragma"] = "no-cache";
            ctx.Context.Response.Headers["Expires"] = "0";
        }
    }
});
app.MapControllers();

// SPA fallback: 非 API 且非靜態檔的請求都回傳 index.html
app.MapFallbackToFile("index.html");

app.Run();
