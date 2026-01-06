using dsstats.db;
using dsstats.dbServices;
using dsstats.dbServices.Extensions;
using dsstats.ratings;
using dsstats.shared;
using dsstats.shared.Arcade;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace dsstats.tests;

[TestClass]
public class ArcadeMatchDb
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

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        context.Database.EnsureDeleted();
        context.Database.Migrate();

        return serviceProvider;
    }

    [TestMethod]
    public async Task FindSc2ArcadeMatches_ShouldFindMatches()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = serviceProvider.GetRequiredService<ILogger<RatingService>>();
            var importOptions = Options.Create(new ImportOptions());
            var ratingService = new RatingService(scopeFactory, importOptions, logger);
            var importService = new ImportService(scopeFactory, serviceProvider.GetRequiredService<ILogger<ImportService>>());

            // Ensure schema is created
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
                await context.Database.MigrateAsync();

                var replay = CreateReplay(1);
                var arcadeReplay = CreateArcadeReplay(1, replay.Gametime.AddMinutes(5));
                await importService.InsertReplays([replay]);
                await importService.ImportArcadeReplays([arcadeReplay]);
            }

            // Act
            await ratingService.FindSc2ArcadeMatches();

            // Assert
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
                var match = await context.ReplayArcadeMatches
                    .FirstOrDefaultAsync();

                Assert.IsNotNull(match);
                var matchReplay = await context.Replays
                    .FirstOrDefaultAsync(r => r.ReplayId == match.ReplayId);
                var matchArcadeReplay = await context.ArcadeReplays
                    .FirstOrDefaultAsync(ar => ar.ArcadeReplayId == match.ArcadeReplayId);

                Assert.IsNotNull(matchReplay);
                Assert.IsNotNull(matchArcadeReplay);

                // Assert on meaningful business data
                Assert.AreEqual(new DateTime(2021, 2, 1, 12, 0, 0), matchReplay.Gametime);
                Assert.AreEqual(6, matchArcadeReplay.PlayerCount);
            }
        }
        finally
        {
            connection.Dispose();
        }
    }

    [TestMethod]
    public async Task MatchNewDsstatsReplays_ShouldMatchNewlyImportedReplays()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = serviceProvider.GetRequiredService<ILogger<RatingService>>();
            var importOptions = Options.Create(new ImportOptions());
            var ratingService = new RatingService(scopeFactory, importOptions, logger);
            var importService = new ImportService(scopeFactory, serviceProvider.GetRequiredService<ILogger<ImportService>>());

            var checkpoint = DateTime.UtcNow;
            await Task.Delay(100); // Ensure timestamp difference

            // Arrange: Setup initial data
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
                await context.Database.MigrateAsync();

                // Import old arcade replays first (simulating nightly import history)
                var oldArcadeReplay1 = CreateArcadeReplay(1, new DateTime(2021, 2, 1, 12, 5, 0));
                var oldArcadeReplay2 = CreateArcadeReplay(2, new DateTime(2021, 3, 15, 14, 5, 0), startingPlayerId: 10);
                await importService.ImportArcadeReplays([oldArcadeReplay1, oldArcadeReplay2]);
            }

            await Task.Delay(100); // Ensure timestamp difference

            // Import new dsstats replays from a new uploader with historical games
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

                var newReplay1 = CreateReplay(1, startingPlayerId: 1); // Feb 1 - matches oldArcadeReplay1
                var newReplay2 = CreateReplay(2, startingPlayerId: 10); // March 15 - matches oldArcadeReplay2
                var newReplay3 = CreateReplay(3, startingPlayerId: 100); // No match
                newReplay2.Gametime = new DateTime(2021, 3, 15, 14, 0, 0);
                newReplay3.Gametime = new DateTime(2021, 4, 1, 10, 0, 0);

                await importService.InsertReplays([newReplay1, newReplay2, newReplay3]);
            }

            // Act: Match only newly imported dsstats replays
            await ratingService.MatchNewDsstatsReplays(checkpoint);

            // Assert
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
                var matches = await GetOrderdReplayMatches(context);

                // Should find 2 matches
                Assert.HasCount(2, matches);

                // Verify first match (Feb 1)
                var match1 = matches.FirstOrDefault(m => m.Replay.Gametime.Month == 2);
                Assert.IsNotNull(match1);
                Assert.AreEqual(new DateTime(2021, 2, 1, 12, 0, 0), match1.Replay.Gametime);
                Assert.AreEqual(new DateTime(2021, 2, 1, 12, 5, 0), match1.ArcadeReplay.CreatedAt);

                // Verify second match (March 15)
                var match2 = matches.FirstOrDefault(m => m.Replay.Gametime.Month == 3);
                Assert.IsNotNull(match2);
                Assert.AreEqual(new DateTime(2021, 3, 15, 14, 0, 0), match2.Replay.Gametime);
                Assert.AreEqual(new DateTime(2021, 3, 15, 14, 5, 0), match2.ArcadeReplay.CreatedAt);

                // Verify no match for April replay (no corresponding arcade replay)
                var unmatchedReplay = await context.Replays
                    .FirstOrDefaultAsync(r => r.Gametime.Month == 4);
                Assert.IsNotNull(unmatchedReplay);
                Assert.IsFalse(matches.Any(m => m.Match.ReplayId == unmatchedReplay.ReplayId));
            }
        }
        finally
        {
            connection.Dispose();
        }
    }

    [TestMethod]
    public async Task MatchWithNewArcadeReplays_ShouldRematchWhenLateArcadeReplaysArrive()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = serviceProvider.GetRequiredService<ILogger<RatingService>>();
            var importOptions = Options.Create(new ImportOptions());
            var ratingService = new RatingService(scopeFactory, importOptions, logger);
            var importService = new ImportService(scopeFactory, serviceProvider.GetRequiredService<ILogger<ImportService>>());

            // Arrange: Setup initial data with unmatched replays
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
                await context.Database.MigrateAsync();

                // Import dsstats replays that initially have no matches
                var replay1 = CreateReplay(1, startingPlayerId: 1);
                var replay2 = CreateReplay(2, startingPlayerId: 10);
                var replay3 = CreateReplay(3, startingPlayerId: 20);
                replay1.Gametime = new DateTime(2021, 2, 1, 12, 0, 0);
                replay2.Gametime = new DateTime(2021, 2, 2, 15, 0, 0);
                replay3.Gametime = new DateTime(2021, 2, 3, 18, 0, 0);

                await importService.InsertReplays([replay1, replay2, replay3]);

                // Import only one matching arcade replay (replay1 gets matched)
                var arcadeReplay1 = CreateArcadeReplay(1, new DateTime(2021, 2, 1, 12, 5, 0), startingPlayerId: 1);
                await importService.ImportArcadeReplays([arcadeReplay1]);
            }

            // Run initial matching
            await ratingService.FindSc2ArcadeMatches();

            // Verify initial state: only 1 match
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
                var initialMatches = await context.ReplayArcadeMatches.CountAsync();
                Assert.AreEqual(1, initialMatches);
            }

            //var checkpoint = DateTime.UtcNow;
            //await Task.Delay(100); // Ensure timestamp difference
            DateTime checkpoint;
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
                checkpoint = await context.ArcadeReplays
                    .Select(r => r.Imported)
                    .MaxAsync();
            }

            // Act: Simulate nightly import with late-arriving arcade replays
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

                // These arcade replays appear 1-2 days late (simulating the 6-day lookback window)
                var lateArcadeReplay2 = CreateArcadeReplay(2, new DateTime(2021, 2, 2, 15, 5, 0), startingPlayerId: 10);
                var lateArcadeReplay3 = CreateArcadeReplay(3, new DateTime(2021, 2, 3, 18, 5, 0), startingPlayerId: 20);
                await importService.ImportArcadeReplays([lateArcadeReplay2, lateArcadeReplay3]);
            }

            // Match with newly imported arcade replays
            await ratingService.MatchWithNewArcadeReplays(checkpoint);

            // Assert
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
                var matches = await GetOrderdReplayMatches(context);

                // Should now have 3 matches total
                Assert.HasCount(3, matches);

                // Verify all three matches exist with correct dates
                Assert.AreEqual(new DateTime(2021, 2, 1, 12, 0, 0), matches[0].Replay.Gametime);
                Assert.AreEqual(new DateTime(2021, 2, 2, 15, 0, 0), matches[1].Replay.Gametime);
                Assert.AreEqual(new DateTime(2021, 2, 3, 18, 0, 0), matches[2].Replay.Gametime);

                // Verify the new matches are with the late-arriving arcade replays
                Assert.AreEqual(new DateTime(2021, 2, 2, 15, 5, 0), matches[1].ArcadeReplay.CreatedAt);
                Assert.AreEqual(new DateTime(2021, 2, 3, 18, 5, 0), matches[2].ArcadeReplay.CreatedAt);
            }
        }
        finally
        {
            connection.Dispose();
        }
    }

    [TestMethod]
    public async Task ContinueFindSc2ArcadeMatches_ShouldRunBothStrategies()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = serviceProvider.GetRequiredService<ILogger<RatingService>>();
            var importOptions = Options.Create(new ImportOptions());
            var ratingService = new RatingService(scopeFactory, importOptions, logger);
            var importService = new ImportService(scopeFactory, serviceProvider.GetRequiredService<ILogger<ImportService>>());

            // Arrange: Initial state with some matches
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
                await context.Database.MigrateAsync();

                // Old matched replay
                var oldReplay = CreateReplay(1, startingPlayerId: 1);
                var oldArcadeReplay = CreateArcadeReplay(1, oldReplay.Gametime.AddMinutes(5), startingPlayerId: 1);
                await importService.InsertReplays([oldReplay]);
                await importService.ImportArcadeReplays([oldArcadeReplay]);
            }

            await ratingService.FindSc2ArcadeMatches();

            var checkpoint = DateTime.UtcNow;
            await Task.Delay(100);

            // Scenario 1: New dsstats replay from new uploader (historical)
            // Scenario 2: Late-arriving arcade replay for existing unmatched dsstats replay
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

                // Unmatched dsstats replay (no arcade replay yet)
                var unmatchedReplay = CreateReplay(2, startingPlayerId: 10);
                unmatchedReplay.Gametime = new DateTime(2021, 2, 5, 10, 0, 0);
                await importService.InsertReplays([unmatchedReplay]);

                // New dsstats replay from new uploader
                var newUploaderReplay = CreateReplay(3, startingPlayerId: 20);
                newUploaderReplay.Gametime = new DateTime(2021, 2, 10, 14, 0, 0);
                await importService.InsertReplays([newUploaderReplay]);

                // Arcade replay for the new uploader (already existed)
                var existingArcadeForNewUploader = CreateArcadeReplay(2, new DateTime(2021, 2, 10, 14, 5, 0), startingPlayerId: 20);
                // Late-arriving arcade replay
                var lateArcadeReplay = CreateArcadeReplay(3, new DateTime(2021, 2, 5, 10, 5, 0), startingPlayerId: 10);

                await importService.ImportArcadeReplays([existingArcadeForNewUploader, lateArcadeReplay]);
            }

            // Act: Run combined strategy
            await ratingService.ContinueFindSc2ArcadeMatches(checkpoint);

            // Assert
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
                var matches = await GetOrderdReplayMatches(context);

                // Should have 3 matches total (1 old + 2 new)
                Assert.HasCount(3, matches);

                // Verify the old match still exists
                var oldMatch = matches.FirstOrDefault(m => m.Replay.Gametime.Day == 1);
                Assert.IsNotNull(oldMatch);

                // Verify match for previously unmatched replay (late arcade replay)
                var lateMatch = matches.FirstOrDefault(m => m.Replay.Gametime.Day == 5);
                Assert.IsNotNull(lateMatch);
                Assert.AreEqual(new DateTime(2021, 2, 5, 10, 0, 0), lateMatch.Replay.Gametime);
                Assert.AreEqual(new DateTime(2021, 2, 5, 10, 5, 0), lateMatch.ArcadeReplay.CreatedAt);

                // Verify match for new uploader's replay
                var newUploaderMatch = matches.FirstOrDefault(m => m.Replay.Gametime.Day == 10);
                Assert.IsNotNull(newUploaderMatch);
                Assert.AreEqual(new DateTime(2021, 2, 10, 14, 0, 0), newUploaderMatch.Replay.Gametime);
                Assert.AreEqual(new DateTime(2021, 2, 10, 14, 5, 0), newUploaderMatch.ArcadeReplay.CreatedAt);
            }
        }
        finally
        {
            connection.Dispose();
        }
    }

    [TestMethod]
    public async Task MatchingMethods_ShouldNotReuseArcadeReplays()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = serviceProvider.GetRequiredService<ILogger<RatingService>>();
            var importOptions = Options.Create(new ImportOptions());
            var ratingService = new RatingService(scopeFactory, importOptions, logger);
            var importService = new ImportService(scopeFactory, serviceProvider.GetRequiredService<ILogger<ImportService>>());

            // Arrange: Two dsstats replays with same players, one arcade replay
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
                await context.Database.MigrateAsync();

                var replay1 = CreateReplay(1, startingPlayerId: 1);
                var replay2 = CreateReplay(2, startingPlayerId: 1); // Same players!
                replay1.Gametime = new DateTime(2021, 2, 1, 12, 0, 0);
                replay2.Gametime = new DateTime(2021, 2, 1, 12, 4, 0); // 4 minute later

                await importService.InsertReplays([replay1, replay2]);

                // Only one arcade replay that could match both
                var arcadeReplay = CreateArcadeReplay(1, new DateTime(2021, 2, 1, 12, 5, 0), startingPlayerId: 1);
                await importService.ImportArcadeReplays([arcadeReplay]);
            }

            // Act
            await ratingService.FindSc2ArcadeMatches();

            // Assert
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
                var matches = await context.ReplayArcadeMatches.ToListAsync();

                // Should only match one replay (whichever scores highest)
                Assert.HasCount(1, matches);

                // Verify one dsstats replay remains unmatched
                var unmatchedCount = await context.Replays
                    .Where(r => !context.ReplayArcadeMatches.Any(m => m.ReplayId == r.ReplayId))
                    .CountAsync();
                Assert.AreEqual(1, unmatchedCount);
            }
        }
        finally
        {
            connection.Dispose();
        }
    }

    [TestMethod]
    public async Task FullWorkflow_ShouldImportMatchAndRecalculateRatings()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            // --- ARRANGE ---
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = serviceProvider.GetRequiredService<ILogger<RatingService>>();
            var importOptions = Options.Create(new ImportOptions());
            var ratingService = new RatingService(scopeFactory, importOptions, logger);
            var importService = new ImportService(scopeFactory, serviceProvider.GetRequiredService<ILogger<ImportService>>());

            // Ensure schema is ready
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
                await context.Database.MigrateAsync();
            }

            // STEP 1: Initial upload (simulate old user replays)
            List<ReplayDto> replays = [
            CreateReplay(1, startingPlayerId: 1),
                CreateReplay(2, startingPlayerId: 10)
            ];

            replays[0].Gametime = new DateTime(2021, 2, 1, 12, 0, 0);
            replays[1].Gametime = new DateTime(2021, 2, 2, 12, 0, 0);

            await importService.InsertReplays(replays);

            // --- Simulate the NIGHTLY JOB ---
            var nightlyCheckpoint = DateTime.UtcNow;
            await Task.Delay(50);

            // Import new arcade replays
            List<ArcadeReplayDto> arcades = [
            CreateArcadeReplay(1, new DateTime(2021, 2, 1, 12, 5, 0), startingPlayerId: 1),
                CreateArcadeReplay(2, new DateTime(2021, 2, 2, 12, 5, 0), startingPlayerId: 10)
            ];

            await importService.ImportArcadeReplays(arcades);

            // Run nightly matching (Find or MatchNewDsstatsReplays + rating recalculation)
            await ratingService.MatchNewDsstatsReplays();
            var nightlyMatches = await GetOrderdReplayMatches(serviceProvider);
            Assert.HasCount(2, nightlyMatches, "Nightly job should create 2 replay-arcade matches.");

            // --- Simulate HOURLY JOB ---
            var hourlyCheckpoint = DateTime.UtcNow;
            await Task.Delay(50);

            // Add a new replay from a new uploader
            var replay3 = CreateReplay(3, startingPlayerId: 20);
            replay3.Gametime = new DateTime(2021, 2, 3, 12, 0, 0);
            await importService.InsertReplays([replay3]);

            // Run hourly incremental matching and recalculation
            await ratingService.MatchNewDsstatsReplays(hourlyCheckpoint);

            // --- ASSERT: verify that all matches and ratings are consistent ---
            var matchesAfterHourly = await GetOrderdReplayMatches(serviceProvider);
            Assert.HasCount(2, matchesAfterHourly, "No new arcade replays, match count should remain stable.");
        }
        finally
        {
            connection.Dispose();
        }
    }

    [TestMethod]
    public async Task FullWorkflow_ShouldMatchLateArrivingArcadeReplays()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = serviceProvider.GetRequiredService<ILogger<RatingService>>();
            var importOptions = Options.Create(new ImportOptions());
            var ratingService = new RatingService(scopeFactory, importOptions, logger);
            var importService = new ImportService(scopeFactory, serviceProvider.GetRequiredService<ILogger<ImportService>>());

            // --- ARRANGE: Ensure schema is ready ---
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
                await context.Database.MigrateAsync();
            }

            // STEP 1: Import DSStats replays FIRST (these will initially be unmatched)
            List<ReplayDto> replays = [
                CreateReplay(1, startingPlayerId: 1),
            CreateReplay(2, startingPlayerId: 10),
            CreateReplay(3, startingPlayerId: 20)
            ];

            replays[0].Gametime = new DateTime(2021, 2, 1, 12, 0, 0);
            replays[1].Gametime = new DateTime(2021, 2, 2, 15, 0, 0);
            replays[2].Gametime = new DateTime(2021, 2, 3, 18, 0, 0);

            await importService.InsertReplays(replays);

            // Run matching before arcade replays exist
            await ratingService.FindSc2ArcadeMatches();

            // Initially: no matches
            var initialMatches = await GetOrderdReplayMatches(serviceProvider);
            Assert.IsEmpty(initialMatches, "No matches should exist before arcade replays are imported.");

            // --- STEP 2: Later (next nightly job) new Arcade replays arrive ---
            var nightlyCheckpoint = DateTime.UtcNow;
            await Task.Delay(50); // Ensure timestamp separation

            List<ArcadeReplayDto> arcades = [
                CreateArcadeReplay(1, new DateTime(2021, 2, 1, 12, 5, 0), startingPlayerId: 1),
            CreateArcadeReplay(2, new DateTime(2021, 2, 2, 15, 5, 0), startingPlayerId: 10),
            CreateArcadeReplay(3, new DateTime(2021, 2, 3, 18, 5, 0), startingPlayerId: 20)
            ];

            await importService.ImportArcadeReplays(arcades);

            // --- STEP 3: Run nightly re-matching logic ---
            await ratingService.MatchWithNewArcadeReplays(nightlyCheckpoint);

            // --- ASSERT ---
            var matchesAfterNightly = await GetOrderdReplayMatches(serviceProvider);
            Assert.HasCount(3, matchesAfterNightly, "All previously unmatched replays should now be matched.");

            // Verify timestamps and associations
            foreach (var match in matchesAfterNightly)
            {
                var diff = (match.ArcadeReplay.CreatedAt - match.Replay.Gametime).TotalMinutes;
                Assert.IsTrue(diff > 0 && diff < 10, $"Expected replay {match.Replay.ReplayId} to be within 10 min of arcade creation.");
            }

            // Verify unique pairing (no reuse)
            Assert.HasCount(
                matchesAfterNightly.Select(m => m.Replay.ReplayId).Distinct().Count(),
                matchesAfterNightly, "Each replay should have a unique match.");

            Assert.HasCount(
                matchesAfterNightly.Select(m => m.ArcadeReplay.ArcadeReplayId).Distinct().Count(),
                matchesAfterNightly, "Each arcade replay should be matched only once.");
        }
        finally
        {
            connection.Dispose();
        }
    }


    private static async Task<List<ReplayMatchData>> GetOrderdReplayMatches(ServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        return await (from ram in context.ReplayArcadeMatches
                      join r in context.Replays on ram.ReplayId equals r.ReplayId
                      join ar in context.ArcadeReplays on ram.ArcadeReplayId equals ar.ArcadeReplayId
                      orderby r.Gametime
                      select new ReplayMatchData(ram, r, ar)).ToListAsync();
    }



    private static ReplayDto CreateReplay(int replayId, int startingPlayerId = 1, int startingReplayPlayerId = 1)
    {
        return new ReplayDto
        {
            Gametime = new DateTime(2021, 2, 1, 12, 0, 0),
            Duration = 500,
            GameMode = GameMode.Standard,
            WinnerTeam = 1,
            RegionId = 1,
            Players = new List<ReplayPlayerDto>
            {
                new() { TeamId = 1, Player = new() { Name = "Player1", ToonId = new() { Region = 1, Realm = 1, Id = startingPlayerId } } },
                new() { TeamId = 1, Player = new() { Name = "Player2", ToonId = new() { Region = 1, Realm = 1, Id = startingPlayerId + 1 } } },
                new() { TeamId = 1, Player = new() { Name = "Player3", ToonId = new() { Region = 1, Realm = 1, Id = startingPlayerId + 2 } } },
                new() { TeamId = 2, Player = new() { Name = "Player4", ToonId = new() { Region = 1, Realm = 1, Id = startingPlayerId + 3 } } },
                new() { TeamId = 2, Player = new() { Name = "Player5", ToonId = new() { Region = 1, Realm = 1, Id = startingPlayerId + 4 } } },
                new() { TeamId = 2, Player = new() { Name = "Player6", ToonId = new() { Region = 1, Realm = 1, Id = startingPlayerId + 5 } } },
            }
        };
    }

    private static ArcadeReplayDto CreateArcadeReplay(int arcadeReplayId, DateTime createdAt, int startingPlayerId = 1)
    {
        return new()
        {
            CreatedAt = createdAt,
            Duration = 500,
            GameMode = GameMode.Standard,
            PlayerCount = 6,
            WinnerTeam = 1,
            RegionId = 1,
            BnetBucketId = Random.Shared.Next(10_000_000),
            BnetRecordId = Random.Shared.Next(10_000_000),
            Players = new()
            {
                new() { Team = 1, Player = new() { Name = "Player1", ToonId = new() { Region = 1, Realm = 1, Id = startingPlayerId } } },
                new() { Team = 1, Player = new() { Name = "Player2", ToonId = new() { Region = 1, Realm = 1, Id = startingPlayerId + 1 } } },
                new() { Team = 1, Player = new() { Name = "Player3", ToonId = new() { Region = 1, Realm = 1, Id = startingPlayerId + 2 } } },
                new() { Team = 2, Player = new() { Name = "Player4", ToonId = new() { Region = 1, Realm = 1, Id = startingPlayerId + 3 } } },
                new() { Team = 2, Player = new() { Name = "Player5", ToonId = new() { Region = 1, Realm = 1, Id = startingPlayerId + 4 } } },
                new() { Team = 2, Player = new() { Name = "Player6", ToonId = new() { Region = 1, Realm = 1, Id = startingPlayerId + 5 } } },
            }
        };
    }

    private static async Task<List<ReplayMatchData>> GetOrderdReplayMatches(DsstatsContext context)
    {
        var query = from ram in context.ReplayArcadeMatches
                    join r in context.Replays on ram.ReplayId equals r.ReplayId
                    join ar in context.ArcadeReplays on ram.ArcadeReplayId equals ar.ArcadeReplayId
                    orderby r.Gametime
                    select new ReplayMatchData(ram, r, ar);
        return await query.ToListAsync();
    }

    private static double GetMatchScore(ReplayMatchData matchData)
    {
        var replayMatch = matchData.Replay.ToReplayMatchDto();
        var arcadeMatch = matchData.ArcadeReplay.ToReplayMatchDto();

        if (!RatingService.GetOrderedToonKey(replayMatch).Equals(RatingService.GetOrderedToonKey(arcadeMatch)))
        {

            return 0;
        }

        var score = RatingService.GetMatchScore(replayMatch, arcadeMatch);
        return score;
    }
}

internal sealed record ReplayMatchData(ReplayArcadeMatch Match, Replay Replay, ArcadeReplay ArcadeReplay);