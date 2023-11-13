using System.Text.Json;
using dsstats.db8;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dsstats.ratings;

class Program
{
    static void Main(string[] args)
    {
        var services = new ServiceCollection();

        var jsonStrg = File.ReadAllText("/data/localserverconfig.json");
        var json = JsonSerializer.Deserialize<JsonElement>(jsonStrg);
        var config = json.GetProperty("ServerConfig");
        var importConnectionString = config.GetProperty("ImportConnectionString").GetString() ?? "";
        var mySqlConnectionString = config.GetProperty("DsstatsConnectionString").GetString();

        services.AddOptions<DbImportOptions>()
            .Configure(x =>
                {
                    x.ImportConnectionString = importConnectionString;
                    x.IsSqlite = false;
                });

        services.AddLogging(options =>
        {
            options.SetMinimumLevel(LogLevel.Information);
            options.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            options.AddConsole();
        });

        services.AddDbContext<ReplayContext>(options =>
        {
            options.UseMySql(mySqlConnectionString, ServerVersion.AutoDetect(mySqlConnectionString), p =>
            {
                p.CommandTimeout(600);
                p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
        });

        services.AddSingleton<RatingService>();
        services.AddSingleton<RatingsSaveService>();

        var serviceProvider = services.BuildServiceProvider();

        var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var ratingService = scope.ServiceProvider.GetRequiredService<RatingService>();
        var ratingsSaveService = scope.ServiceProvider.GetRequiredService<RatingsSaveService>();

        ratingService.CombineTest().Wait();

        // ratingService.ProduceRatings(RatingCalcType.Arcade, true).Wait();
        // var options = scope.ServiceProvider.GetRequiredService<IOptions<DbImportOptions>>();

        Console.WriteLine("done.");
        Console.ReadLine();
    }

}
