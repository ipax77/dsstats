using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using dsstats.wasm;
using dsstats.shared.Interfaces;
using dsstats.apiServices;
using dsstats.wasm.Services;
using pax.BlazorChartJs;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

if (builder.HostEnvironment.IsDevelopment())
{
    builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7048") });
}
if (builder.HostEnvironment.IsProduction())
{
    builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://dsstats.pax77.org") });
    // builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://dsstats-dev.pax77.org") });
}

builder.Services.AddChartJs(options =>
{
    options.ChartJsLocation = "/_content/dsstats.razorlib/js/chart.js";
    options.ChartJsPluginDatalabelsLocation = "/_content/dsstats.razorlib/js/chartjs-plugin-datalabels.js";
});

builder.Services.AddSingleton<IRemoteToggleService, RemoteToggleService>();

builder.Services.AddScoped<IWinrateService, WinrateService>();
builder.Services.AddScoped<ITimelineService, TimelineService>();
builder.Services.AddScoped<ISynergyService, SynergyService>();
builder.Services.AddScoped<IDurationService, DurationService>();
builder.Services.AddScoped<IReplaysService, ReplaysService>();
builder.Services.AddScoped<IDamageService, DamageService>();
builder.Services.AddScoped<ICountService, CountService>();
builder.Services.AddScoped<ITeamcompService, TeamcompService>();
builder.Services.AddScoped<IArcadeService, ArcadeService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IBuildService, BuildService>();
builder.Services.AddScoped<ICmdrInfoService, CmdrInfoService>();
builder.Services.AddScoped<ITourneysService, TourneysService>();

await builder.Build().RunAsync();
