using dsstats.db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace dsstats.migrations.mysql;

public class DsstatsContextFactory : IDesignTimeDbContextFactory<DsstatsContext>
{
    public DsstatsContext CreateDbContext(string[] args)
    {
        var connectionString = "server=localhost;port=9801;database=dsstats10;user=pax;Password=dOdVIs8VHQbgweMu2kMR";
        var serverVersion = new MySqlServerVersion(new Version(8, 4, 6));

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
}

