using dsstats.api;
using dsstats.api.Hubs;
using dsstats.api.Services;
using dsstats.db;
using dsstats.db.Services.Stats;
using dsstats.dbServices;
using dsstats.dbServices.Builds;
using dsstats.dbServices.Stats;
using dsstats.ratings;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using sc2arcade.crawler;
using System.Threading.RateLimiting;

var MyAllowSpecificOrigins = "dsstatsOrigin";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
if (builder.Environment.IsProduction())
{
    builder.Configuration.AddJsonFile("/data/localserverconfig.json", optional: true, reloadOnChange: false);
}
builder.Services.AddLogging(l => l.AddSimpleConsole(o => o.TimestampFormat = "yyyy-MM-dd HH:mm:ss: "));

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          var allowedOrigins = new HashSet<string>
                          {
                              "https://dsstats.pax77.org",
                              "https://dsstats-dev.pax77.org",
                              "https://mydsstats.pax77.org",
                              "https://localhost:7257",
                              "https://localhost:7227",
                              "http://localhost:5123",
                              "https://localhost:7039"
                          };

                          if (builder.Environment.IsDevelopment())
                          {
                              allowedOrigins.Add("https://localhost:7240");
                              allowedOrigins.Add("http://localhost:5261");
                              allowedOrigins.Add("https://localhost:7039");
                          }

                          policy.WithOrigins([.. allowedOrigins])
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 4;
        options.Window = TimeSpan.FromSeconds(12);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    });
});

builder.Services.AddDbConfig(builder.Configuration);
builder.Services.AddUploadChannels();

if (builder.Environment.IsProduction())
{
    builder.Services.AddHostedService<TimedHostedService>();
}

builder.Services.AddSignalR();
builder.Services.AddMemoryCache();
builder.Services.AddSC2ArcadeCrawler();


builder.Services.AddSingleton<AuthenticationFilterAttribute>();
builder.Services.AddSingleton<UploadService>();
builder.Services.AddSingleton<IImportService, ImportService>();
builder.Services.AddSingleton<IRatingService, RatingService>();

builder.Services.AddScoped<IDashboardStatsService, DashboardStatsService>();
builder.Services.AddScoped<IReplayRepository, ReplayRepository>();
builder.Services.AddScoped<IPlayerService, PlayerService>();

builder.Services.AddScoped<IStatsProvider, WinrateStatsProvider>();
builder.Services.AddScoped<IStatsService, StatsService>();
builder.Services.AddScoped<IBuildsService, BuildsService>();

builder.Services.AddScoped<TransitionService>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddRequestDecompression();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

var app = builder.Build();

using var scope = app.Services.CreateScope();
using var dbContext = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
dbContext.Database.Migrate();

//var transitionService = scope.ServiceProvider.GetRequiredService<TransitionService>();
//transitionService.FixHashes().Wait();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseForwardedHeaders();
// app.UseHttpsRedirection();

app.UseRateLimiter();
app.UseCors(MyAllowSpecificOrigins);


app.UseAuthorization();

app.UseRequestDecompression();
app.UseResponseCompression();
app.MapControllers();

app.MapHub<UploadHub>("/hubs/upload");

app.Run();
