using dsstats.migrations.mysql;
using Microsoft.EntityFrameworkCore;

namespace dsstats.tests;

[TestClass]
[DoNotParallelize]
public sealed class MigrationContextFactoryTests
{
    [TestMethod]
    public void CreateDbContext_ReadsConnectionStringFromProtectedConfiguration()
    {
        const string expected = "server=mysql8;database=dsstats10;user=test;Password=test";
        var configPath = Path.GetTempFileName();
        var previousConfig = Environment.GetEnvironmentVariable("DSS_MIGRATION_CONFIG_FILE");
        var previousConnection = Environment.GetEnvironmentVariable("DSS_MIGRATION_CONNECTION_STRING");
        var previousConnectionFile = Environment.GetEnvironmentVariable("DSS_MIGRATION_CONNECTION_STRING_FILE");
        var previousVersion = Environment.GetEnvironmentVariable("DSS_MYSQL_SERVER_VERSION");

        try
        {
            File.WriteAllText(configPath, $$"""
                {
                  "dsstats": {
                    "ConnectionString": "{{expected}}"
                  }
                }
                """);
            Environment.SetEnvironmentVariable("DSS_MIGRATION_CONFIG_FILE", configPath);
            Environment.SetEnvironmentVariable("DSS_MIGRATION_CONNECTION_STRING", null);
            Environment.SetEnvironmentVariable("DSS_MIGRATION_CONNECTION_STRING_FILE", null);
            Environment.SetEnvironmentVariable("DSS_MYSQL_SERVER_VERSION", "9.7.0");

            using var context = new MysqlDsstatsContextFactory().CreateDbContext([]);

            var actual = context.Database.GetConnectionString();
            StringAssert.Contains(actual, "Server=mysql8");
            StringAssert.Contains(actual, "Database=dsstats10");
            StringAssert.Contains(actual, "User ID=test");
        }
        finally
        {
            Environment.SetEnvironmentVariable("DSS_MIGRATION_CONFIG_FILE", previousConfig);
            Environment.SetEnvironmentVariable("DSS_MIGRATION_CONNECTION_STRING", previousConnection);
            Environment.SetEnvironmentVariable("DSS_MIGRATION_CONNECTION_STRING_FILE", previousConnectionFile);
            Environment.SetEnvironmentVariable("DSS_MYSQL_SERVER_VERSION", previousVersion);
            File.Delete(configPath);
        }
    }

    [TestMethod]
    public void CreateDbContext_RejectsInvalidServerVersion()
    {
        var previousVersion = Environment.GetEnvironmentVariable("DSS_MYSQL_SERVER_VERSION");
        try
        {
            Environment.SetEnvironmentVariable("DSS_MYSQL_SERVER_VERSION", "not-a-version");

            var exception = Assert.ThrowsExactly<InvalidOperationException>(
                () => new MysqlDsstatsContextFactory().CreateDbContext([]));

            StringAssert.Contains(exception.Message, "DSS_MYSQL_SERVER_VERSION");
        }
        finally
        {
            Environment.SetEnvironmentVariable("DSS_MYSQL_SERVER_VERSION", previousVersion);
        }
    }
}
