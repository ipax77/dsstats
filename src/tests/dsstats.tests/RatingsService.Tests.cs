
using dsstats.db;
using dsstats.ratings;
using dsstats.shared;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace dsstats.tests;

[TestClass]
public class RatingsServiceTests
{
    private ServiceProvider BuildServiceProvider(out SqliteConnection connection)
    {
        var services = new ServiceCollection();

        // One in-memory SQLite connection per test
        var localConnection = new SqliteConnection("Filename=:memory:");
        localConnection.Open();
        connection = localConnection;

        services.AddDbContext<DsstatsContext>(o => o.UseSqlite(localConnection, options =>
        {
            options.MigrationsAssembly("dsstats.migrations.sqlite");
        }));
        services.AddLogging();

        return services.BuildServiceProvider();
    }

    [TestMethod]
    public void CanCalculateAllRatings()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = serviceProvider.GetRequiredService<ILogger<RatingService>>();
            var importOptions = Options.Create(new ImportOptions());
            var ratingService = new RatingService(scopeFactory, importOptions, logger);

            var playerRatingsStore = new PlayerRatingsStore();
            var testReplay = GetTestReplay();
            var result = ratingService.ProcessReplay(testReplay, RatingType.All, playerRatingsStore);
            Assert.IsNotNull(result);
            Assert.AreEqual(0.5, result.ExpectedWinProbability);

