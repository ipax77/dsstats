using dsstats.indexedDb.Services;
using dsstats.pwa;
using dsstats.pwa.Services;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using pax.BBToast;
using pax.BlazorChartJs;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.Configure<HostOptions>(options =>
{
    options.Kind = HostAppKind.BlazorWasmPwa;
});

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddHttpClient("ApiClient", client =>
{
    var env = builder.HostEnvironment;
    client.BaseAddress = env.IsProduction()
        ? new Uri("https://dsstats.pax77.org")
        : new Uri("http://localhost:5279");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Authorization = new("DS8upload77");
});

builder.Services.AddHttpClient("api", client =>
{
    var env = builder.HostEnvironment;
    client.BaseAddress = env.IsProduction()
        ? new Uri("https://dsstats.pax77.org")
        : new Uri("http://localhost:5279");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Authorization = new("DS8upload77");
});

builder.Services.AddChartJs(options =>
{
    var version = "4.5.1";
    options.ChartJsLocation = $"/_content/dsstats.weblib/js/chart.umd.min.js?v={version}";
    options.ChartJsPluginDatalabelsLocation = "/_content/dsstats.weblib/js/chartjs-plugin-annotation.min.js";
});
builder.Services.AddBbToast();


builder.Services.AddScoped<IndexedDbService>();
builder.Services.AddSingleton<PwaConfigService>();
builder.Services.AddScoped<BackupService>();
builder.Services.AddScoped<IReplayRepository, ReplayRepository>();
builder.Services.AddScoped<IPlayerService, dsstats.apiServices.PlayerService>();
builder.Services.AddScoped<IStatsService, dsstats.apiServices.StatsService>();

builder.Services.AddSingleton<DecodeService>();

await builder.Build().RunAsync();
