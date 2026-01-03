using dsstats.db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace dsstats.migrations.postgresql;

public class DsstatsContextFactory : IDesignTimeDbContextFactory<DsstatsContext>
{
    public DsstatsContext CreateDbContext(string[] args)
    {
        var connectionString = "Host=localhost;Port=5432;Database=dsstatsdb;Username=dsstats;Password=dsstats_pass";

        var optionsBuilder = new DbContextOptionsBuilder<DsstatsContext>();
        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.EnableRetryOnFailure();
            options.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            options.CommandTimeout(800);
            options.MigrationsAssembly("dsstats.migrations.postgresql");
        });

        return new DsstatsContext(optionsBuilder.Options);
    }
}

