using dsstats.worker;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using Microsoft.EntityFrameworkCore;

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


builder.Services.AddSingleton<DsstatsService>();
builder.Services.AddHostedService<WindowsBackgroundService>();

builder.Logging.AddConfiguration(
    builder.Configuration.GetSection("Logging"));
    
var host = builder.Build();

var dsstatsService = host.Services.GetRequiredService<DsstatsService>();

host.Run();
