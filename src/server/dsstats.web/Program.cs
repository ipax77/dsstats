using dsstats.apiServices;
using dsstats.shared;
using dsstats.shared.Builder;
using dsstats.shared.InHouse;
using dsstats.shared.Interfaces;
using dsstats.weblib.Replays;
using dsstats.web.Components;
using pax.BlazorChartJs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(l => l.AddSimpleConsole(o => o.TimestampFormat = "yyyy-MM-dd HH:mm:ss: "));

builder.Services.AddHttpClient("api", httpClient =>
{
    var defaultApiBaseAddress = builder.Environment.IsDevelopment()
        ? "http://localhost:5279"
        : "http://api:8080";
    var configuredApiBaseAddress = builder.Configuration["ApiClient:BaseAddress"]
        ?? defaultApiBaseAddress;
    httpClient.BaseAddress = new Uri(configuredApiBaseAddress, UriKind.Absolute);
    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddChartJs(options =>
{
    var version = "4.5.1";
    options.ChartJsLocation = $"/_content/dsstats.weblib/js/chart.umd.min.js?v={version}";
    options.ChartJsPluginDatalabelsLocation = "/_content/dsstats.weblib/js/chartjs-plugin-datalabels.min.js";
    options.ChartJsCallbacksModuleLocation = "/_content/dsstats.weblib/js/chartJsCallbacks.js?v=0.2";
});
builder.Services.AddMemoryCache();
builder.Services.Configure<dsstats.shared.HostOptions>(options =>
{
    options.Kind = HostAppKind.BlazorServer;
});
builder.Services.Configure<ReplayUserRatingClientOptions>(options =>
{
    var defaultApiBaseAddress = builder.Environment.IsDevelopment()
        ? "http://localhost:5279"
        : string.Empty;
    options.ApiBaseAddress = builder.Configuration["ReplayUserRating:ApiBaseAddress"]
        ?? defaultApiBaseAddress;
});

builder.Services.AddScoped<ISpawnPlaybackSidecarDecoder, DotNetSpawnPlaybackSidecarDecoder>();
builder.Services.AddScoped<SpawnPlaybackSidecarCache>();
builder.Services.AddScoped<SpawnPositionHydrationService>();
builder.Services.AddSingleton<IBuilderService, UnavailableBuilderService>();
builder.Services.AddScoped<IReplayRepository, ReplayRepository>();
builder.Services.AddScoped<IReplayImportService, ReplayImportService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();

builder.Services.AddScoped<IStatsService, StatsService>();
builder.Services.AddScoped<IBuildsService, BuildsService>();
builder.Services.AddScoped<IUnitLifeCostService, UnitLifeCostService>();
builder.Services.AddScoped<IBuildDetailsService, BuildDetailsService>();
builder.Services.AddScoped<IDashboardStatsService, DashboardStatsService>();
builder.Services.AddScoped<IInHouseClosedGameSessionService, InHouseClosedGameSessionService>();
builder.Services.AddScoped<IPatchNotesService, PatchNotesService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
// app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapGet("/health/live", static () => Results.NoContent());
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
