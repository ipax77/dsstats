using dsstats.api.Services;
using dsstats.db;
using dsstats.dbServices;
using dsstats.shared;
using dsstats.shared.Interfaces;
using dsstats.shared.Upload;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Channels;

namespace dsstats.tests;

[TestClass]
public sealed class SpawnPlaybackUploadServiceTests
{
    private static readonly JsonSerializerOptions UploadJsonOptions = new(JsonSerializerDefaults.Web);

    [TestMethod]
    public async Task ProcessSpawnPlaybackUploadAsync_SingleReplayStoresPackageAndQueuesJob()
    {
        var tempDirectory = CreateTempDirectory();
        await using var fixture = await UploadFixture.Create(tempDirectory, Mock.Of<IImportService>());
        try
        {
            var replay = CreateReplay("Direct Strike", 900);
            var sidecarPayload = new byte[] { 1, 2, 3, 4 };

            var result = await fixture.UploadService.ProcessSpawnPlaybackUploadAsync(
                JsonSerializer.Serialize(replay, UploadJsonOptions),
                CreateFormFile("sidecar", sidecarPayload),
                SpawnPlaybackSidecarCodec.FormatVersion,
                SpawnPlaybackCompression.GZip,
                sidecarPayload.Length,
                uncompressedLength: 16,
                unitCount: 1,
                CancellationToken.None);

            Assert.IsTrue(result.Success, result.Error);
            Assert.AreEqual(replay.ComputeHash(), result.ReplayHash);

            Assert.IsTrue(await fixture.UploadChannel.Reader.WaitToReadAsync(CancellationToken.None));
            Assert.IsTrue(fixture.UploadChannel.Reader.TryRead(out var queuedJob));
            Assert.IsTrue(Directory.Exists(queuedJob.BlobFilePath));
            Assert.IsTrue(File.Exists(Path.Combine(queuedJob.BlobFilePath, "request.json.gz")));
            Assert.IsTrue(File.Exists(Path.Combine(queuedJob.BlobFilePath, "manifest.json")));
            Assert.AreEqual(1, Directory.GetFiles(queuedJob.BlobFilePath, "*.sidecar").Length);

            await using var context = await fixture.ContextFactory.CreateDbContextAsync();
            var dbJob = await context.UploadJobs.SingleAsync();
            Assert.AreEqual(queuedJob.UploadJobId, dbJob.UploadJobId);
            Assert.AreEqual(string.Empty, dbJob.Version);
            Assert.IsNull(dbJob.FinishedAt);
            Assert.AreEqual(string.Empty, dbJob.Error);
        }
        finally
        {
            DeleteTempDirectory(tempDirectory);
        }
    }

    [TestMethod]
    public async Task ProcessSpawnPlaybackUploadAsync_SingleReplayRejectsInvalidRequestsWithoutQueueing()
    {
        var tempDirectory = CreateTempDirectory();
        await using var fixture = await UploadFixture.Create(tempDirectory, Mock.Of<IImportService>());
        try
        {
            var replay = CreateReplay("Direct Strike", 900);
            var replayJson = JsonSerializer.Serialize(replay, UploadJsonOptions);
            var sidecar = CreateFormFile("sidecar", [1, 2, 3, 4]);

            var invalidReplay = await fixture.UploadService.ProcessSpawnPlaybackUploadAsync(
                "{",
                sidecar,
                SpawnPlaybackSidecarCodec.FormatVersion,
                SpawnPlaybackCompression.GZip,
                compressedLength: 4,
                uncompressedLength: 16,
                unitCount: 1,
                CancellationToken.None);
            Assert.IsFalse(invalidReplay.Success);
            Assert.AreEqual("Invalid replay payload.", invalidReplay.Error);

            var mismatchedLength = await fixture.UploadService.ProcessSpawnPlaybackUploadAsync(
                replayJson,
                sidecar,
                SpawnPlaybackSidecarCodec.FormatVersion,
                SpawnPlaybackCompression.GZip,
                compressedLength: 5,
                uncompressedLength: 16,
                unitCount: 1,
                CancellationToken.None);
            Assert.IsFalse(mismatchedLength.Success);
            Assert.AreEqual("Sidecar compressed length does not match payload length.", mismatchedLength.Error);

            var invalidMetadata = await fixture.UploadService.ProcessSpawnPlaybackUploadAsync(
                replayJson,
                sidecar,
                formatVersion: 0,
                compression: SpawnPlaybackCompression.GZip,
                compressedLength: 4,
                uncompressedLength: 16,
                unitCount: 1,
                CancellationToken.None);
            Assert.IsFalse(invalidMetadata.Success);
            Assert.AreEqual("Invalid sidecar metadata.", invalidMetadata.Error);

            var unsupportedCompression = await fixture.UploadService.ProcessSpawnPlaybackUploadAsync(
                replayJson,
                sidecar,
                SpawnPlaybackSidecarCodec.FormatVersion,
                (SpawnPlaybackCompression)255,
                compressedLength: 4,
                uncompressedLength: 16,
                unitCount: 1,
                CancellationToken.None);
            Assert.IsFalse(unsupportedCompression.Success);
            Assert.AreEqual("Unsupported sidecar compression.", unsupportedCompression.Error);

            await using var context = await fixture.ContextFactory.CreateDbContextAsync();
            Assert.AreEqual(0, await context.UploadJobs.CountAsync());
            Assert.IsFalse(fixture.UploadChannel.Reader.TryRead(out _));
        }
        finally
        {
            DeleteTempDirectory(tempDirectory);
        }
    }