            var p1Rating = result.ReplayPlayerRatings.FirstOrDefault(f => f.ReplayPlayerId == 1);
            var p2Rating = result.ReplayPlayerRatings.FirstOrDefault(f => f.ReplayPlayerId == 2);
            Assert.IsNotNull(p1Rating);
            Assert.IsNotNull(p2Rating);
            Assert.IsGreaterThan(0, p1Rating.RatingDelta);
            Assert.IsLessThan(0, p2Rating.RatingDelta);
        }
        finally
        {
            connection.Close();
        }
    }

    [TestMethod]
    public void CanCalculateLeaverRating()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = serviceProvider.GetRequiredService<ILogger<RatingService>>();
            var importOptions = Options.Create(new ImportOptions());
            var ratingService = new RatingService(scopeFactory, importOptions, logger);

            static PlayerRatingsStore GetInitialRatings()
            {
                var store = new PlayerRatingsStore();
                store.GetOrCreate(1, RatingType.All, 3000);
                store.GetOrCreate(3, RatingType.All, 3000);
                store.GetOrCreate(4, RatingType.All, 3000);
                return store;
            }

            var testReplay = GetTestReplay(1);
            testReplay.Players.AddRange([
                new() { ReplayPlayerId = 3, IsLeaver = false, IsMvp = false, Team = 1, Race = Commander.Terran, PlayerId = 3, },
                new() { ReplayPlayerId = 4, IsLeaver = false, IsMvp = false, Team = 1, Race = Commander.Terran, PlayerId = 4, },
                new() { ReplayPlayerId = 5, IsLeaver = false, IsMvp = false, Team = 2, Race = Commander.Terran, PlayerId = 5, },
                new() { ReplayPlayerId = 6, IsLeaver = false, IsMvp = false, Team = 2, Race = Commander.Terran, PlayerId = 6, }
            ]);

            // --- Step 1: Calculate ratings without a leaver ---
            var ratingsNoLeaverStore = GetInitialRatings();
            var resultNoLeaver = ratingService.ProcessReplay(testReplay, RatingType.All, ratingsNoLeaverStore);
            Assert.IsNotNull(resultNoLeaver);
            var winnerRatingNoLeaver = resultNoLeaver.ReplayPlayerRatings.FirstOrDefault(f => f.ReplayPlayerId == 4);
            var loserRatingNoLeaver = resultNoLeaver.ReplayPlayerRatings.FirstOrDefault(f => f.ReplayPlayerId == 5);
            Assert.IsNotNull(winnerRatingNoLeaver);
            Assert.IsNotNull(loserRatingNoLeaver);


            // --- Step 2: Calculate ratings with a leaver ---
            var leaverPlayer = testReplay.Players.First(p => p.PlayerId == 3);
            testReplay.Players.Remove(leaverPlayer);
            testReplay.Players.Add(new() { ReplayPlayerId = 3, IsLeaver = true, IsMvp = false, Team = 1, Race = Commander.Terran, PlayerId = 3, });
            leaverPlayer = testReplay.Players.First(p => p.PlayerId == 3);

            var ratingsWithLeaverStore = GetInitialRatings();
            var resultWithLeaver = ratingService.ProcessReplay(testReplay, RatingType.All, ratingsWithLeaverStore);
            Assert.IsNotNull(resultWithLeaver);

            var leaverRating = resultWithLeaver.ReplayPlayerRatings.FirstOrDefault(f => f.ReplayPlayerId == 3);
            var winnerRatingWithLeaver = resultWithLeaver.ReplayPlayerRatings.FirstOrDefault(f => f.ReplayPlayerId == 4);
            var loserRatingWithLeaver = resultWithLeaver.ReplayPlayerRatings.FirstOrDefault(f => f.ReplayPlayerId == 5);

            Assert.IsNotNull(leaverRating);
            Assert.IsNotNull(winnerRatingWithLeaver);
            Assert.IsNotNull(loserRatingWithLeaver);

            // --- Step 3: Assertions ---
            // Leaver gets a heavy penalty
            Assert.IsLessThan(-50, leaverRating.RatingDelta);

            // Other players get half the rating change
            const double tolerance = 0.01;
            Assert.AreEqual(winnerRatingNoLeaver.RatingDelta / 2, winnerRatingWithLeaver.RatingDelta, tolerance);
            Assert.AreEqual(loserRatingNoLeaver.RatingDelta / 2, loserRatingWithLeaver.RatingDelta, tolerance);
        }
        finally
        {
            connection.Close();
        }
    }

    [TestMethod]
    [DeploymentItem("testdata/calcdtos.json")]
    [DataRow("calcdtos.json")]
    public void CanCalculateReplays(string testData)
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = serviceProvider.GetRequiredService<ILogger<RatingService>>();
            var importOptions = Options.Create(new ImportOptions());
            var ratingService = new RatingService(scopeFactory, importOptions, logger);

            var playerRatingsStore = new PlayerRatingsStore();
            var testReplays = JsonSerializer.Deserialize<List<ReplayCalcDto>>(File.ReadAllText(testData));
            Assert.IsNotNull(testReplays);
            GetStats(testReplays);
            foreach (var testReplay in testReplays)
            {
                var ratingTypes = RatingService.GetRatingTypes(testReplay);
                foreach (var ratingType in ratingTypes)
                {
                    var result = ratingService.ProcessReplay(testReplay, ratingType, playerRatingsStore);
                }
            }
            GetPlayerStats(63397, playerRatingsStore.GetAll());
            double topRating = 0.0;
            foreach (var ent in playerRatingsStore.GetAll())
            {
                if (ent.Value.TryGetValue(RatingType.All, out var rating))
                {
                    if (rating.Rating > topRating)
                        topRating = rating.Rating;
                }
            }
            Assert.IsGreaterThan(2000, topRating);
        }
        finally
        {
            connection.Close();
        }
    }

    private static void GetPlayerStats(int playerId, Dictionary<int, Dictionary<RatingType, PlayerRatingCalcDto>> playerRatingsStore)
    {
        if (playerRatingsStore.TryGetValue(playerId, out var ratings))
        {
            foreach (var rating in ratings)
            {
                Console.WriteLine($"Player {playerId} - {rating.Key}: {rating.Value.Rating} ({rating.Value.Games} games)");
            }
        }
        else
        {
            Console.WriteLine($"No ratings found for player {playerId}");
        }
    }

    private static void GetStats(List<ReplayCalcDto> replays)
    {
        Dictionary<int, int> playerGamesDict = [];
        foreach (var replay in replays)
        {
            foreach (var player in replay.Players)
            {
                if (!playerGamesDict.ContainsKey(player.PlayerId))
                    playerGamesDict[player.PlayerId] = 0;
                playerGamesDict[player.PlayerId]++;
            }
        }
        foreach (var ent in playerGamesDict.OrderByDescending(o => o.Value).Take(5))
        {
            Console.WriteLine($"Player {ent.Key} has {ent.Value} games");
        }
    }

    private ReplayCalcDto GetTestReplay(int winnerTeam = 1)
    {
        return new()
        {
            ReplayId = 1,
            Gametime = DateTime.UtcNow,
            GameMode = GameMode.Standard,
            PlayerCount = 2,
            WinnerTeam = winnerTeam,
            TE = false,
            Players = [
                new() {
                    ReplayPlayerId = 1,
                    IsLeaver = false,
                    IsMvp = false,
                    Team = 1,
                    Race = Commander.Terran,
                    PlayerId = 1,
                },
                new() {
                    ReplayPlayerId = 2,
                    IsLeaver = false,
                    IsMvp = false,
                    Team = 2,
                    Race = Commander.Terran,
                    PlayerId = 2,
                },
            ]
        };
    }
}
