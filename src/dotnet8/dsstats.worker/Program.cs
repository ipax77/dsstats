using dsstats.worker;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng.Repositories;
using System.Net.Http.Headers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Dsstats Service";
});

var sqliteConnectionString = $"Data Source={Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "dsstats.worker", "dsstats.db")}";
builder.Services.AddOptions<DbImportOptions>()
    .Configure(x => x.ImportConnectionString = sqliteConnectionString);
builder.Services.AddDbContext<ReplayContext>(options => options
    .UseSqlite(sqliteConnectionString, sqlOptions =>
    {
        sqlOptions.MigrationsAssembly("SqliteMigrations");
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
    })
//.EnableDetailedErrors()
//.EnableSensitiveDataLogging()
);

builder.Services.AddHttpClient("dsstats")
    .ConfigureHttpClient(options => {
        options.BaseAddress = new Uri("https://dsstats.pax77.org");
        // options.BaseAddress = new Uri("http://localhost:5090");
        options.DefaultRequestHeaders.Add("Accept", "application/json");
        options.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DSupload77");
    });
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
builder.Services.AddSingleton<DsstatsService>();
builder.Services.AddScoped<IReplayRepository, ReplayRepository>();
builder.Services.AddHostedService<WindowsBackgroundService>();

builder.Logging.AddConfiguration(
    builder.Configuration.GetSection("Logging"));
    
var host = builder.Build();

var dsstatsService = host.Services.GetRequiredService<DsstatsService>();

host.Run();
