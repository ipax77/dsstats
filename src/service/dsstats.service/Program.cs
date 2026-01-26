using dsstats.db;
using dsstats.dbServices;
using dsstats.service;
using dsstats.service.Models;
using dsstats.service.Services;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using System.Net;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(o => o.Listen(IPAddress.Loopback, 5177));

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Dsstats Service";
});

builder.Services.Configure<DsstatsConfig>(
    builder.Configuration.GetSection("DsstatsConfig"));

var sqliteConnectionString = $"Data Source={Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "dsstats.worker", "dsstats3.db")}";
builder.Services.AddDbContext<DsstatsContext>(options => options
    .UseSqlite(sqliteConnectionString, sqlOptions =>
    {
        sqlOptions.MigrationsAssembly("dsstats.migrations.sqlite");
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
    })
//.EnableDetailedErrors()
//.EnableSensitiveDataLogging()
);

var dsstats = builder.Configuration.GetSection("DsstatsConfig").Get<DsstatsConfig>();

builder.Services.AddHttpClient("api", httpClient =>
{
    httpClient.BaseAddress = new Uri(dsstats!.UploadUrl);
    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
}).ConfigurePrimaryHttpMessageHandler(() =>
    new HttpClientHandler
    {
        AutomaticDecompression =
            DecompressionMethods.GZip |
            DecompressionMethods.Brotli,
    });

builder.Services.AddHttpClient("update")
    .ConfigureHttpClient(options => {
        options.BaseAddress = new Uri("https://github.com/ipax77/dsstats.service/releases/latest/download/");
        options.DefaultRequestHeaders.Add("Accept", "application/octet-stream");
    });

builder.Services.AddSingleton<IImportService, ImportService>();
builder.Services.AddScoped<IRatingService, RatingsService>();
builder.Services.AddSingleton<IDsstatsService, DsstatsService>();
builder.Services.AddOptions();
builder.Services.AddHostedService<Worker>();
builder.Services.AddControllers();

var app = builder.Build();

if (!Directory.Exists(DsstatsService.appFolder))
{
    Directory.CreateDirectory(DsstatsService.appFolder);
}
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
    context.Database.Migrate();
}

app.MapControllers();

app.Run();