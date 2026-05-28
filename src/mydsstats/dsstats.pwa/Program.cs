using dsstats.indexedDb.Services;
using dsstats.pwa;
using dsstats.pwa.Services;
using dsstats.shared;
using dsstats.shared.InHouse;
using dsstats.shared.Interfaces;
using dsstats.weblib.Replays;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using pax.BlazorChartJs;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.Configure<HostOptions>(options =>
{
    options.Kind = HostAppKind.BlazorWasmPwa;
});

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

var isProduction = builder.Configuration.GetValue<bool?>("Dsstats:Pwa:IsProduction")
    ?? builder.HostEnvironment.IsProduction();
var inHouseHubBaseAddress = isProduction
    ? new Uri(builder.HostEnvironment.BaseAddress)
    : new Uri("http://localhost:5279");

builder.Services.AddSingleton(new InHouseHubOptions(inHouseHubBaseAddress));

builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = isProduction
        ? new Uri("https://dsstats.pax77.org")
        : new Uri("http://localhost:5279");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Authorization = new("DS8upload77");
});

builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = isProduction
        ? new Uri("https://dsstats.pax77.org")
        : new Uri("http://localhost:5279");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Authorization = new("DS8upload77");
});

builder.Services.AddHttpClient("InHouseApi", client =>
{
    client.BaseAddress = isProduction
        ? new Uri("https://dsstats.pax77.org")
        : new Uri("http://localhost:5279");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddChartJs(options =>
{
    var version = "4.5.1";
    options.ChartJsLocation = $"/_content/dsstats.weblib/js/chart.umd.min.js?v={version}";
    options.ChartJsPluginDatalabelsLocation = "/_content/dsstats.weblib/js/chartjs-plugin-annotation.min.js";
    options.ChartJsCallbacksModuleLocation = "/_content/dsstats.weblib/js/chartJsCallbacks.js?v=0.1";
});


builder.Services.AddScoped<IndexedDbService>();
builder.Services.AddSingleton<AppNotificationService>();
builder.Services.AddSingleton<PwaConfigService>();
builder.Services.AddSingleton<PwaUpdateService>();
builder.Services.AddScoped<BackupService>();
builder.Services.AddScoped<ISpawnPlaybackSidecarDecoder, BrowserSpawnPlaybackSidecarDecoder>();
builder.Services.AddScoped<SpawnPlaybackSidecarCache>();
builder.Services.AddScoped<SpawnPositionHydrationService>();
builder.Services.AddScoped<IReplayRepository, ReplayRepository>();
builder.Services.AddScoped<RatingService>();
builder.Services.AddScoped<SessionProgressService>();
builder.Services.AddScoped<InHouseAuthClient>();
builder.Services.AddScoped<InHouseGameSessionsClient>();
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy(InHousePolicies.CloseSession, policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(context => InHouseAuthorization.CanCloseSession(
            context.User,
            context.Resource as IInHouseSessionAuthorizationResource)));
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, InHouseAuthenticationStateProvider>();
builder.Services.AddScoped<IPlayerService, dsstats.apiServices.PlayerService>();
builder.Services.AddScoped<IStatsService, dsstats.apiServices.StatsService>();
builder.Services.AddScoped<IUnitLifeCostService, NoOpUnitLifeCostService>();

builder.Services.AddSingleton<DecodeService>();

await builder.Build().RunAsync();
