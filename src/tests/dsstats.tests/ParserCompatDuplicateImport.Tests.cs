using dsstats.db;
using dsstats.dbServices;
using dsstats.shared;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dsstats.tests;

[TestClass]
public sealed class ParserCompatDuplicateImportTests
{
    [TestMethod]
    public async Task InsertReplays_ParserCompatDuplicateInBatch_KeepsLongestAndMergesUploader()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var importService = CreateImportService(serviceProvider);
            var shortReplay = CreateReplay(
                new DateTime(2021, 1, 1, 12, 2, 59, DateTimeKind.Utc),
                duration: 600,
                parserCompatHash: "parser-duplicate",
                uploaderGamePos: 2);
            var longReplay = CreateReplay(
                new DateTime(2021, 1, 1, 12, 3, 1, DateTimeKind.Utc),
                duration: 900,
                parserCompatHash: "parser-duplicate");

            await importService.InsertReplays([shortReplay, longReplay]);

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
            var replay = await context.Replays
                .Include(r => r.Players)
                .SingleAsync();

            Assert.AreEqual(900, replay.Duration);
            Assert.AreEqual(longReplay.ComputeHash(), replay.ReplayHash);
            Assert.IsTrue(replay.Players.Single(p => p.GamePos == 2).IsUploader);
        }
        finally
        {
            connection.Dispose();
        }
    }

    [TestMethod]
    public async Task InsertReplays_ExistingShorterParserCompatDuplicate_IsReplaced()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var importService = CreateImportService(serviceProvider);
            var existingShortReplay = CreateReplay(
                new DateTime(2021, 1, 1, 12, 2, 59, DateTimeKind.Utc),
                duration: 600,
                parserCompatHash: "parser-replace",
                uploaderGamePos: 1);
            var incomingLongReplay = CreateReplay(
                new DateTime(2021, 1, 1, 12, 3, 1, DateTimeKind.Utc),
                duration: 900,
                parserCompatHash: "parser-replace");

            await importService.InsertReplays([existingShortReplay]);
            await importService.InsertReplays([incomingLongReplay]);

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
            var replay = await context.Replays
                .Include(r => r.Players)
                .SingleAsync();

            Assert.AreEqual(900, replay.Duration);
            Assert.AreEqual(incomingLongReplay.ComputeHash(), replay.ReplayHash);
            Assert.IsTrue(replay.Players.Single(p => p.GamePos == 1).IsUploader);
        }
        finally
        {
            connection.Dispose();
        }
    }

    [TestMethod]
    public async Task InsertReplays_ExistingLongerParserCompatDuplicate_SavesUploaderUpdate()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var importService = CreateImportService(serviceProvider);
            var existingLongReplay = CreateReplay(
                new DateTime(2021, 1, 1, 12, 2, 59, DateTimeKind.Utc),
                duration: 900,
                parserCompatHash: "parser-existing-long");
            var incomingShortReplay = CreateReplay(
                new DateTime(2021, 1, 1, 12, 3, 1, DateTimeKind.Utc),
                duration: 600,
                parserCompatHash: "parser-existing-long",
                uploaderGamePos: 3);

            await importService.InsertReplays([existingLongReplay]);
            await importService.InsertReplays([incomingShortReplay]);

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
            var replay = await context.Replays
                .Include(r => r.Players)
                .SingleAsync();

            Assert.AreEqual(900, replay.Duration);
            Assert.AreEqual(existingLongReplay.ComputeHash(), replay.ReplayHash);
            Assert.IsTrue(replay.Players.Single(p => p.GamePos == 3).IsUploader);
        }
        finally
        {
            connection.Dispose();
        }
    }

    [TestMethod]
    public async Task InsertReplays_ParserCompatDuplicateOutsideWindow_IsNotMerged()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var importService = CreateImportService(serviceProvider);
            var firstReplay = CreateReplay(
                new DateTime(2021, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                duration: 600,
                parserCompatHash: "parser-outside-window");
            var secondReplay = CreateReplay(
                new DateTime(2021, 1, 1, 12, 4, 0, DateTimeKind.Utc),
                duration: 900,
                parserCompatHash: "parser-outside-window");

            await importService.InsertReplays([firstReplay, secondReplay]);

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

            Assert.AreEqual(2, await context.Replays.CountAsync());
        }
        finally
        {
            connection.Dispose();
        }
    }

    [TestMethod]
    public async Task InsertReplays_LegacyExactDuplicateWithoutParserHashes_StillKeepsLongest()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var importService = CreateImportService(serviceProvider);
            var shortReplay = CreateReplay(
                new DateTime(2021, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                duration: 600,
                parserCompatHash: string.Empty,
                playerCompatHashes: false,
                uploaderGamePos: 4);
            var longReplay = CreateReplay(
                new DateTime(2021, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                duration: 900,
                parserCompatHash: string.Empty,
                playerCompatHashes: false);

            await importService.InsertReplays([shortReplay, longReplay]);

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
            var replay = await context.Replays
                .Include(r => r.Players)
                .SingleAsync();

            Assert.AreEqual(900, replay.Duration);
            Assert.IsNull(replay.ParserCompatHash);
            Assert.IsTrue(replay.Players.Single(p => p.GamePos == 4).IsUploader);
        }
        finally
        {
            connection.Dispose();
        }
    }

    private static ServiceProvider BuildServiceProvider(out SqliteConnection connection)
    {
        var services = new ServiceCollection();

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

    private static ImportService CreateImportService(ServiceProvider serviceProvider)
    {
        return new(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            serviceProvider.GetRequiredService<ILogger<ImportService>>());
    }

    private static ReplayDto CreateReplay(
        DateTime gametime,
        int duration,
        string parserCompatHash,
        bool playerCompatHashes = true,
        int? uploaderGamePos = null)
    {
        return new()
        {
            Title = "Direct Strike",
            Version = "5.0.14",
            GameMode = GameMode.Standard,
            RegionId = 1,
            Gametime = gametime,
            Duration = duration,
            WinnerTeam = 1,
            CompatHash = parserCompatHash,
            Players =
            [
                CreatePlayer(1, duration, playerCompatHashes, uploaderGamePos),
                CreatePlayer(2, duration, playerCompatHashes, uploaderGamePos),
                CreatePlayer(3, duration, playerCompatHashes, uploaderGamePos),
                CreatePlayer(4, duration, playerCompatHashes, uploaderGamePos)
            ]
        };
    }

    private static ReplayPlayerDto CreatePlayer(int gamePos, int duration, bool playerCompatHashes, int? uploaderGamePos)
    {
        return new()
        {
            CompatHash = playerCompatHashes ? $"ds-player-compat-v1-player-{gamePos}-stable-value" : null,
            Name = $"Player{gamePos}",
            Race = Commander.Terran,
            SelectedRace = Commander.Terran,
            GamePos = gamePos,
            TeamId = gamePos <= 2 ? 1 : 2,
            Result = gamePos <= 2 ? PlayerResult.Win : PlayerResult.Los,
            Duration = duration,
            IsUploader = uploaderGamePos == gamePos,
            Player = new()
            {
                Name = $"Player{gamePos}",
                ToonId = new()
                {
                    Region = 1,
                    Realm = 1,
                    Id = gamePos
                }
            }
        };
    }
}
