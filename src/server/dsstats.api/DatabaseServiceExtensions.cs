using dsstats.db;
using dsstats.db.Old;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.api;

public static class DatabaseServiceExtensions
{
    public static IServiceCollection AddDbConfig(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["dsstats:ConnectionString"] ?? string.Empty;
        var oldConnectionString = configuration["ServerConfig:ProdConnectionString"] ?? string.Empty;
        var importConnectionString = configuration["dsstats:ImportConnectionString"] ?? string.Empty;

        var serverVersion = new MySqlServerVersion(new Version(8, 4, 7));
        var oldServerVersion = new MySqlServerVersion(new Version(5, 7, 44));

        // DsstatsContext (registered once - removed duplicate)
        services.AddDbContext<DsstatsContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion, o =>
            {
                o.MigrationsAssembly("dsstats.migrations.mysql");
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
            //options.EnableDetailedErrors();
            //options.EnableSensitiveDataLogging();
        });

        // StagingDsstatsContext
        services.AddDbContext<StagingDsstatsContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion, o =>
            {
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
            //options.EnableDetailedErrors();
            //options.EnableSensitiveDataLogging();
        });

        // OldReplayContext
        services.AddDbContext<OldReplayContext>(options =>
        {
            options.UseMySql(oldConnectionString, oldServerVersion, o =>
            {
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
        });

        // Import options
        services.AddOptions<ImportOptions>()
            .Configure(x => x.ConnectionString = importConnectionString);

        return services;
    }
}
