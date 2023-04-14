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

        var serverVersion = new MySqlServerVersion(new Version(5, 7, 41));
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
                p.CommandTimeout(120);
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

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();

        var tillDate = new DateTime(2021, 02, 01);

        if (args.Length > 0 && args[0] == "ratings")
        {
            Console.WriteLine($"producing arcade ratings");
            var arcadeRatingsService = scope.ServiceProvider.GetRequiredService<ArcadeRatingsService>();
            arcadeRatingsService.ProduceRatings().Wait();
        }
        else
        {
            if (args.Length > 0 && int.TryParse(args[0], out int days))
            {
                tillDate = DateTime.Today.AddDays(days * -1);
            }
            var crawlerService = scope.ServiceProvider.GetRequiredService<CrawlerService>();
            Console.WriteLine($"Crawling lobby histories from today till {tillDate.ToShortDateString()}");
            crawlerService.GetLobbyHistory(tillDate).Wait();
            Console.WriteLine($"jon done.");
        }
    }
}
