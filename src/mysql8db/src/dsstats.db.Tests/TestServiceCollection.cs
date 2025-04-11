
using System.Text.Json;
using dsstats.db8;
using dsstats.shared8;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace dsstats.db.Tests;

public static class TestServiceCollection
{
    private static readonly string configFilePath = "/data/localserverconfig.json";

    public static ServiceCollection GetServiceCollection()
    {
        var services = new ServiceCollection();
        var jsonStrg = File.ReadAllText(configFilePath);
        var json = JsonSerializer.Deserialize<JsonElement>(jsonStrg);
        var config = json.GetProperty("ServerConfig");
        var connectionString = config.GetProperty("Test8ConnectionString").GetString() ?? "";
        var importConnectionString = config.GetProperty("ImportTest8ConnectionString").GetString() ?? "";
        var oldConnectionString = connectionString.Replace("dsstatstest", "dsstats");
        var mySqlImportDir = config.GetProperty("MySqlImportDir").GetString() ?? "unknown";

        services.AddOptions<DbImportOptions8>()
            .Configure(x =>
            {
                x.ImportConnectionString = importConnectionString ?? "";
                x.MySqlImportDir = mySqlImportDir;
            });

        services.AddDbContext<DsstatsContext>(options =>
        {
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), x =>
            {
                x.EnableRetryOnFailure();
                x.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                x.ExecutionStrategy(e => new NonRetryingExecutionStrategy(e));
                x.CommandTimeout(800);
                x.MigrationsAssembly("MySqlMigrations");
            });
        });

        services.AddDbContext<ReplayContext>(options =>
        {
            options.UseMySql(oldConnectionString, ServerVersion.AutoDetect(connectionString), x =>
            {
                x.EnableRetryOnFailure();
                x.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
        });

        services.AddLogging();
        services.AddMemoryCache();
        services.AddAutoMapper(typeof(DsstatsAutoMapperProfile));

        return services;
    }
}