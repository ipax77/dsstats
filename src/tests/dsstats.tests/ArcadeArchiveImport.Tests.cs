using dsstats.db;
using dsstats.dbServices;
using dsstats.shared;
using dsstats.shared.Arcade;
using dsstats.shared.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sc2arcade.backfill;

namespace dsstats.tests;

[TestClass]
public class ArcadeArchiveImportTests
{
    [TestMethod]
    public async Task PreviewDoesNotWrite_ExecuteAndResumeDeduplicate()
    {
        using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        using var services = Build(connection);
        var factory = services.GetRequiredService<IDbContextFactory<DsstatsContext>>();
        await using var context = await factory.CreateDbContextAsync();
        await context.Database.MigrateAsync();
        var importer = new ArcadeArchiveImporter(factory);
        var replay = Replay();
        var preview = await importer.ImportAsync([replay, replay], false);
        Assert.AreEqual(1, preview.Missing);
        Assert.AreEqual(0, await context.Players.CountAsync());
        Assert.AreEqual(0, await context.ArcadeReplays.CountAsync());
        var first = await importer.ImportAsync([replay, replay], true);
        Assert.AreEqual(1, first.Inserted);
        Assert.AreEqual(6, first.NewPlayers);
        var second = await importer.ImportAsync([replay], true);
        Assert.AreEqual(1, second.Existing);
        Assert.AreEqual(0, second.Inserted);
        Assert.AreEqual(1, await context.ArcadeReplays.CountAsync());
        Assert.AreEqual(6, await context.ArcadeReplayPlayers.CountAsync());
    }

    [TestMethod]
    public async Task FailedReplaySaveRollsBackPlayers_ResumeRetriesWholeBatch()
    {
        using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var failure = new ReplayFailure();
        using var services = Build(connection, failure);
        var factory = services.GetRequiredService<IDbContextFactory<DsstatsContext>>();
        await using var context = await factory.CreateDbContextAsync();
        await context.Database.MigrateAsync();
        var importer = new ArcadeArchiveImporter(factory);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await importer.ImportAsync([Replay()], true));
        Assert.AreEqual(0, await context.Players.CountAsync());
        Assert.AreEqual(0, await context.ArcadeReplays.CountAsync());
        failure.Fail = false;
        Assert.AreEqual(1, (await importer.ImportAsync([Replay()], true)).Inserted);
    }

    [TestMethod]
    public async Task LiveImporterDoesNotCacheReplayKeysBeforeCommit()
    {
        using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var failure = new LiveReplayFailure();
        using var services = Build(connection, failure);
        var factory = services.GetRequiredService<IDbContextFactory<DsstatsContext>>();
        await using var context = await factory.CreateDbContextAsync();
        await context.Database.MigrateAsync();
        var importer = new ImportService(factory, services.GetRequiredService<IServiceScopeFactory>(),
            Mock.Of<IRatingService>(), NullLogger<ImportService>.Instance);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => importer.ImportArcadeReplays([Replay()]));
        failure.Fail = false;
        await importer.ImportArcadeReplays([Replay()]);
        Assert.AreEqual(1, await context.ArcadeReplays.CountAsync());
    }

    private static ServiceProvider Build(SqliteConnection connection, IInterceptor? failure = null)
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<DsstatsContext>(o =>
        {
            o.UseSqlite(connection, s => s.MigrationsAssembly("dsstats.migrations.sqlite"));
            if (failure is not null) o.AddInterceptors(failure);
        });
        return services.BuildServiceProvider();
    }

    private static ArcadeReplayDto Replay() => new()
    {
        RegionId = 1, BnetBucketId = 123, BnetRecordId = 456, CreatedAt = new(2026, 9, 1),
        GameMode = GameMode.Commanders, PlayerCount = 6, Duration = 1000, WinnerTeam = 1,
        Players = Enumerable.Range(1, 6).Select(i => new ArcadeReplayPlayerDto
        {
            SlotNumber = i, Team = i <= 3 ? 1 : 2,
            Player = new() { Name = $"Player{i}", ToonId = new() { Region = 1, Realm = 1, Id = i } }
        }).ToList()
    };

    private sealed class ReplayFailure : DbCommandInterceptor
    {
        public bool Fail { get; set; } = true;
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Fail && command.CommandText.StartsWith("INSERT INTO `ArcadeReplays`", StringComparison.Ordinal))
                throw new InvalidOperationException("Simulated failed replay write.");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class LiveReplayFailure : SaveChangesInterceptor
    {
        public bool Fail { get; set; } = true;
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Fail && eventData.Context!.ChangeTracker.Entries<ArcadeReplay>().Any())
                throw new InvalidOperationException("Simulated replay save failure.");
            return ValueTask.FromResult(result);
        }
    }
}
