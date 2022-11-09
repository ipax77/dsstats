using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using pax.dsstats.dbng;

namespace SqliteMigrations;

public class ReplayContextFactory : IDesignTimeDbContextFactory<ReplayContext>
{
    public ReplayContext CreateDbContext(string[] args)
    {
        var connectionString = "Data Source=/data/dsreplaystest2.db";

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