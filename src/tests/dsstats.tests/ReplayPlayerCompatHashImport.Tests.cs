using dsstats.db;
using dsstats.dbServices;
using dsstats.shared;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace dsstats.tests;

[TestClass]
public sealed class ReplayPlayerCompatHashImportTests
{
    [TestMethod]
    public async Task InsertReplays_NormalizesReplayPlayerCompatHashes()
    {
        const string rawCompatHash = "ds-player-compat-v1-test-raw-value-that-is-longer-than-the-db-column";
        const string existingSha256 = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";

        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var importService = new ImportService(
                scopeFactory,
                serviceProvider.GetRequiredService<ILogger<ImportService>>());

            await importService.InsertReplays([CreateReplay(rawCompatHash, existingSha256)]);

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
            var replayPlayers = await context.ReplayPlayers
                .OrderBy(o => o.GamePos)
                .ToListAsync();

            Assert.AreEqual(4, replayPlayers.Count);
            Assert.AreEqual(ComputeSha256(rawCompatHash), replayPlayers[0].CompatHash);
            Assert.AreEqual(existingSha256, replayPlayers[1].CompatHash);
            Assert.IsNull(replayPlayers[2].CompatHash);
            Assert.AreEqual(string.Empty, replayPlayers[3].CompatHash);
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

    private static ReplayDto CreateReplay(string rawCompatHash, string existingSha256)
    {
        return new()
        {
            Title = "Direct Strike",
            Version = "5.0.14",
            GameMode = GameMode.Standard,
            RegionId = 1,
            Gametime = new DateTime(2021, 2, 1, 12, 0, 0, DateTimeKind.Utc),
            Duration = 900,
            WinnerTeam = 1,
            Players =
            [
                CreatePlayer(1, rawCompatHash),
                CreatePlayer(2, existingSha256),
                CreatePlayer(3, null),
                CreatePlayer(4, string.Empty)
            ]
        };
    }

    private static ReplayPlayerDto CreatePlayer(int gamePos, string? compatHash)
    {
        return new()
        {
            CompatHash = compatHash,
            Name = $"Player{gamePos}",
            Race = Commander.Terran,
            SelectedRace = Commander.Terran,
            GamePos = gamePos,
            TeamId = gamePos <= 2 ? 1 : 2,
            Result = gamePos <= 2 ? PlayerResult.Win : PlayerResult.Los,
            Duration = 900,
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

    private static string ComputeSha256(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
