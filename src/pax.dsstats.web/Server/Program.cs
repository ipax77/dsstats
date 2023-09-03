using AutoMapper;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.dbng.Services.MergeService;
using pax.dsstats.dbng.Services.Ratings;
using pax.dsstats.dbng.Services.ServerStats;
using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;
using pax.dsstats.shared.Interfaces;
using pax.dsstats.shared.Services;
using pax.dsstats.web.Server.Attributes;
using pax.dsstats.web.Server.Hubs;
using pax.dsstats.web.Server.Services;
using pax.dsstats.web.Server.Services.Arcade;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppConfiguration((context, config) =>
{
    config.AddJsonFile("/data/localserverconfig.json", optional: true, reloadOnChange: false);
});


// Add services to the container.

var serverVersion = new MySqlServerVersion(new System.Version(5, 7, 43));
var connectionString = builder.Configuration["ServerConfig:DsstatsConnectionString"];
var importConnectionString = builder.Configuration["ServerConfig:ImportConnectionString"];

// var connectionString = builder.Configuration["ServerConfig:DsstatsProdConnectionString"];

// var connectionString = builder.Configuration["ServerConfig:TestConnectionString"];
// var importConnectionString = builder.Configuration["ServerConfig:ImportTestConnectionString"];

builder.Services.AddOptions<DbImportOptions>()
    .Configure(x => x.ImportConnectionString = importConnectionString);

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
builder.Services.AddSingleton<pax.dsstats.web.Server.Services.Import.ImportService>();
builder.Services.AddSingleton<RatingsService>();

builder.Services.AddScoped<ArcadeRatingsService>();
builder.Services.AddScoped<IRatingRepository, pax.dsstats.dbng.Services.RatingRepository>();
// builder.Services.AddScoped<ImportService>();
// builder.Services.AddScoped<MmrProduceService>();
builder.Services.AddScoped<CheatDetectService>();
builder.Services.AddScoped<PlayerService>();
builder.Services.AddScoped<CrawlerService>();
builder.Services.AddScoped<IServerStatsService, ServerStatsService>();
builder.Services.AddScoped<ReplaysMergeService>();
builder.Services.AddScoped<IDurationService, DurationService>();
builder.Services.AddScoped<ITimelineService, TimelineService>();
builder.Services.AddScoped<IDsUpdateService, DsUpdateService>();
builder.Services.AddScoped<IWinrateService, WinrateService>();
builder.Services.AddScoped<ISynergyService, SynergyService>();
builder.Services.AddScoped<IDamageService, DamageService>();
builder.Services.AddScoped<ITeamService, TeamService>();

builder.Services.AddTransient<IStatsService, StatsService>();
builder.Services.AddTransient<IReplayRepository, ReplayRepository>();
builder.Services.AddTransient<IStatsRepository, StatsRepository>();
builder.Services.AddTransient<BuildService>();
builder.Services.AddTransient<CmdrsService>();
builder.Services.AddTransient<TourneyService>();
builder.Services.AddTransient<IArcadeService, ArcadeService>();

builder.Services.AddHostedService<CacheBackgroundService>();
builder.Services.AddHostedService<RatingsBackgroundService>();

builder.Services.AddHttpClient("sc2arcardeClient")
    .ConfigureHttpClient(options =>
    {
        options.BaseAddress = new Uri("https://api.sc2arcade.com");
        options.DefaultRequestHeaders.Add("Accept", "application/json");
    });

var app = builder.Build();

using var scope = app.Services.CreateScope();

var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
mapper.ConfigurationProvider.AssertConfigurationIsValid();

using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
// context.Database.EnsureDeleted();
context.Database.Migrate();

// SEED
if (app.Environment.IsProduction())
{
    var buildService = scope.ServiceProvider.GetRequiredService<BuildService>();
    buildService.SeedBuildsCache().GetAwaiter().GetResult();

    var tourneyService = scope.ServiceProvider.GetRequiredService<TourneyService>();
    tourneyService.CollectTourneyReplays().Wait();

    var importService = scope.ServiceProvider.GetRequiredService<pax.dsstats.web.Server.Services.Import.ImportService>();
    importService.ImportInit();
}

// DEBUG
if (app.Environment.IsDevelopment())
{
    var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();
    // ratingsService.NoUploadAdjustment().Wait();
    // ratingsService.ProduceRatings(recalc: true).Wait();
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
