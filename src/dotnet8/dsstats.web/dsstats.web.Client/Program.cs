using dsstats.shared.Interfaces;
using dsstats.webclient.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using pax.BlazorChartJs;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddChartJs();

builder.Services.AddScoped<IWinrateService, WinrateService>();

await builder.Build().RunAsync();
