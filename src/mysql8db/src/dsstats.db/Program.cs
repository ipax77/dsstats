using System.Text.Json;
using AutoMapper;
using dsstats.db8;
using dsstats.shared8;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dsstats.db;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var json = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText("/data/localserverconfig.json"));
        var config = json.GetProperty("ServerConfig");
        var connectionString = config.GetProperty("Dsstats8ConnectionString").GetString();
        var oldConnectionString = config.GetProperty("DsstatsConnectionString").GetString();
        var importConnectionString = config.GetProperty("Import8ConnectionString").GetString();
        var mySqlImportDir = config.GetProperty("MySqlImportDir").GetString() ?? "unknown";

        var services = new ServiceCollection();

        services.AddLogging(options =>
        {
            options.SetMinimumLevel(LogLevel.Information);
            options.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
            options.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            options.AddConsole();
        });

        services.AddDbContext<ReplayContext>(options =>
        {
            options.UseMySql(oldConnectionString, ServerVersion.AutoDetect(oldConnectionString), p =>
            {
                p.CommandTimeout(600);
                p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                // p.EnablePrimitiveCollectionsSupport();
            })
            //.EnableDetailedErrors()
            //.EnableSensitiveDataLogging()
            ;
        });

        services.AddDbContext<DsstatsContext>(options =>
        {
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), p =>
            {
                p.CommandTimeout(600);
                p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                // p.EnablePrimitiveCollectionsSupport();
            })
            //.EnableDetailedErrors()
            //.EnableSensitiveDataLogging()
            ;
        });

        services.AddOptions<DbImportOptions8>()
            .Configure(x =>
            {
                x.ImportConnectionString = importConnectionString ?? "";
                x.MySqlImportDir = mySqlImportDir;
            });

        services.AddAutoMapper(typeof(DsstatsAutoMapperProfile));

        var serviceProvider = services.BuildServiceProvider();

        var mapper = serviceProvider.GetRequiredService<IMapper>();
        mapper.ConfigurationProvider.AssertConfigurationIsValid();

        Test(serviceProvider);


        Console.WriteLine("Jon done.");
        Console.ReadLine();
    }

    private static void Test(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replay = context.Replays.OrderByDescending(o => o.GameTime).FirstOrDefault();
        if (replay != null)
        {
            Console.WriteLine(replay.GameTime);
        }
    }
}
