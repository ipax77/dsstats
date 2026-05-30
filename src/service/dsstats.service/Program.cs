using dsstats.db;
using dsstats.dbServices;
using dsstats.service;
using dsstats.service.Models;
using dsstats.service.Services;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Net;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Dsstats Service";
});

builder.Services.Configure<DsstatsConfig>(
    builder.Configuration.GetSection("DsstatsConfig"));

var servicePaths = DsstatsServicePaths.Initialize();
var sqliteConnectionString = $"Data Source={servicePaths.DatabasePath}";
builder.Services.AddDbContext<DsstatsContext>(ConfigureDsstatsContext);
builder.Services.AddDbContextFactory<DsstatsContext>(ConfigureDsstatsContext);

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
builder.Services.AddSingleton<DsstatsService>();
builder.Services.AddOptions();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
using var scope = host.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
context.Database.Migrate();

host.Run();

void ConfigureDsstatsContext(DbContextOptionsBuilder options)
{
    options.UseSqlite(sqliteConnectionString, sqlOptions =>
    {
        sqlOptions.MigrationsAssembly("dsstats.migrations.sqlite");
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
    });
}
