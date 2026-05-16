using dsstats.api;
using dsstats.api.Authentication;
using dsstats.api.Hubs;
using dsstats.api.InHouse;
using dsstats.api.Services;
using dsstats.db;
using dsstats.dbServices;
using dsstats.dbServices.Builds;
using dsstats.dbServices.InHouse;
using dsstats.dbServices.Stats;
using dsstats.ratings;
using dsstats.shared.InHouse;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using sc2arcade.crawler;
using System.Security.Claims;
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
                          };

                          if (builder.Environment.IsDevelopment())
                          {
                              allowedOrigins.Add("https://localhost:7240");
                              allowedOrigins.Add("http://localhost:5261");
                              allowedOrigins.Add("https://localhost:7039");
                              allowedOrigins.Add("https://localhost:5066");
                              allowedOrigins.Add("http://localhost:5190");
                          }

                          policy.WithOrigins([.. allowedOrigins])
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 4;
        options.Window = TimeSpan.FromSeconds(12);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    });

    options.AddFixedWindowLimiter(policyName: "admin", options =>
    {
        options.PermitLimit = 3;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    });

    options.AddPolicy("inhouse-device-link-attempt", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(GetClientPartitionKey(httpContext), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 2,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
        }));

    options.AddPolicy("inhouse-device-link-create", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(GetInHouseUserOrClientPartitionKey(httpContext), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 2,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
        }));
});

builder.Services.AddDbConfig(builder.Configuration);
builder.Services.AddUploadChannels();
builder.Services.Configure<InHouseAuthOptions>(builder.Configuration.GetSection(InHouseAuthOptions.SectionName));

if (builder.Environment.IsProduction())
{
    builder.Services.AddHostedService<TimedHostedService>();
}

builder.Services.AddSignalR();
builder.Services.AddMemoryCache();
builder.Services.AddSC2ArcadeCrawler();

var inHouseAuthOptions = builder.Configuration.GetSection(InHouseAuthOptions.SectionName).Get<InHouseAuthOptions>() ?? new();
builder.Services.AddFido2(fidoOptions =>
{
    fidoOptions.ServerName = "mydsstats InHouse";
    fidoOptions.ServerDomain = builder.Environment.IsProduction()
        ? inHouseAuthOptions.ProductionRpId
        : inHouseAuthOptions.LocalRpId;
    fidoOptions.Origins = builder.Environment.IsProduction()
        ? new HashSet<string> { inHouseAuthOptions.ProductionOrigin }
        : inHouseAuthOptions.LocalOrigins.ToHashSet();
});

builder.Services
    .AddAuthentication(InHouseBearerAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, InHouseBearerAuthenticationHandler>(
        InHouseBearerAuthenticationHandler.SchemeName,
        _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(InHousePolicies.CloseSession, policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(context => InHouseAuthorization.CanCloseSession(
            context.User,
            context.Resource as IInHouseSessionAuthorizationResource)));
});

builder.Services.AddSingleton<AuthenticationFilterAttribute>();
builder.Services.AddSingleton<UploadService>();
builder.Services.AddSingleton<ArcadeJobService>();
builder.Services.AddSingleton<IImportService, ImportService>();
builder.Services.AddSingleton<IRatingService, RatingService>();
builder.Services.AddSingleton<IPickBanService, PickBanService>();
builder.Services.AddSingleton<InHouseConnectionTracker>();
builder.Services.AddSingleton<IInHouseAccountNotifier, InHouseAccountNotifier>();
builder.Services.AddSingleton<IInHouseGameSessionService, InHouseGameSessionService>();

builder.Services.AddScoped<IDashboardStatsService, DashboardStatsService>();
builder.Services.AddScoped<IReplayRepository, ReplayRepository>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<TransitionService>();
builder.Services.AddScoped<IInHouseAuthService, InHouseAuthService>();

builder.Services.AddStats();
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
await using var dbContext = await scope.ServiceProvider
    .GetRequiredService<IDbContextFactory<DsstatsContext>>()
    .CreateDbContextAsync();
dbContext.Database.Migrate();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //var ratingsService = scope.ServiceProvider.GetRequiredService<IRatingService>();
    //ratingsService.CreateRatings().Wait();
}

app.Use(async (httpContext, next) =>
{
    try
    {
        await next();
    }
    catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
    {
        if (!httpContext.Response.HasStarted)
        {
            httpContext.Response.StatusCode = 499;
        }
    }
});

app.UseForwardedHeaders();
// app.UseHttpsRedirection();

app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.UseRequestDecompression();
app.UseResponseCompression();
app.MapControllers();

app.MapHub<UploadHub>("/hubs/upload");
app.MapHub<PickBanHub>("/hubs/pickban");
app.MapHub<InHouseHub>("/hubs/inhouse");

app.Run();

static string GetClientPartitionKey(HttpContext httpContext)
{
    var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
    return string.IsNullOrWhiteSpace(ipAddress) ? "unknown-client" : $"ip:{ipAddress}";
}

static string GetInHouseUserOrClientPartitionKey(HttpContext httpContext)
{
    var userId = httpContext.User.FindFirstValue(InHouseClaims.UserId);
    return string.IsNullOrWhiteSpace(userId) ? GetClientPartitionKey(httpContext) : $"inhouse-user:{userId}";
}
