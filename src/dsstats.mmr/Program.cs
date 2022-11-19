using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using dsstats.raven;
using pax.dsstats.dbng;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;
using Raven.Client.Documents.Session;

namespace dsstats.mmr;

internal class Program
{
    static void Main(string[] args)
    {
        var json = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText("/data/localserverconfig.json"));
        var config = json.GetProperty("ServerConfig");
        var connectionString = config.GetProperty("DsstatsConnectionString").GetString();
        var serverVersion = new MySqlServerVersion(new System.Version(5, 0, 40));

        var services = new ServiceCollection();

        services.AddAutoMapper(typeof(AutoMapperProfile));
        services.AddSingleton<DocumentStoreHolder>();
        services.AddLogging(builder =>
            {
                builder.ClearProviders();
                // Clear Microsoft's default providers (like eventlogs and others)
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "yyyy-MM-dd hh:mm:ss ";
                }).SetMinimumLevel(LogLevel.Information);
            });
        services.AddDbContext<ReplayContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion, p =>
            {
                p.EnableRetryOnFailure();
                p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            })
            // .EnableDetailedErrors()
            // .EnableSensitiveDataLogging()
            ;
        });

        var serviceProvider = services.BuildServiceProvider();

        // END SERVICE CONFIG

        Stopwatch sw = Stopwatch.StartNew();

        Produce(serviceProvider);

        sw.Stop();
        Console.WriteLine($"jobs done in {sw.ElapsedMilliseconds} ms");
    }

    internal static void Request()
    {
        RatingsRequest ratingsRequest = new()
        {
            Skip = 20,
            Take = 40,
            // Search = "Feralan",
            Orders = new()
            {
                new()
                {
                    Property = "Games",
                    Ascending = true
                },

            }
        };

        var ratings = RavenService.GetPlayerRatings(ratingsRequest).GetAwaiter().GetResult();
    }

    internal static void CollectInital(int take)
    {
        using var session = DocumentStoreHolder.Store.OpenSession();

        var ratings = session.Query<PlayerRating>()
            .Statistics(out QueryStatistics stats)
            .OrderByDescending(o => o.Mmr)
            .Take(take)
            .ToList();

        Console.WriteLine($"got init data ({ratings.Count}|{stats.TotalResults}) in {stats.DurationInMs} ms");
    }

    internal static void Collect(int skip, int take)
    {
        using var session = DocumentStoreHolder.Store.OpenSession();

        Stopwatch sw = Stopwatch.StartNew();

        var ratings = session.Query<PlayerRating>()
            .OrderByDescending(o => o.Mmr)
            .Skip(skip)
            .Take(take)
            .ToList();

        sw.Stop();

        Console.WriteLine($"got data ({ratings.Count}) in {sw.ElapsedMilliseconds} ms");
    }

    internal static void Produce(IServiceProvider serviceProvider)
    {
        Stopwatch sw = Stopwatch.StartNew();

        RavenService.DeleteRatings().GetAwaiter().GetResult();
        sw.Stop();
        Console.WriteLine($"cleared data in {sw.ElapsedMilliseconds} ms");

        sw.Start();
        var data = MmrService.GetCmdrReplayDsRDtos(serviceProvider, DateTime.MinValue, DateTime.MinValue)
            .GetAwaiter().GetResult();
        sw.Stop();
        Console.WriteLine($"got data in {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        (var ratingResult, var changeResult) = MmrService.GeneratePlayerRatings(data);
        sw.Stop();

        Console.WriteLine($"calculated data in {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        RavenService.BulkInsert(ratingResult.Values.ToList()).GetAwaiter().GetResult();
        RavenService.BulkInsert(changeResult).GetAwaiter().GetResult();
        sw.Stop();

        Console.WriteLine($"data stored in {sw.ElapsedMilliseconds} ms");
    }
}


