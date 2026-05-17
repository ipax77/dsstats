using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace dsstats.api;

public static class DatabaseServiceExtensions
{
    public static IServiceCollection AddDbConfig(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["dsstats:ConnectionString"] ?? string.Empty;
        var importConnectionString = configuration["dsstats:ImportConnectionString"] ?? string.Empty;

        var serverVersion = new MySqlServerVersion(new Version(9, 7, 0));

        // Keep only the hot context pooled. Do not store request/user/tenant state on DsstatsContext;
        // pass those values into service methods and LINQ queries instead.
        services.AddPooledDbContextFactory<DsstatsContext>(options =>
        {
            SuppressClientCancellationNoise(options);
            options.UseMySql(connectionString, serverVersion, o =>
            {
                o.MigrationsAssembly("dsstats.migrations.mysql");
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
            //options.EnableDetailedErrors();
            //options.EnableSensitiveDataLogging();
        });

        // Staging contexts run rare, long rating/table-swap jobs, so pooling adds little value.
        services.AddDbContextFactory<StagingDsstatsContext>(options =>
        {
            SuppressClientCancellationNoise(options);
            options.UseMySql(connectionString, serverVersion, o =>
            {
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
            //options.EnableDetailedErrors();
            //options.EnableSensitiveDataLogging();
        });

        // Import options
        services.AddOptions<ImportOptions>()
            .Configure(x => x.ConnectionString = importConnectionString);

        return services;
    }

    private static void SuppressClientCancellationNoise(DbContextOptionsBuilder options)
    {
        // Rapid UI pagination cancels in-flight requests. EF logs that as Database.Connection[20004]
        // before OperationCanceledException reaches our handlers, which floods production logs.
        options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.ConnectionError));
    }
}
