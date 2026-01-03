using dsstats.apiServices;
using dsstats.shared.Interfaces;
using dsstats.web.Components;
using pax.BlazorChartJs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddHttpClient("api", httpClient =>
{
    if (builder.Environment.IsDevelopment())
    {
        httpClient.BaseAddress = new Uri("http://localhost:5279");
    }
    else
    {
        httpClient.BaseAddress = new Uri("http://dsstats10");
    }
    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddChartJs(options =>
{
    var version = "4.5.1";
    options.ChartJsLocation = $"/_content/dsstats.weblib/js/chart.umd.min.js?v={version}";
    options.ChartJsPluginDatalabelsLocation = "/_content/dsstats.weblib/js/chartjs-plugin-datalabels.min.js";
});
builder.Services.AddMemoryCache();

//builder.Services.AddSingleton<IImportService, ImportService>();
//builder.Services.AddSingleton<IRatingService, RatingService>();

builder.Services.AddScoped<IReplayRepository, ReplayRepository>();
builder.Services.AddScoped<IPlayerService, PlayerService>();

builder.Services.AddScoped<IStatsService, StatsService>();
builder.Services.AddScoped<IBuildsService, BuildsService>();
builder.Services.AddScoped<IDashboardStatsService, DashboardStatsService>();

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
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