    [TestMethod]
    public async Task ProcessSpawnPlaybackUploadAsync_StoresPackageAndQueuesJob()
    {
        var tempDirectory = CreateTempDirectory();
        await using var fixture = await UploadFixture.Create(tempDirectory, Mock.Of<IImportService>());
        try
        {
            var replay = CreateReplay("Direct Strike", 900);
            var sidecarPayload = new byte[] { 1, 2, 3, 4 };
            var request = CreateUploadRequestFile(CreateUploadRequest(replay));
            var manifest = CreateManifestJson(replay, sidecarPayload.Length);
            var files = CreateFormFiles(request, CreateFormFile("sidecar-0", sidecarPayload));

            var result = await fixture.UploadService.ProcessSpawnPlaybackUploadAsync(
                request,
                manifest,
                files,
                CancellationToken.None);

            Assert.IsTrue(result.Success, result.Error);
            CollectionAssert.AreEqual(new[] { replay.ComputeHash() }, result.ReplayHashes);

            Assert.IsTrue(await fixture.UploadChannel.Reader.WaitToReadAsync(CancellationToken.None));
            Assert.IsTrue(fixture.UploadChannel.Reader.TryRead(out var queuedJob));
            Assert.IsTrue(Directory.Exists(queuedJob.BlobFilePath));
            Assert.IsTrue(File.Exists(Path.Combine(queuedJob.BlobFilePath, "request.json.gz")));
            Assert.IsTrue(File.Exists(Path.Combine(queuedJob.BlobFilePath, "manifest.json")));
            Assert.AreEqual(1, Directory.GetFiles(queuedJob.BlobFilePath, "*.sidecar").Length);

            await using var context = await fixture.ContextFactory.CreateDbContextAsync();
            var dbJob = await context.UploadJobs.SingleAsync();
            Assert.AreEqual(queuedJob.UploadJobId, dbJob.UploadJobId);
            Assert.AreEqual("test", dbJob.Version);
            Assert.IsNull(dbJob.FinishedAt);
            Assert.AreEqual(string.Empty, dbJob.Error);
        }
        finally
        {
            DeleteTempDirectory(tempDirectory);
        }
    }

    [TestMethod]
    public async Task ProcessSpawnPlaybackUploadAsync_RejectsInvalidRequestsWithoutQueueing()
    {
        var tempDirectory = CreateTempDirectory();
        await using var fixture = await UploadFixture.Create(tempDirectory, Mock.Of<IImportService>());
        try
        {
            var replay = CreateReplay("Direct Strike", 900);
            var validRequest = CreateUploadRequestFile(CreateUploadRequest(replay));
            var validManifest = CreateManifestJson(replay, 4);
            var validFiles = CreateFormFiles(validRequest, CreateFormFile("sidecar-0", [1, 2, 3, 4]));

            var missingRequest = await fixture.UploadService.ProcessSpawnPlaybackUploadAsync(
                null,
                validManifest,
                new FormFileCollection(),
                CancellationToken.None);
            Assert.IsFalse(missingRequest.Success);
            Assert.AreEqual("Invalid replay payload.", missingRequest.Error);

            var invalidGzip = await fixture.UploadService.ProcessSpawnPlaybackUploadAsync(
                CreateFormFile("request", [1, 2, 3]),
                validManifest,
                validFiles,
                CancellationToken.None);
            Assert.IsFalse(invalidGzip.Success);
            Assert.AreEqual("Invalid replay payload gzip stream.", invalidGzip.Error);

            var missingManifest = await fixture.UploadService.ProcessSpawnPlaybackUploadAsync(
                validRequest,
                null,
                validFiles,
                CancellationToken.None);
            Assert.IsFalse(missingManifest.Success);
            Assert.AreEqual("Missing sidecar manifest.", missingManifest.Error);

            var duplicatePayload = await fixture.UploadService.ProcessSpawnPlaybackUploadAsync(
                CreateUploadRequestFile(CreateUploadRequest(replay)),
                validManifest,
                CreateFormFiles(
                    CreateUploadRequestFile(CreateUploadRequest(replay)),
                    CreateFormFile("sidecar-0", [1]),
                    CreateFormFile("sidecar-0", [2])),
                CancellationToken.None);
            Assert.IsFalse(duplicatePayload.Success);
            Assert.AreEqual("Duplicate sidecar payload.", duplicatePayload.Error);

            await using var context = await fixture.ContextFactory.CreateDbContextAsync();
            Assert.AreEqual(0, await context.UploadJobs.CountAsync());
            Assert.IsFalse(fixture.UploadChannel.Reader.TryRead(out _));
        }
        finally
        {
            DeleteTempDirectory(tempDirectory);
        }
    }

