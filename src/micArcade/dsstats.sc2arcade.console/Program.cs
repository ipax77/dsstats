using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Services.Ratings;
using pax.dsstats.shared;
using pax.dsstats.web.Server.Services.Arcade;

namespace dsstats.sc2arcade.console;
class Program
{
    static void Main(string[] args)
    {
        var services = new ServiceCollection();

        var serverVersion = new MySqlServerVersion(new Version(5, 7, 42));
        var jsonStrg = File.ReadAllText("/data/localserverconfig.json");
        var json = JsonSerializer.Deserialize<JsonElement>(jsonStrg);
        var config = json.GetProperty("ServerConfig");
        var connectionString = config.GetProperty("DsstatsConnectionString").GetString();
        var importConnectionString = config.GetProperty("ImportConnectionString").GetString() ?? "";

        services.AddOptions<DbImportOptions>()
            .Configure(x => x.ImportConnectionString = importConnectionString);

        services.AddLogging(options =>
        {
            options.SetMinimumLevel(LogLevel.Information);
            options.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
            options.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            options.AddConsole();
        });

        services.AddDbContext<ReplayContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion, p =>
            {
                p.CommandTimeout(600);
                p.MigrationsAssembly("MysqlMigrations");
                p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
        });

        services.AddHttpClient("sc2arcardeClient")
            .ConfigureHttpClient(options =>
            {
                options.BaseAddress = new Uri("https://api.sc2arcade.com");
                options.DefaultRequestHeaders.Add("Accept", "application/json");
            });

        services.AddAutoMapper(typeof(AutoMapperProfile));
        services.AddScoped<CrawlerService>();
        services.AddScoped<ArcadeRatingsService>();
        services.AddScoped<RatingsService>();

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();

        if (args.Length > 0 && args[0] == "dsratings")
        {
            Console.WriteLine($"producing dsstats ratings");
            var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();
            ratingsService.ProduceRatings(recalc: true).Wait();
        }
        else if (args.Length > 0 && args[0] == "ratings")
        {
            Console.WriteLine($"producing arcade ratings");
            var arcadeRatingsService = scope.ServiceProvider.GetRequiredService<ArcadeRatingsService>();
            arcadeRatingsService.ProduceRatings().Wait();
        }
        else if (args.Length > 0 && args[0] == "sethash")
        {
            // var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
            // context.Database.Migrate();

            Console.WriteLine($"setting arcadeReplays hash");
            var crawlerService = scope.ServiceProvider.GetRequiredService<CrawlerService>();
            crawlerService.SetReplaysHash();
        }
        else if (args.Length > 0 && args[0] == "fixplayerresult")
        {
            // var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
            // context.Database.Migrate();

            Console.WriteLine($"fixing player results");
            var crawlerService = scope.ServiceProvider.GetRequiredService<CrawlerService>();
            crawlerService.FixPlayerResults();
        }
        else if (args.Length > 0 && args[0] == "test")
        {
            // var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
            // context.Database.Migrate();

            Console.WriteLine($"testing players and replays");
            var crawlerService = scope.ServiceProvider.GetRequiredService<CrawlerService>();
            crawlerService.CheckPlayers().Wait();
            crawlerService.CheckReplays().Wait();
        } 
        else if (args.Length > 0 && args[0] == "fixnames")
        {
            // var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
            // context.Database.Migrate();

            Console.WriteLine($"fixing player names");
            var crawlerService = scope.ServiceProvider.GetRequiredService<CrawlerService>();
            crawlerService.FixPlayerNames().Wait();
        }                 
        else
        {
            var tillDate = new DateTime(2021, 02, 01);

            if (args.Length > 0 && int.TryParse(args[0], out int days))
            {
                tillDate = DateTime.Today.AddDays(days * -1);
            }
            var crawlerService = scope.ServiceProvider.GetRequiredService<CrawlerService>();
            Console.WriteLine($"Crawling lobby histories from today till {tillDate.ToShortDateString()}");
            crawlerService.GetLobbyHistory(tillDate).Wait();
        }
        Console.WriteLine($"job done.");
    }
}
