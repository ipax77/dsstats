using AutoMapper;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;
using pax.dsstats.web.Server.Attributes;
using pax.dsstats.web.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppConfiguration((context, config) =>
{
    config.AddJsonFile("/data/localserverconfig.json", optional: true, reloadOnChange: false);
});


// Add services to the container.

var serverVersion = new MySqlServerVersion(new System.Version(5, 7, 40));
var connectionString = builder.Configuration["ServerConfig:DsstatsConnectionString"];
// var connectionString = builder.Configuration["ServerConfig:DsstatsProdConnectionString"];
// var connectionString = builder.Configuration["ServerConfig:TestConnectionString"];

//var oldConnectionString = builder.Configuration["ServerConfig:DBConnectionString2"];

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

//builder.Services.AddDbContext<sc2dsstatsContext>(options =>
//{
//    options.UseMySql(oldConnectionString, serverVersion, p =>
//    {
//        p.EnableRetryOnFailure();
//        p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
//    });
//});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddMemoryCache();
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

//builder.Services.AddSingleton<MmrService>();

builder.Services.AddSingleton<UploadService>();
builder.Services.AddSingleton<AuthenticationFilterAttribute>();

builder.Services.AddScoped<IRatingRepository, pax.dsstats.dbng.Services.RatingRepository>();
builder.Services.AddScoped<ImportService>();
builder.Services.AddScoped<MmrProduceService>();
builder.Services.AddScoped<CheatDetectService>();

builder.Services.AddTransient<IStatsService, StatsService>();
builder.Services.AddTransient<IReplayRepository, ReplayRepository>();
builder.Services.AddTransient<IStatsRepository, StatsRepository>();
builder.Services.AddTransient<BuildService>();
builder.Services.AddTransient<CmdrsService>();

builder.Services.AddHostedService<CacheBackgroundService>();
builder.Services.AddHostedService<RatingsBackgroundService>();

var app = builder.Build();

Data.MysqlConnectionString = connectionString;
using var scope = app.Services.CreateScope();

var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
mapper.ConfigurationProvider.AssertConfigurationIsValid();

using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
// context.Database.EnsureDeleted();
context.Database.Migrate();

// SEED
if (app.Environment.IsProduction())
{
    var mmrProduceService = scope.ServiceProvider.GetRequiredService<MmrProduceService>();
    mmrProduceService.ProduceRatings(new(true)).GetAwaiter().GetResult();

    var buildService = scope.ServiceProvider.GetRequiredService<BuildService>();
    buildService.SeedBuildsCache().GetAwaiter().GetResult();
}

// DEBUG
if (app.Environment.IsDevelopment())
{
    // var cheatDetectService = scope.ServiceProvider.GetRequiredService<CheatDetectService>();
    // var result = cheatDetectService.Detect(true).GetAwaiter().GetResult();
    // cheatDetectService.DetectNoUpload().Wait();

    var mmrProduceService = scope.ServiceProvider.GetRequiredService<MmrProduceService>();
    mmrProduceService.ProduceRatings(new(reCalc: true)).GetAwaiter().GetResult();

    //var statsService = scope.ServiceProvider.GetRequiredService<IStatsService>();
    //var result = statsService.GetCrossTable(new());
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
app.MapFallbackToFile("index.html");

app.Run();
