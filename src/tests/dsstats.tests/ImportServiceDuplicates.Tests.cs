using dsstats.api.Services;
using dsstats.db;
using dsstats.dbServices;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace dsstats.tests;

[TestClass]
public sealed class ImportServiceDuplicatesTests
{
    [TestMethod]
    public async Task LongerReplayReplacementDeletesOldUpgradesAndPreservesOtherReplays()
    {
        await using var fixture = await Fixture.Create();
        var original = CreateReplay("Direct Strike", 800, 100);
        var unrelated = CreateReplay("Another match", 900, 300, dayOffset: 1);
        await fixture.Importer.InsertReplays([original, unrelated]);
        var oldUpgrades = await fixture.Upgrades(original.ComputeHash());
        var unrelatedUpgrades = await fixture.Upgrades(unrelated.ComputeHash());

        var replacement = CreateReplay("Direct Strike", 900, 200);
        await fixture.Importer.InsertReplays([replacement]);

        await fixture.AssertDeleted(oldUpgrades);
        CollectionAssert.AreEqual(unrelatedUpgrades, await fixture.Upgrades(unrelated.ComputeHash()));
        var replacementUpgrades = await fixture.Upgrades(replacement.ComputeHash());
        CollectionAssert.AreEquivalent(new[] { 201, 202, 203, 204, 301, 302, 303, 304 },
            replacementUpgrades.Select(x => x.Gameloop).ToArray());
        await fixture.AssertIntact(replays: 2, upgrades: 16);
    }

    [TestMethod]
    public async Task CandidateDeduplicationDeletesOnlyDiscardedReplaysUpgrades()
    {
        await using var fixture = await Fixture.Create();
        var keeper = CreateReplay("Keeper", 900, 200);
        var duplicate = CreateReplay("Duplicate", 800, 100);
        var unrelated = CreateReplay("Another match", 900, 300, dayOffset: 1);
        await fixture.Importer.InsertReplays([keeper, duplicate, unrelated]);
        var keeperUpgrades = await fixture.Upgrades(keeper.ComputeHash());
        var duplicateUpgrades = await fixture.Upgrades(duplicate.ComputeHash());
        var unrelatedUpgrades = await fixture.Upgrades(unrelated.ComputeHash());

        await fixture.Importer.CheckDuplicateCandidates();
        await fixture.Importer.CheckDuplicateCandidates();

        await fixture.AssertDeleted(duplicateUpgrades);
        CollectionAssert.AreEqual(keeperUpgrades, await fixture.Upgrades(keeper.ComputeHash()));
        CollectionAssert.AreEqual(unrelatedUpgrades, await fixture.Upgrades(unrelated.ComputeHash()));
        await fixture.AssertIntact(replays: 2, upgrades: 16);
    }

    [TestMethod]
    [DataRow(800)]
    [DataRow(900)]
    public async Task IgnoredDuplicatePreservesStoredUpgrades(int incomingDuration)
    {
        await using var fixture = await Fixture.Create();
        var keeper = CreateReplay("Direct Strike", 900, 200);
        await fixture.Importer.InsertReplays([keeper]);
        var keeperUpgrades = await fixture.Upgrades(keeper.ComputeHash());

        await fixture.Importer.InsertReplays([CreateReplay("Direct Strike", incomingDuration, 100)]);

        CollectionAssert.AreEqual(keeperUpgrades, await fixture.Upgrades(keeper.ComputeHash()));
        await fixture.AssertIntact(replays: 1, upgrades: 8);
    }

    [TestMethod]
    public async Task HashTransitionDeduplicationDeletesDiscardedUpgrades()
    {
        await using var fixture = await Fixture.Create();
        var keeper = CreateReplay(string.Empty, 900, 200);
        var duplicate = CreateReplay("Legacy title", 800, 100);
        await fixture.Importer.InsertReplays([keeper, duplicate]);
        var keeperUpgrades = await fixture.Upgrades(keeper.ComputeHash());
        var duplicateUpgrades = await fixture.Upgrades(duplicate.ComputeHash());
        var transition = new TransitionService(fixture.Factory, NullLogger<TransitionService>.Instance);

        await transition.FixHashes();

        await fixture.AssertDeleted(duplicateUpgrades);
        // Hash repair can also rename the keeper's hash; its owned rows must remain unchanged.
        await using var context = await fixture.Factory.CreateDbContextAsync();
        var survivor = await context.Replays.SingleAsync();
        Assert.AreEqual(900, survivor.Duration);
        CollectionAssert.AreEqual(keeperUpgrades, await fixture.Upgrades(survivor.ReplayHash));
        await fixture.AssertIntact(replays: 1, upgrades: 8);
    }

