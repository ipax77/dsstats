using Blazored.LocalStorage;
using Blazored.Toast;
using dsstats.apiServices;
using dsstats.shared.Interfaces;
using dsstats.web.Client.Pages;
using dsstats.web.Client.Services;
using dsstats.web.Components;
using pax.BlazorChartJs;
using dsstats.authclient;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5116") });

    builder.Services.AddDsstatsAuthClient(options =>
    {
        options.ApiBaseUri = new Uri("http://localhost:5116");
    });
}
if (builder.Environment.IsProduction())
{
    builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://dsstats.pax77.org") });
    
    builder.Services.AddDsstatsAuthClient(options =>
    {
        options.ApiBaseUri = new Uri("https://dsstats.pax77.org");
    });
}

builder.Services.AddOptions();

builder.Services.AddChartJs(options =>
{
    options.ChartJsLocation = "/_content/dsstats.razorlib/js/chart.umd.js";
    options.ChartJsPluginDatalabelsLocation = "/_content/dsstats.razorlib/js/chartjs-plugin-datalabels.js";
});
builder.Services.AddBlazoredLocalStorage();

builder.Services.AddBlazoredToast();

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
builder.Services.AddScoped<IUnitmapService, UnitmapService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IDsDataService, DsDataService>();
builder.Services.AddScoped<IFaqService, FaqService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(InfoPage).Assembly);

app.Run();
