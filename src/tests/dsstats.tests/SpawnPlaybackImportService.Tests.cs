using dsstats.db;
using dsstats.dbServices;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace dsstats.tests;

[TestClass]
public sealed class SpawnPlaybackImportServiceTests
{
    [TestMethod]
    public async Task InsertReplayImports_NewReplayWithSidecar_StoresPayload()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var importService = serviceProvider.GetRequiredService<IImportService>();
            var sidecar = CreateSidecar("Marine");

            await importService.InsertReplayImports([new(CreateReplay("Direct Strike", 900), sidecar)]);

            var stored = await GetOnlySidecar(serviceProvider);
            Assert.AreEqual(sidecar.CompressedLength, stored.CompressedLength);
            CollectionAssert.AreEqual(sidecar.Payload, stored.Payload);
        }
        finally
        {
            connection.Dispose();
        }
    }

    [TestMethod]
    public async Task InsertReplayImports_NewReplayWithSidecar_StoresNullSpawnUnitPositions()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var importService = serviceProvider.GetRequiredService<IImportService>();

            await importService.InsertReplayImports([new(CreateReplayWithSpawnPositions("Direct Strike", 900), CreateSidecar("Marine"))]);

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
            var spawnUnit = await context.SpawnUnits.SingleAsync();

            Assert.IsNull(spawnUnit.Positions);

            var replay = await context.Replays
                .Include(r => r.Players)
                    .ThenInclude(p => p.Player)
                .Include(r => r.Players)
                    .ThenInclude(p => p.Spawns)
                        .ThenInclude(s => s.Units)
                            .ThenInclude(u => u.Unit)
                .SingleAsync();
            Assert.IsNull(replay.ToDto().Players[0].Spawns[0].Units[0].Positions);
        }
        finally
        {
            connection.Dispose();
        }
    }

    [TestMethod]
    public async Task InsertReplayImports_NewReplayWithoutSidecar_KeepsSpawnUnitPositions()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var importService = serviceProvider.GetRequiredService<IImportService>();

            await importService.InsertReplayImports([new(CreateReplayWithSpawnPositions("Direct Strike", 900), null)]);

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
            var spawnUnit = await context.SpawnUnits.SingleAsync();

            Assert.IsNotNull(spawnUnit.Positions);
            CollectionAssert.AreEqual(new[] { 165, 174, 166, 173 }, spawnUnit.Positions!);
        }
        finally
        {
            connection.Dispose();
        }
    }

    [TestMethod]
    public async Task InsertReplayImports_DuplicateKeptWithExistingSidecar_DoesNotOverwrite()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var importService = serviceProvider.GetRequiredService<IImportService>();
            var existingSidecar = CreateSidecar("Marine");
            var incomingSidecar = CreateSidecar("Marauder");

            await importService.InsertReplayImports([new(CreateReplay("Direct Strike", 900), existingSidecar)]);
            await importService.InsertReplayImports([new(CreateReplay("Direct Strike", 800), incomingSidecar)]);

            var stored = await GetOnlySidecar(serviceProvider);
            CollectionAssert.AreEqual(existingSidecar.Payload, stored.Payload);
        }
        finally
        {
            connection.Dispose();
        }
    }

    [TestMethod]
    public async Task InsertReplayImports_DuplicateKeptWithoutSidecar_AttachesIncomingSidecar()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var importService = serviceProvider.GetRequiredService<IImportService>();
            var incomingSidecar = CreateSidecar("Marauder");

            await importService.InsertReplayImports([new(CreateReplay("Direct Strike", 900), null)]);
            await importService.InsertReplayImports([new(CreateReplay("Direct Strike", 800), incomingSidecar)]);

            var stored = await GetOnlySidecar(serviceProvider);
            CollectionAssert.AreEqual(incomingSidecar.Payload, stored.Payload);
        }
        finally
        {
            connection.Dispose();
        }
    }

    [TestMethod]
    public async Task InsertReplayImports_ReplacingReplay_StoresIncomingSidecar()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var importService = serviceProvider.GetRequiredService<IImportService>();
            var oldSidecar = CreateSidecar("Marine");
            var replacementSidecar = CreateSidecar("Marauder");

            await importService.InsertReplayImports([new(CreateReplay("Direct Strike", 800), oldSidecar)]);
            await importService.InsertReplayImports([new(CreateReplay("Direct Strike", 900), replacementSidecar)]);

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
            var replay = await context.Replays.SingleAsync();
            var stored = await context.ReplaySpawnPlaybacks.SingleAsync();

            Assert.AreEqual(900, replay.Duration);
            CollectionAssert.AreEqual(replacementSidecar.Payload, stored.Payload);
        }
        finally
        {
            connection.Dispose();
        }
    }

    [TestMethod]
    public async Task CheckDuplicateCandidates_CopiesDuplicateSidecarToKeeper()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var importService = serviceProvider.GetRequiredService<IImportService>();
            var duplicateSidecar = CreateSidecar("Marauder");

            await importService.InsertReplayImports([new(CreateReplay("Keeper Title", 900), null)]);
            await importService.InsertReplayImports([new(CreateReplay("Duplicate Title", 800), duplicateSidecar)]);

            await importService.CheckDuplicateCandidates();

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
            var replay = await context.Replays.SingleAsync();
            var stored = await context.ReplaySpawnPlaybacks.SingleAsync();

            Assert.AreEqual("Keeper Title", replay.Title);
            CollectionAssert.AreEqual(duplicateSidecar.Payload, stored.Payload);
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

        services.AddDbContextFactory<DsstatsContext>(o => o.UseSqlite(localConnection, options =>
        {
            options.MigrationsAssembly("dsstats.migrations.sqlite");
        }));
        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<DsstatsContext>>().CreateDbContext());
        services.AddSingleton(Mock.Of<IRatingService>());
        services.AddSingleton<IImportService, ImportService>();
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        context.Database.EnsureDeleted();
        context.Database.Migrate();
        return serviceProvider;
    }

    private static async Task<ReplaySpawnPlayback> GetOnlySidecar(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        return await context.ReplaySpawnPlaybacks.SingleAsync();
    }

    private static ReplayDto CreateReplay(string title, int duration)
    {
        return new()
        {
            Title = title,
            Version = "5.0.14",
            GameMode = GameMode.Standard,
            RegionId = 1,
            Gametime = new DateTime(2026, 5, 23, 12, 0, 0, DateTimeKind.Utc),
            Duration = duration,
            WinnerTeam = 1,
            Players =
            [
                CreatePlayer(1),
                CreatePlayer(2),
                CreatePlayer(3),
                CreatePlayer(4)
            ]
        };
    }

    private static ReplayDto CreateReplayWithSpawnPositions(string title, int duration)
    {
        var replay = CreateReplay(title, duration);
        replay.Players[0].Spawns.Add(new()
        {
            Breakpoint = Breakpoint.Min5,
            GasCount = 1,
            Units =
            [
                new()
                {
                    Name = "Marine",
                    Count = 2,
                    Positions = [165, 174, 166, 173]
                }
            ]
        });
        return replay;
    }

    private static ReplayPlayerDto CreatePlayer(int gamePos)
    {
        return new()
        {
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

    private static SpawnPlaybackEncodedSidecar CreateSidecar(string unitName)
    {
        return SpawnPlaybackSidecarCodec.EncodeWithMetadata(new(
            DurationGameloop: 900 * 16,
            StepGameloops: 80,
            Players:
            [
                new(1,
                [
                    new(
                        UnitIndex: 1,
                        Name: unitName,
                        SpawnNumber: 1,
                        SpawnGameloop: 160,
                        SpawnX: 10,
                        SpawnY: 20,
                        DiedGameloop: 320,
                        DiedX: 30,
                        DiedY: 40,
                        KillGameloops: [200, 260])
                ])
            ],
            Snapshots:
            [
                new(1, 160, 320)
            ]));
    }
}
