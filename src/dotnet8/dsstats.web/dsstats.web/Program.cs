using dsstats.db;
using dsstats.Services;
using dsstats.shared;
using dsstats.shared.Interfaces;
using dsstats.web.Client.Pages.Stats;
using dsstats.web.Components;
using Microsoft.EntityFrameworkCore;
using pax.BlazorChartJs;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("/data/localserverconfig.json", optional: true, reloadOnChange: false);

var serverVersion = new MySqlServerVersion(new System.Version(5, 7, 43));
var connectionString = builder.Configuration["ServerConfig:DsstatsConnectionString"];
var importConnectionString = builder.Configuration["ServerConfig:ImportConnectionString"];

// Add services to the container.
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
    // .EnableDetailedErrors()
    // .EnableSensitiveDataLogging()
    ;
});

builder.Services.AddScoped<IWinrateService, WinrateService>();
    
builder.Services.AddRazorComponents()
    .AddServerComponents()
    .AddWebAssemblyComponents();

builder.Services.AddControllers();

builder.Services.AddMemoryCache();
builder.Services.AddChartJs();

builder.Services.AddScoped<IWinrateService, WinrateService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.MapRazorComponents<App>()
    .AddServerRenderMode()
    .AddWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(WinratePage).Assembly);

app.MapControllers();

app.Run();
