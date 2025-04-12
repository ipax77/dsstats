using System.Text.Json;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using dsstats.db;
using dsstats.db.Services.Ratings;
using dsstats.db.Services.Stats;
using dsstats.db8;
using dsstats.db8.AutoMapper;
using dsstats.shared;
using dsstats.shared8;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dsstats.cli;

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

        services.AddAutoMapper(
            typeof(AutoMapperProfile),
            typeof(DsstatsAutoMapperProfile)
        );
        services.AddSingleton<RatingsService>();
        services.AddSingleton<dsstats.db.Services.Import.ImportService>();

        services.AddScoped<WinrateService>();

        var serviceProvider = services.BuildServiceProvider();

        var mapper = serviceProvider.GetRequiredService<IMapper>();
        mapper.ConfigurationProvider.AssertConfigurationIsValid();

        // Test(serviceProvider);
        // CalculateRatings(serviceProvider).Wait();
        // ImportOldReplays(serviceProvider).Wait();
        // SetOpponents(serviceProvider).Wait();
        // Test(serviceProvider).Wait();
        // ImportSc2ArcadeReplays(serviceProvider).Wait();
        CombineReplays(serviceProvider).Wait();

        Console.WriteLine("Jon done.");
        Console.ReadLine();
    }

    public static async Task CombineReplays(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateAsyncScope();
        var importService = scope.ServiceProvider.GetRequiredService<dsstats.db.Services.Import.ImportService>();
        await importService.CombineDsstatsSc2ArcadeReplays();
    }

    public static async Task Test(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateAsyncScope();
        var winrateService = scope.ServiceProvider.GetRequiredService<WinrateService>();

        var request = new StatsRequest()
        {
            TimePeriod = TimePeriod.All
        };

        var results = await winrateService.GetData(request, default);
        ArgumentNullException.ThrowIfNull(results);

        foreach (var result in results.OrderByDescending(o => o.AvgGain))
        {
            Console.WriteLine($"{result.Commander} => {result.AvgGain}");
        }
    }

    public static async Task ImportOldReplays(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateAsyncScope();
        var oldContext = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var importService = scope.ServiceProvider.GetRequiredService<dsstats.db.Services.Import.ImportService>();
        int skip = 510000;
        int take = 2500;

        while (true)
        {
            var replayDtos = await oldContext.Replays
                .OrderBy(o => o.ReplayId)
                .ProjectTo<ReplayDto>(mapper.ConfigurationProvider)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
            if (replayDtos.Count == 0)
            {
                break;
            }

            await importService.Import(replayDtos);

            skip += take;
            Console.WriteLine($"importing {skip}");
        }
    }

    public static async Task ImportSc2ArcadeReplays(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateAsyncScope();
        var oldContext = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var importService = scope.ServiceProvider.GetRequiredService<dsstats.db.Services.Import.ImportService>();
        int skip = 5_760_000;
        int take = 10000;

        while (true)
        {
            var replayDtos = await oldContext.ArcadeReplays
                .OrderBy(o => o.ArcadeReplayId)
                .ProjectTo<ArcadeReplayDto>(mapper.ConfigurationProvider)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
            if (replayDtos.Count == 0)
            {
                break;
            }

            await importService.ImportArcadeReplays(replayDtos);

            skip += take;
            Console.WriteLine($"importing {skip}");
        }
    }

    public static async Task SetOpponents(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        int skip = 0;
        int take = 5000;

        while (true)
        {
            var replays = await context.Replays
                .Include(i => i.ReplayPlayers)
                .OrderBy(o => o.ReplayId)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            if (replays.Count == 0)
            {
                break;
            }

            foreach (var replay in replays)
            {
                foreach (var replayPlayer in replay.ReplayPlayers)
                {
                    replayPlayer.Opponent = replayPlayer.GamePos switch
                    {
                        1 => replay.ReplayPlayers.FirstOrDefault(f => f.GamePos == 4),
                        2 => replay.ReplayPlayers.FirstOrDefault(f => f.GamePos == 5),
                        3 => replay.ReplayPlayers.FirstOrDefault(f => f.GamePos == 6),
                        4 => replay.ReplayPlayers.FirstOrDefault(f => f.GamePos == 1),
                        5 => replay.ReplayPlayers.FirstOrDefault(f => f.GamePos == 2),
                        6 => replay.ReplayPlayers.FirstOrDefault(f => f.GamePos == 3),
                        _ => null
                    };
                }
            }
            await context.SaveChangesAsync();
            skip += take;
            Console.WriteLine($"opp set {skip}");
        }
    }
}

