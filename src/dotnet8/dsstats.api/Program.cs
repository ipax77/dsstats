using AutoMapper;
using dsstats.api;
using dsstats.api.Services;
using dsstats.db8;
using dsstats.db8.AutoMapper;
using dsstats.db8services;
using dsstats.db8services.Import;
using dsstats.ratings.lib;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var MyAllowSpecificOrigins = "dsstatsOrigin";

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("/data/localserverconfig.json", optional: true, reloadOnChange: false);

builder.Services.AddLogging(l => l.AddSimpleConsole(o => o.TimestampFormat = "yyyy-MM-ss HH:mm:ss: "));

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("https://dsstats-dev.pax77.org",
                                              "https://localhost:7257")
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                      });
});

builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 4;
        options.Window = TimeSpan.FromSeconds(12);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    }));

// Add services to the container.
var serverVersion = new MySqlServerVersion(new System.Version(5, 7, 43));
var connectionString = builder.Configuration["ServerConfig:DsstatsConnectionString"];
var importConnectionString = builder.Configuration["ServerConfig:ImportConnectionString"];

builder.Services.AddOptions<DbImportOptions>()
    .Configure(x => x.ImportConnectionString = importConnectionString ?? "");

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

builder.Services.AddMemoryCache();
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

builder.Services.AddSingleton<CalcService>();
builder.Services.AddSingleton<ImportService>();
builder.Services.AddSingleton<UploadService>();
builder.Services.AddSingleton<AuthenticationFilterAttribute>();

builder.Services.AddScoped<ICalcRepository, CalcRepository>();
builder.Services.AddScoped<IWinrateService, WinrateService>();
builder.Services.AddScoped<ITimelineService, TimelineService>();
builder.Services.AddScoped<ISynergyService, SynergyService>();
builder.Services.AddScoped<IDurationService, DurationService>();
builder.Services.AddScoped<IDamageService, DamageService>();
builder.Services.AddScoped<ICountService, CountService>();
builder.Services.AddScoped<ITeamcompService, TeamcompService>();
builder.Services.AddScoped<IReplaysService, ReplaysService>();
//builder.Services.AddScoped<IDsstatsService, DsstatsService>();
//builder.Services.AddScoped<IArcadeService, ArcadeService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IBuildService, BuildService>();
builder.Services.AddScoped<ICmdrInfoService, CmdrInfoService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using var scope = app.Services.CreateScope();

var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
mapper.ConfigurationProvider.AssertConfigurationIsValid();

app.UseRateLimiter();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    //var calcRepository = scope.ServiceProvider.GetRequiredService<ICalcRepository>();
    //calcRepository.DebugDeleteContinueTest().Wait();

    //var calcService = scope.ServiceProvider.GetRequiredService<CalcService>();
    //await calcService.GenerateRatings(RatingCalcType.Dsstats, true);

    //var importService = scope.ServiceProvider.GetRequiredService<ImportService>();
    //importService.Init().Wait();
}

app.UseHttpsRedirection();

app.UseCors(MyAllowSpecificOrigins);

app.UseAuthorization();

app.MapControllers();

app.Run();