    [TestMethod]
    public async Task UploadProcessingService_ImportsDirectoryBackedSpawnPlaybackBatchPackage()
    {
        var tempDirectory = CreateTempDirectory();
        await using var serviceProvider = BuildServiceProvider(out var connection);
        await using var fixture = await UploadFixture.Create(
            tempDirectory,
            serviceProvider.GetRequiredService<IImportService>(),
            serviceProvider.GetRequiredService<IDbContextFactory<DsstatsContext>>());
        var worker = new UploadProcessingService(
            NullLogger<UploadProcessingService>.Instance,
            fixture.ContextFactory,
            fixture.UploadChannel,
            serviceProvider.GetRequiredService<IImportService>());

        try
        {
            var replay = CreateReplay("Direct Strike", 900);
            var sidecarPayload = new byte[] { 5, 6, 7, 8 };
            var request = CreateUploadRequestFile(CreateUploadRequest(replay));
            var manifest = CreateManifestJson(replay, sidecarPayload.Length);

            var result = await fixture.UploadService.ProcessSpawnPlaybackUploadAsync(
                request,
                manifest,
                CreateFormFiles(request, CreateFormFile("sidecar-0", sidecarPayload)),
                CancellationToken.None);
            Assert.IsTrue(result.Success, result.Error);

            Assert.IsTrue(fixture.UploadChannel.Reader.TryRead(out _));
            await worker.StartAsync(CancellationToken.None);

            var job = await WaitForFinishedJob(fixture.ContextFactory);
            Assert.IsNull(job.Error);

            await using var context = await fixture.ContextFactory.CreateDbContextAsync();
            Assert.AreEqual(1, await context.Replays.CountAsync());
            var stored = await context.ReplaySpawnPlaybacks.SingleAsync();
            CollectionAssert.AreEqual(sidecarPayload, stored.Payload);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
            await connection.DisposeAsync();
            DeleteTempDirectory(tempDirectory);
        }
    }

