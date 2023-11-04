using dsstats.db8;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SqliteMigrations;

public class ReplayContextFactory : IDesignTimeDbContextFactory<ReplayContext>
{
    public ReplayContext CreateDbContext(string[] args)
    {
        var connectionString = "Data Source=/data/ds/dsstats.db";

        var optionsBuilder = new DbContextOptionsBuilder<ReplayContext>();
        optionsBuilder.UseSqlite(connectionString, x =>
        {
            x.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            x.MigrationsAssembly("SqliteMigrations");
        });

        return new ReplayContext(optionsBuilder.Options);
    }
}

//public class ReplayContextFactory : IDesignTimeDbContextFactory<ReplayContext>
//{
//    public ReplayContext CreateDbContext(string[] args)
//    {

//        var optionsBuilder = new DbContextOptionsBuilder<ReplayContext>();
//        optionsBuilder.UseSqlite("Data Source=/data/dsreplaystest.db", x =>
//        {
//            x.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
//            x.MigrationsAssembly("SqliteMigrations");
//        });

//        return new ReplayContext(optionsBuilder.Options);
//    }
//}