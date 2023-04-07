using Blazored.Toast;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using pax.BlazorChartJs;
using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;
using pax.dsstats.web.Client;
using pax.dsstats.web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddBlazoredToast();
builder.Services.AddChartJs(options =>
{
    //options.ChartJsLocation = "_content/sc2dsstats.razorlib/js/chart.js";
    //options.ChartJsPluginDatalabelsLocation = "_content/sc2dsstats.razorlib/js/chartjs-plugin-datalabels.js";
    options.ChartJsLocation = "/js/chart.js";
    options.ChartJsPluginDatalabelsLocation = "/js/chartjs-plugin-datalabels.js";
});

builder.Services.AddTransient<IDataService, DataService>();
builder.Services.AddTransient<IArcadeService, ArcadeService>();

await builder.Build().RunAsync();
