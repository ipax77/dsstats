﻿using dsstats.db8;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Text.Json;

namespace MysqlMigrations;

public class ReplayContextFactory : IDesignTimeDbContextFactory<ReplayContext>
{
    public ReplayContext CreateDbContext(string[] args)
    {
        var json = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText("/data/localserverconfig.json"));
        var config = json.GetProperty("ServerConfig");
        var connectionString = config.GetProperty("DsstatsConnectionString").GetString();
        // var connectionString = config.GetProperty("ProdConnectionString").GetString();
        var serverVersion = new MySqlServerVersion(new System.Version(5, 7, 44));

        var optionsBuilder = new DbContextOptionsBuilder<ReplayContext>();
        optionsBuilder.UseMySql(connectionString, serverVersion, x =>
        {
            x.CommandTimeout(300);
            x.EnableRetryOnFailure();
            x.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            x.MigrationsAssembly("MysqlMigrations");
        });

        return new ReplayContext(optionsBuilder.Options);
    }
}