    [TestMethod]
    public async Task UploadProcessingService_ImportsDirectoryBackedSingleSpawnPlaybackPackage()
    {
        var tempDirectory = CreateTempDirectory();
        await using var serviceProvider = BuildServiceProvider(out var connection);
        await using var fixture = await UploadFixture.Create(
            tempDirectory,
            serviceProvider.GetRequiredService<IImportService>(),
            serviceProvider.GetRequiredService<IDbContextFactory<DsstatsContext>>());
        var worker = new UploadProcessingService(
            NullLogger<UploadProcessingService>.Instance,
            fixture.ContextFactory,
            fixture.UploadChannel,
            serviceProvider.GetRequiredService<IImportService>());

        try
        {
            var replay = CreateReplay("Direct Strike", 900);
            var sidecarPayload = new byte[] { 5, 6, 7, 8 };

            var result = await fixture.UploadService.ProcessSpawnPlaybackUploadAsync(
                JsonSerializer.Serialize(replay, UploadJsonOptions),
                CreateFormFile("sidecar", sidecarPayload),
                SpawnPlaybackSidecarCodec.FormatVersion,
                SpawnPlaybackCompression.GZip,
                sidecarPayload.Length,
                uncompressedLength: 16,
                unitCount: 1,
                CancellationToken.None);
            Assert.IsTrue(result.Success, result.Error);

            Assert.IsTrue(fixture.UploadChannel.Reader.TryRead(out _));
            await worker.StartAsync(CancellationToken.None);

            var job = await WaitForFinishedJob(fixture.ContextFactory);
            Assert.IsNull(job.Error);

            await using var context = await fixture.ContextFactory.CreateDbContextAsync();
            Assert.AreEqual(1, await context.Replays.CountAsync());
            var stored = await context.ReplaySpawnPlaybacks.SingleAsync();
            CollectionAssert.AreEqual(sidecarPayload, stored.Payload);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
            await connection.DisposeAsync();
            DeleteTempDirectory(tempDirectory);
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

    private static async Task<UploadJob> WaitForFinishedJob(IDbContextFactory<DsstatsContext> contextFactory)
    {
        for (var i = 0; i < 100; i++)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var job = await context.UploadJobs.SingleOrDefaultAsync();
            if (job?.FinishedAt is not null)
            {
                return job;
            }

            await Task.Delay(50);
        }

        Assert.Fail("Timed out waiting for upload job to finish.");
        throw new UnreachableException();
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

    private static string CreateManifestJson(ReplayDto replay, int payloadLength)
    {
        SpawnPlaybackUploadManifestEntryDto[] manifest =
        [
            new()
            {
                ReplayHash = replay.ComputeHash(),
                PartName = "sidecar-0",
                FormatVersion = SpawnPlaybackSidecarCodec.FormatVersion,
                Compression = SpawnPlaybackCompression.GZip,
                CompressedLength = payloadLength,
                UncompressedLength = 16,
                UnitCount = 1,
            }
        ];
        return JsonSerializer.Serialize(manifest, UploadJsonOptions);
    }

    private static IFormFile CreateUploadRequestFile(UploadRequestDto request)
    {
        using var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
        {
            JsonSerializer.Serialize(gzip, request, UploadJsonOptions);
        }

        return CreateFormFile("request", compressed.ToArray(), "request.json.gz");
    }

    private static FormFileCollection CreateFormFiles(IFormFile request, params IFormFile[] sidecars)
    {
        var files = new FormFileCollection
        {
            request
        };

        foreach (var sidecar in sidecars)
        {
            files.Add(sidecar);
        }

        return files;
    }

    private static IFormFile CreateFormFile(string name, byte[] content, string? fileName = null)
    {
        return new FormFile(new MemoryStream(content), 0, content.Length, name, fileName ?? name)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream",
        };
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "dsstats-upload-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteTempDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private sealed class UploadFixture : IAsyncDisposable
    {
        private readonly ServiceProvider? serviceProvider;

        private UploadFixture(
            string tempDirectory,
            IImportService importService,
            IDbContextFactory<DsstatsContext> contextFactory,
            ServiceProvider? serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            ContextFactory = contextFactory;
            UploadChannel = Channel.CreateUnbounded<UploadJob>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
            });
            ReplayChannel = Channel.CreateUnbounded<ReplayUploadJob>();
            UploadService = new(
                ContextFactory,
                UploadChannel,
                ReplayChannel,
                importService,
                Options.Create(new UploadStorageOptions
                {
                    BlobBaseDir = tempDirectory,
                    ReplayBaseDir = tempDirectory,
                }),
                NullLogger<UploadService>.Instance);
        }

        public IDbContextFactory<DsstatsContext> ContextFactory { get; }
        public Channel<UploadJob> UploadChannel { get; }
        public Channel<ReplayUploadJob> ReplayChannel { get; }
        public UploadService UploadService { get; }

        public static async Task<UploadFixture> Create(
            string tempDirectory,
            IImportService importService,
            IDbContextFactory<DsstatsContext>? contextFactory = null)
        {
            if (contextFactory is not null)
            {
                return new(tempDirectory, importService, contextFactory, null);
            }

            var services = new ServiceCollection();
            var connection = new SqliteConnection("Filename=:memory:");
            await connection.OpenAsync();
            services.AddSingleton(connection);
            services.AddDbContextFactory<DsstatsContext>(o => o.UseSqlite(connection, options =>
            {
                options.MigrationsAssembly("dsstats.migrations.sqlite");
            }));

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<IDbContextFactory<DsstatsContext>>();
            await using var context = await factory.CreateDbContextAsync();
            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();
            return new(tempDirectory, importService, factory, serviceProvider);
        }

        public async ValueTask DisposeAsync()
        {
            if (serviceProvider is not null)
            {
                var connection = serviceProvider.GetRequiredService<SqliteConnection>();
                await connection.DisposeAsync();
                await serviceProvider.DisposeAsync();
            }
        }
    }
}
