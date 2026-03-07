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
builder.Services.AddScoped<IBackfillService, BackfillService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddSingleton<ITechnicalIndicatorService, TechnicalIndicatorService>();

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
app.UseStaticFiles();
app.MapControllers();

// SPA fallback: 非 API 且非靜態檔的請求都回傳 index.html
app.MapFallbackToFile("index.html");

app.Run();
