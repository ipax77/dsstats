using System.Text.Json;
using Microsoft.EntityFrameworkCore.Design;
using dsstats.db;
using Microsoft.EntityFrameworkCore;

namespace MySqlMigrations;

public class DsstatsContextFactory : IDesignTimeDbContextFactory<DsstatsContext>
{
    public DsstatsContext CreateDbContext(string[] args)
    {
        var json = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText("/data/localserverconfig.json"));
        var config = json.GetProperty("ServerConfig");
        var connectionString = config.GetProperty("Dsstats8ConnectionString").GetString();

        var optionsBuilder = new DbContextOptionsBuilder<DsstatsContext>();
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), x =>
        {
            x.EnableRetryOnFailure();
            x.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            x.CommandTimeout(800);
            x.MigrationsAssembly("MySqlMigrations");
        });

        return new DsstatsContext(optionsBuilder.Options);
    }
}
