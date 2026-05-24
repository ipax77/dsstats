using dsstats.db;
using dsstats.dbServices;
using dsstats.shared;
using dsstats.shared.Interfaces;
using dsstats.shared.Upload;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO.Compression;

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

    [TestMethod]
    public async Task InsertReplayImportsWithSidecars_GZipManifest_StoresPayload()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var importService = serviceProvider.GetRequiredService<IImportService>();
            var replay = CreateReplay("Direct Strike", 900);
            var sidecar = CreateGZipSidecar("Marine");

            var result = await importService.InsertReplayImportsWithSidecars(
                CreateUploadRequest(replay),
                [CreateManifestEntry(replay, sidecar)],
                CreatePayloads(sidecar));

            Assert.IsTrue(result.Success, result.Error);
            var stored = await GetOnlySidecar(serviceProvider);
            Assert.AreEqual(SpawnPlaybackCompression.GZip, stored.Compression);
            Assert.AreEqual(sidecar.CompressedLength, stored.CompressedLength);
            CollectionAssert.AreEqual(sidecar.Payload, stored.Payload);
        }
        finally
        {
            connection.Dispose();
        }
    }

    [TestMethod]
    public async Task InsertReplayImportsWithSidecars_DuplicateReplayWithSidecar_ReturnsSuccessAndStoresPayload()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var importService = serviceProvider.GetRequiredService<IImportService>();
            var replay = CreateReplay("Direct Strike", 900);
            var sidecar = CreateGZipSidecar("Marine");

            await importService.InsertReplayImports([new(replay, null)]);

            var result = await importService.InsertReplayImportsWithSidecars(
                CreateUploadRequest(replay),
                [CreateManifestEntry(replay, sidecar)],
                CreatePayloads(sidecar));

            Assert.IsTrue(result.Success, result.Error);
            CollectionAssert.AreEqual(new[] { replay.ComputeHash() }, result.ReplayHashes);

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
            Assert.AreEqual(1, await context.Replays.CountAsync());
            var stored = await context.ReplaySpawnPlaybacks.SingleAsync();
            CollectionAssert.AreEqual(sidecar.Payload, stored.Payload);
        }
        finally
        {
            connection.Dispose();
        }
    }

    [TestMethod]
    public async Task InsertReplayImportsWithSidecars_MismatchedLength_ImportsReplayWithoutSidecar()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var importService = serviceProvider.GetRequiredService<IImportService>();
            var replay = CreateReplay("Direct Strike", 900);
            var sidecar = CreateGZipSidecar("Marine");
            var manifest = CreateManifestEntry(replay, sidecar, compressedLength: sidecar.CompressedLength + 1);

            var result = await importService.InsertReplayImportsWithSidecars(
                CreateUploadRequest(replay),
                [manifest],
                CreatePayloads(sidecar));

            Assert.IsTrue(result.Success, result.Error);

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
            Assert.AreEqual(1, await context.Replays.CountAsync());
            Assert.AreEqual(0, await context.ReplaySpawnPlaybacks.CountAsync());
        }
        finally
        {
            connection.Dispose();
        }
    }

    [TestMethod]
    public async Task InsertReplayImportsWithSidecars_UnsupportedCompression_ImportsReplayWithoutSidecar()
    {
        using var serviceProvider = BuildServiceProvider(out var connection);
        try
        {
            var importService = serviceProvider.GetRequiredService<IImportService>();
            var replay = CreateReplay("Direct Strike", 900);
            var sidecar = CreateGZipSidecar("Marine");
            var manifest = CreateManifestEntry(replay, sidecar, compression: (SpawnPlaybackCompression)255);

            var result = await importService.InsertReplayImportsWithSidecars(
                CreateUploadRequest(replay),
                [manifest],
                CreatePayloads(sidecar));

            Assert.IsTrue(result.Success, result.Error);

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
            Assert.AreEqual(1, await context.Replays.CountAsync());
            Assert.AreEqual(0, await context.ReplaySpawnPlaybacks.CountAsync());
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
        return SpawnPlaybackSidecarCodec.EncodeWithMetadata(CreateSidecarDto(unitName));
    }

    private static SpawnPlaybackEncodedSidecar CreateGZipSidecar(string unitName)
    {
        var raw = SpawnPlaybackSidecarCodec.EncodeRawWithMetadata(CreateSidecarDto(unitName));
        byte[] gzipPayload = GZip(raw.Payload);
        return raw with
        {
            Payload = gzipPayload,
            CompressedLength = gzipPayload.Length,
            Compression = SpawnPlaybackCompression.GZip,
        };
    }

    private static SpawnPlaybackSidecarDto CreateSidecarDto(string unitName)
    {
        return new(
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
            ]);
    }

    private static UploadRequestDto CreateUploadRequest(ReplayDto replay)
    {
        return new()
        {
            AppGuid = Guid.NewGuid(),
            AppVersion = "test",
            Replays = [replay],
        };
    }

    private static SpawnPlaybackUploadManifestEntryDto CreateManifestEntry(
        ReplayDto replay,
        SpawnPlaybackEncodedSidecar sidecar,
        SpawnPlaybackCompression? compression = null,
        int? compressedLength = null)
    {
        return new()
        {
            ReplayHash = replay.ComputeHash(),
            PartName = "sidecar-0",
            FormatVersion = sidecar.FormatVersion,
            Compression = compression ?? sidecar.Compression,
            CompressedLength = compressedLength ?? sidecar.CompressedLength,
            UncompressedLength = sidecar.UncompressedLength,
            UnitCount = sidecar.UnitCount,
        };
    }

    private static Dictionary<string, SpawnPlaybackUploadPayload> CreatePayloads(SpawnPlaybackEncodedSidecar sidecar)
    {
        return new(StringComparer.Ordinal)
        {
            ["sidecar-0"] = new("sidecar-0", sidecar.Payload.Length, () => new MemoryStream(sidecar.Payload))
        };
    }

    private static byte[] GZip(byte[] payload)
    {
        using var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(payload);
        }
        return compressed.ToArray();
    }
}
