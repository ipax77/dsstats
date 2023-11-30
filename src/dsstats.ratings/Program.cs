using System.Diagnostics;
using System.Text.Json;
using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Interfaces;
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

        services.AddSingleton<IRatingService, RatingService>();
        services.AddSingleton<IRatingsSaveService, RatingsSaveService>();

        var serviceProvider = services.BuildServiceProvider();

        var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("ratings start.");

        Stopwatch sw = Stopwatch.StartNew();
        if (args.Length == 1)
        {
            var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();
            if (args[0] == "dsstats")
            {
                logger.LogInformation("producing dsstats ratings.");
                ratingService.ProduceRatings(RatingCalcType.Dsstats, true).Wait();
            }
            else if (args[0] == "sc2arcade")
            {
                logger.LogInformation("producing sc2arcade ratings.");
                ratingService.ProduceRatings(RatingCalcType.Arcade, true).Wait();
            }
            else if (args[0] == "combo")
            {
                logger.LogInformation("producing combo ratings.");
                ratingService.ProduceRatings(RatingCalcType.Combo, true).Wait();
            }
            else if (args[0] == "combo2")
            {
                logger.LogInformation("producing combo2 ratings.");
                ratingService.CombineTest().Wait();
            }
            else
            {
                logger.LogError("allowed parameters: dsstats|sc2arcade|combo");
                return;
            }
        }
        else if (args.Length == 2)
        {
            if (args[0] == "delete")
            {
                var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
                DeleteReplay(args[1], context, logger);
            }
            else
            {
                logger.LogError("allowed parameters: delete <replayHash>");
                return;
            }
        }
        else
        {
            logger.LogError("allowed parameters: dsstats|sc2arcade|combo");
            return;
        }

        sw.Stop();
        logger.LogWarning("job done in {ms} ms ({min} min)", sw.ElapsedMilliseconds, Math.Round(sw.Elapsed.TotalMinutes, 2));
    }

    private static void DeleteReplay(string replayHash, ReplayContext context, ILogger<Program> logger)
    {
        try
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var replay = context.Replays
                .Include(i => i.ReplayPlayers)
                    .ThenInclude(i => i.Spawns)
                        .ThenInclude(i => i.Units)
                .Include(i => i.ReplayRatingInfo)
                    .ThenInclude(i => i.RepPlayerRatings)
                .Include(i => i.ComboReplayRating)
                .Include(i => i.ReplayPlayers)
                    .ThenInclude(i => i.ComboReplayPlayerRating)
                .FirstOrDefault(f => f.ReplayHash == replayHash);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            if (replay is not null)
            {
                context.Replays.Remove(replay);
                context.SaveChanges();
                logger.LogWarning("replay {hash} removed.", replayHash);
            }
            else
            {
                logger.LogWarning("replay {hash} not found.", replayHash);
            }
        }
        catch (Exception ex)
        {
            logger.LogError("failed removing replay {hash}: {error}", replayHash, ex.Message);
        }
    }
}
