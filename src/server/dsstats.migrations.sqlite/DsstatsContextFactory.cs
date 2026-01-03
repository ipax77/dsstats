using dsstats.db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace dsstats.migrations.mysql;

public class DsstatsContextFactory : IDesignTimeDbContextFactory<DsstatsContext>
{
    public DsstatsContext CreateDbContext(string[] args)
    {
        var connectionString = "Data Source=/data/ds/dsstats.db";

        var optionsBuilder = new DbContextOptionsBuilder<DsstatsContext>();
        optionsBuilder.UseSqlite(connectionString, options =>
        {
            options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            options.CommandTimeout(800);
            options.MigrationsAssembly("dsstats.migrations.sqlite");
        });

        return new DsstatsContext(optionsBuilder.Options);
    }
}

