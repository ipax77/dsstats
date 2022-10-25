using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;
using pax.dsstats.web.Server.Services;

namespace dsstats.Tests;

public class Startup
{
    public void ConfigureHost(IHostBuilder hostBuilder) =>
    hostBuilder.ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.AddJsonFile("/data/localserverconfig.json", optional: false, reloadOnChange: false);
    });

    public void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath("/data")
            .AddJsonFile($"localserverconfig.json")
            .Build();

        var serverVersion = new MySqlServerVersion(new System.Version(5, 7, 39));
        var connectionString = configuration["ServerConfig:TestConnectionString"];

        services.AddDbContext<ReplayContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion, p =>
            {
                p.EnableRetryOnFailure();
                p.MigrationsAssembly("MysqlMigrations");
                p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
        });

        services.AddMemoryCache();
        services.AddAutoMapper(typeof(AutoMapperProfile));

        services.AddSingleton<MmrService>();
        services.AddSingleton<FireMmrService>();
        services.AddSingleton<UploadService>();

        services.AddTransient<IStatsService, StatsService>();
        services.AddTransient<IReplayRepository, ReplayRepository>();
        services.AddTransient<IStatsRepository, StatsRepository>();
        services.AddTransient<BuildService>();

        var serviceProvider = services.BuildServiceProvider();
        var context = serviceProvider.GetService<ReplayContext>();
        ArgumentNullException.ThrowIfNull(context);
        context.Database.EnsureDeleted();
        context.Database.Migrate();
    }
}