    [TestMethod]
    public async Task FailedReplayDeletionRollsBackUpgradeDeletion()
    {
        await using var fixture = await Fixture.Create();
        var keeper = CreateReplay("Keeper", 900, 200);
        var duplicate = CreateReplay("Duplicate", 800, 100);
        await fixture.Importer.InsertReplays([keeper, duplicate]);
        var duplicateUpgrades = await fixture.Upgrades(duplicate.ComputeHash());
        await using (var context = await fixture.Factory.CreateDbContextAsync())
        {
            await context.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER BlockReplayDeletion BEFORE DELETE ON Replays
                BEGIN SELECT RAISE(ABORT, 'Deletion blocked for rollback test'); END;
                """);
        }

        await Assert.ThrowsExactlyAsync<DbUpdateException>(() => fixture.Importer.CheckDuplicateCandidates());

        CollectionAssert.AreEqual(duplicateUpgrades, await fixture.Upgrades(duplicate.ComputeHash()));
        await fixture.AssertIntact(replays: 2, upgrades: 16);
    }

    private sealed record UpgradeSnapshot(int Id, int? ReplayPlayerId, int UpgradeId, int Gameloop);

    private sealed class Fixture(SqliteConnection connection, ServiceProvider services) : IAsyncDisposable
    {
        public IDbContextFactory<DsstatsContext> Factory { get; } = services.GetRequiredService<IDbContextFactory<DsstatsContext>>();
        public IImportService Importer { get; } = services.GetRequiredService<IImportService>();

        public static async Task<Fixture> Create()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            await connection.OpenAsync();
            var services = new ServiceCollection().AddLogging()
                .AddDbContextFactory<DsstatsContext>(o => o.UseSqlite(connection,
                    options => options.MigrationsAssembly("dsstats.migrations.sqlite")))
                .AddSingleton(Mock.Of<IRatingService>())
                .AddSingleton<IImportService, ImportService>()
                .BuildServiceProvider();
            var fixture = new Fixture(connection, services);
            await using var context = await fixture.Factory.CreateDbContextAsync();
            await context.Database.MigrateAsync();
            return fixture;
        }

        public async Task<UpgradeSnapshot[]> Upgrades(string replayHash)
        {
            await using var context = await Factory.CreateDbContextAsync();
            var upgrades = await context.ReplayPlayers.Where(p => p.Replay!.ReplayHash == replayHash)
                .SelectMany(p => p.Upgrades).OrderBy(u => u.PlayerUpgradeId)
                .Select(u => new UpgradeSnapshot(u.PlayerUpgradeId, EF.Property<int?>(u, "ReplayPlayerId"), u.UpgradeId, u.Gameloop))
                .ToArrayAsync();
            Assert.HasCount(8, upgrades);
            return upgrades;
        }

        public async Task AssertDeleted(UpgradeSnapshot[] deleted)
        {
            await using var context = await Factory.CreateDbContextAsync();
            var ids = deleted.Select(x => x.Id).ToArray();
            Assert.AreEqual(0, await context.PlayerUpgrades.CountAsync(u => ids.Contains(u.PlayerUpgradeId)),
                "Discarded upgrades must be deleted, not detached from their replay players.");
        }

        public async Task AssertIntact(int replays, int upgrades)
        {
            await using var context = await Factory.CreateDbContextAsync();
            Assert.AreEqual(replays, await context.Replays.CountAsync());
            Assert.AreEqual(replays * 4, await context.ReplayPlayers.CountAsync());
            Assert.AreEqual(upgrades, await context.PlayerUpgrades.CountAsync());
            Assert.AreEqual(0, await context.PlayerUpgrades.CountAsync(u =>
                !context.ReplayPlayers.Any(p => p.ReplayPlayerId == EF.Property<int?>(u, "ReplayPlayerId"))),
                "Every surviving upgrade must still belong to a replay player.");
            Assert.AreEqual(2, await context.Upgrades.CountAsync(), "Shared upgrade definitions must survive deduplication.");
        }

        public async ValueTask DisposeAsync()
        {
            await services.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private static ReplayDto CreateReplay(string title, int duration, int upgradeGameloop, int dayOffset = 0) => new()
    {
        Title = title,
        Version = "5.0.14",
        GameMode = GameMode.Standard,
        RegionId = 1,
        Gametime = new DateTime(2026, 5, 23, 12, 0, 0, DateTimeKind.Utc).AddDays(dayOffset),
        Duration = duration,
        WinnerTeam = 1,
        Players = Enumerable.Range(1, 4).Select(pos => new ReplayPlayerDto
        {
            Name = $"Player{pos}",
            Race = Commander.Terran,
            SelectedRace = Commander.Terran,
            GamePos = pos,
            TeamId = pos <= 2 ? 1 : 2,
            Result = pos <= 2 ? PlayerResult.Win : PlayerResult.Los,
            Duration = duration,
            Player = new() { Name = $"Player{pos}", ToonId = new() { Region = 1, Realm = 1, Id = pos } },
            Upgrades =
            [
                new() { Name = "TerranInfantryWeaponsLevel1", Gameloop = upgradeGameloop + pos },
                new() { Name = "TerranInfantryArmorsLevel1", Gameloop = upgradeGameloop + pos + 100 }
            ]
        }).ToList()
    };
}
