using dsstats.db8;
using dsstats.db8services;
using dsstats.db8services.Import;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.web.Server.Services.Arcade;
using System.Text.Json;

namespace SC2ArcadeCrawler;

class Program
{
    static void Main(string[] args)
    {
        var services = new ServiceCollection();

        var jsonStrg = File.ReadAllText("/data/localserverconfig.json");
        var json = JsonSerializer.Deserialize<JsonElement>(jsonStrg);
        var config = json.GetProperty("ServerConfig");
        // var connectionString = config.GetProperty("MariaDbImportConnectionString").GetString();
        // var importConnectionString = config.GetProperty("MariaDbImportConnectionString").GetString() ?? "";
        var connectionString = config.GetProperty("DsstatsConnectionString").GetString();
        var importConnectionString = config.GetProperty("ImportConnectionString").GetString() ?? "";


        services.AddOptions<DbImportOptions>()
            .Configure(x =>
                {
                    x.ImportConnectionString = importConnectionString;
                    x.IsSqlite = false;
                });

        services.AddLogging(options =>
        {
            options.SetMinimumLevel(LogLevel.Information);
            options.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
            options.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            options.AddConsole();
        });

        services.AddDbContext<ReplayContext>(options =>
        {
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), p =>
            {
                p.CommandTimeout(600);
                p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
        });

        services.AddHttpClient("sc2arcardeClient")
            .ConfigureHttpClient(options =>
            {
                options.BaseAddress = new Uri("https://api.sc2arcade.com");
                options.DefaultRequestHeaders.Add("Accept", "application/json");
            });
        services.AddSingleton<IRemoteToggleService, RemoteToggleService>();
        services.AddSingleton<IImportService, ImportService>();
        services.AddSingleton<CrawlerService>();

        var serviceProvider = services.BuildServiceProvider();

        var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var crawlerService = scope.ServiceProvider.GetRequiredService<CrawlerService>();

        crawlerService.GetLobbyHistory(DateTime.Today.AddDays(-1), default).Wait();
        // crawlerService.GetLobbyHistory(new DateTime(2021, 2, 1), default).Wait();

        Console.WriteLine("done.");
        Console.ReadLine();
    }
}
