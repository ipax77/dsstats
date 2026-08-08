using dsstats.db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Text.Json;

namespace dsstats.migrations.mysql;

public sealed class MysqlDsstatsContextFactory : IDesignTimeDbContextFactory<DsstatsContext>
{
    public DsstatsContext CreateDbContext(string[] args)
    {
        var connectionString = GetConnectionString();
        var configuredVersion = Environment.GetEnvironmentVariable("DSS_MYSQL_SERVER_VERSION") ?? "9.7.0";
        if (!Version.TryParse(configuredVersion, out var parsedVersion))
        {
            throw new InvalidOperationException(
                $"DSS_MYSQL_SERVER_VERSION contains invalid version '{configuredVersion}'.");
        }

        var serverVersion = new MySqlServerVersion(parsedVersion);

        var optionsBuilder = new DbContextOptionsBuilder<DsstatsContext>();
        optionsBuilder.UseMySql(connectionString, serverVersion, options =>
        {
            options.EnableRetryOnFailure();
            options.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            options.CommandTimeout(800);
            options.MigrationsAssembly("dsstats.migrations.mysql");
        });

        return new DsstatsContext(optionsBuilder.Options);
    }

    private static string GetConnectionString()
    {
        var connectionStringFile = Environment.GetEnvironmentVariable("DSS_MIGRATION_CONNECTION_STRING_FILE");
        if (!string.IsNullOrWhiteSpace(connectionStringFile))
        {
            if (!File.Exists(connectionStringFile))
            {
                throw new InvalidOperationException(
                    $"Migration connection-string file does not exist: {connectionStringFile}");
            }

            var fileValue = File.ReadAllText(connectionStringFile).Trim();
            if (fileValue.Length == 0)
            {
                throw new InvalidOperationException("Migration connection-string file is empty.");
            }

            return fileValue;
        }

        var configurationFile = Environment.GetEnvironmentVariable("DSS_MIGRATION_CONFIG_FILE");
        if (!string.IsNullOrWhiteSpace(configurationFile))
        {
            if (!File.Exists(configurationFile))
            {
                throw new InvalidOperationException(
                    $"Migration configuration file does not exist: {configurationFile}");
            }

            using var document = JsonDocument.Parse(File.ReadAllText(configurationFile));
            if (document.RootElement.TryGetProperty("dsstats", out var dsstats) &&
                dsstats.TryGetProperty("ConnectionString", out var configuredConnection) &&
                !string.IsNullOrWhiteSpace(configuredConnection.GetString()))
            {
                return configuredConnection.GetString()!;
            }

            throw new InvalidOperationException(
                "Migration configuration does not contain dsstats:ConnectionString.");
        }

        return Environment.GetEnvironmentVariable("DSS_MIGRATION_CONNECTION_STRING")
            ?? "server=localhost;port=9801;database=dsstats10;user=unused;Password=unused";
    }
}

