using AutoMapper;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;
using pax.dsstats.web.Server.Attributes;
using pax.dsstats.web.Server.Hubs;
using pax.dsstats.web.Server.Services;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppConfiguration((context, config) =>
{
    config.AddJsonFile("/data/localserverconfig.json", optional: true, reloadOnChange: false);
});


// Add services to the container.

var serverVersion = new MySqlServerVersion(new System.Version(5, 7, 40));
var connectionString = builder.Configuration["ServerConfig:DsstatsConnectionString"];
var importConnectionString = builder.Configuration["ServerConfig:ImportConnectionString"];

// var connectionString = builder.Configuration["ServerConfig:DsstatsProdConnectionString"];
// var connectionString = builder.Configuration["ServerConfig:TestConnectionString"];

builder.Services.AddDbContext<ReplayContext>(options =>
{
    options.UseMySql(connectionString, serverVersion, p =>
    {
        p.CommandTimeout(120);
        p.EnableRetryOnFailure();
        p.MigrationsAssembly("MysqlMigrations");
        p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
    })
    //.EnableDetailedErrors()
    //.EnableSensitiveDataLogging()
    ;
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddMemoryCache();
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
builder.Services.AddSignalR();

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

builder.Services.AddSingleton<UploadService>();
builder.Services.AddSingleton<AuthenticationFilterAttribute>();
builder.Services.AddSingleton<PickBanService>();

builder.Services.AddScoped<IRatingRepository, pax.dsstats.dbng.Services.RatingRepository>();
builder.Services.AddScoped<ImportService>();
builder.Services.AddScoped<MmrProduceService>();
builder.Services.AddScoped<CheatDetectService>();

builder.Services.AddTransient<IStatsService, StatsService>();
builder.Services.AddTransient<IReplayRepository, ReplayRepository>();
builder.Services.AddTransient<IStatsRepository, StatsRepository>();
builder.Services.AddTransient<BuildService>();
builder.Services.AddTransient<CmdrsService>();
builder.Services.AddTransient<TourneyService>();

builder.Services.AddHostedService<CacheBackgroundService>();
builder.Services.AddHostedService<RatingsBackgroundService>();

var app = builder.Build();

Data.MysqlConnectionString = importConnectionString;
using var scope = app.Services.CreateScope();

var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
mapper.ConfigurationProvider.AssertConfigurationIsValid();

using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
// context.Database.EnsureDeleted();
context.Database.Migrate();

// SEED
if (app.Environment.IsProduction())
{
    //var importService = scope.ServiceProvider.GetRequiredService<ImportService>();
    //importService.DEBUGFixComputerGames();

    var mmrProduceService = scope.ServiceProvider.GetRequiredService<MmrProduceService>();
    mmrProduceService.ProduceRatings(new(true)).GetAwaiter().GetResult();

    var buildService = scope.ServiceProvider.GetRequiredService<BuildService>();
    buildService.SeedBuildsCache().GetAwaiter().GetResult();

    var tourneyService = scope.ServiceProvider.GetRequiredService<TourneyService>();
    tourneyService.CollectTourneyReplays().Wait();
}

// DEBUG
if (app.Environment.IsDevelopment())
{
    // var cheatDetectService = scope.ServiceProvider.GetRequiredService<CheatDetectService>();
    // var result = cheatDetectService.Detect(true).GetAwaiter().GetResult();
    // cheatDetectService.DetectNoUpload().Wait();

    //var mmrProduceService = scope.ServiceProvider.GetRequiredService<MmrProduceService>();
    //mmrProduceService.ProduceRatings(new(reCalc: true)).GetAwaiter().GetResult();

    //var statsService = scope.ServiceProvider.GetRequiredService<IStatsService>();
    //var result = statsService.GetCrossTable(new());

    //var importService = scope.ServiceProvider.GetRequiredService<ImportService>();
    //importService.DEBUGFixComputerGames();
    //importService.ImportReplayBlobs().Wait();

    //var tourneyService = scope.ServiceProvider.GetRequiredService<TourneyService>();
    //tourneyService.CollectTourneyReplays().Wait();

    //var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
    //replayRepository.FixPlayerNames().Wait();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();


app.MapRazorPages();
app.MapControllers();
app.MapHub<PickBanHub>("/hubs/pickban");
app.MapFallbackToFile("index.html");

app.Run();
