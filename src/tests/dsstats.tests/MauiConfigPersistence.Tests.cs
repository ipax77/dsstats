using dsstats.db;
using dsstats.shared.Maui;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace dsstats.tests;

[TestClass]
public sealed class MauiConfigPersistenceTests
{
    [TestMethod]
    public async Task SaveConfig_PreservesMultipleManualReplayFolders()
    {
        await using var fixture = await SqliteFixture.Create();
        var dto = CreateConfig(
            manualFolders:
            [
                CreateManualFolder(@"C:\Replays\Alpha"),
                CreateManualFolder(@"C:\Replays\Bravo"),
            ]);

        await SaveConfig(fixture, dto);

        Assert.AreEqual(2, dto.ManualReplayFolders.Count(folder => folder.MauiReplayFolderId > 0));
        Assert.AreNotEqual(
            dto.ManualReplayFolders[0].MauiReplayFolderId,
            dto.ManualReplayFolders[1].MauiReplayFolderId);
        var savedFolderIds = dto.ManualReplayFolders
            .Select(folder => folder.MauiReplayFolderId)
            .ToArray();

        var reloaded = await LoadConfig(fixture);
        Assert.AreEqual(0, reloaded.Sc2Profiles.Count);
        Assert.AreEqual(2, reloaded.ManualReplayFolders.Count);
        CollectionAssert.AreEquivalent(
            new[] { @"C:\Replays\Alpha", @"C:\Replays\Bravo" },
            reloaded.ManualReplayFolders.Select(folder => folder.Folder).ToArray());

        await SaveConfig(fixture, dto);

        reloaded = await LoadConfig(fixture);
        Assert.AreEqual(2, reloaded.ManualReplayFolders.Count);
        CollectionAssert.AreEquivalent(
            savedFolderIds,
            reloaded.ManualReplayFolders.Select(folder => folder.MauiReplayFolderId).ToArray());
    }

    [TestMethod]
    public async Task SaveConfig_RemovesOnlyMissingManualReplayFolder()
    {
        await using var fixture = await SqliteFixture.Create();
        var dto = CreateConfig(
            manualFolders:
            [
                CreateManualFolder(@"C:\Replays\Alpha"),
                CreateManualFolder(@"C:\Replays\Bravo"),
            ]);

        await SaveConfig(fixture, dto);

        var removedFolderId = dto.ManualReplayFolders[0].MauiReplayFolderId;
        var remainingFolderId = dto.ManualReplayFolders[1].MauiReplayFolderId;
        dto.ManualReplayFolders.RemoveAt(0);
        await SaveConfig(fixture, dto);

        var reloaded = await LoadConfig(fixture);
        Assert.AreEqual(1, reloaded.ManualReplayFolders.Count);
        Assert.AreEqual(@"C:\Replays\Bravo", reloaded.ManualReplayFolders[0].Folder);
        Assert.AreEqual(remainingFolderId, reloaded.ManualReplayFolders[0].MauiReplayFolderId);
        Assert.AreNotEqual(removedFolderId, reloaded.ManualReplayFolders[0].MauiReplayFolderId);
    }

    [TestMethod]
    public async Task SaveConfig_NormalizesAndDeduplicatesManualReplayFolders()
    {
        await using var fixture = await SqliteFixture.Create();
        var folder = Path.Combine(Path.GetTempPath(), "dsstats-folder-normalize");
        var duplicateFolder = folder + Path.DirectorySeparatorChar;
        var dto = CreateConfig(
            manualFolders:
            [
                CreateManualFolder($" {folder} "),
                CreateManualFolder(duplicateFolder),
            ]);

        await SaveConfig(fixture, dto);

        var expectedFolder = MauiConfigPersistence.NormalizeFolderPath(folder);
        var reloaded = await LoadConfig(fixture);
        Assert.AreEqual(1, reloaded.ManualReplayFolders.Count);
        Assert.AreEqual(expectedFolder, reloaded.ManualReplayFolders[0].Folder);
        Assert.AreEqual(expectedFolder, dto.ManualReplayFolders[0].Folder);
    }

    [TestMethod]
    public async Task SaveConfig_NormalizesAndDeduplicatesIgnoredReplays()
    {
        await using var fixture = await SqliteFixture.Create();
        var replayPath = Path.Combine(Path.GetTempPath(), "dsstats-ignored", "Direct Strike 3817.SC2Replay");
        var dto = CreateConfig();
        dto.IgnoreReplays =
        [
            $" {replayPath} ",
            replayPath.ToUpperInvariant(),
        ];

        await SaveConfig(fixture, dto);

        var expectedReplayPath = MauiConfigPersistence.NormalizeReplayPath(replayPath);
        var reloaded = await LoadConfig(fixture);
        Assert.AreEqual(1, reloaded.IgnoreReplays.Length);
        Assert.AreEqual(expectedReplayPath, reloaded.IgnoreReplays[0]);
        Assert.AreEqual(expectedReplayPath, dto.IgnoreReplays[0]);
    }

    [TestMethod]
    public async Task SaveConfig_PreservesDetectedManualReplayFolderProfile()
    {
        await using var fixture = await SqliteFixture.Create();
        var dto = CreateConfig(
            manualFolders:
            [
                CreateManualFolder(@"C:\Replays\Alpha"),
            ]);

        await SaveConfig(fixture, dto);
        await using (var context = fixture.CreateContext())
        {
            var folder = await context.ManualReplayFolders.SingleAsync();
            MauiConfigPersistence.SetDetectedProfile(
                folder,
                "Alpha",
                new() { Region = 1, Realm = 1, Id = 123 },
                DateTime.UtcNow,
                replayCount: 2);
            await context.SaveChangesAsync();
        }

        var reloaded = await LoadConfig(fixture);
        await SaveConfig(fixture, reloaded);

        reloaded = await LoadConfig(fixture);
        var detectedFolder = reloaded.ManualReplayFolders.Single();
        Assert.AreEqual("Alpha", detectedFolder.DetectedName);
        Assert.IsNotNull(detectedFolder.DetectedToonId);
        Assert.AreEqual(123, detectedFolder.DetectedToonId.Id);
        Assert.AreEqual(2, detectedFolder.DetectedReplayCount);
    }

    [TestMethod]
    public async Task SaveConfig_ClearsDetectedManualReplayFolderProfileWhenFolderChanges()
    {
        await using var fixture = await SqliteFixture.Create();
        var dto = CreateConfig(
            manualFolders:
            [
                CreateManualFolder(@"C:\Replays\Alpha"),
            ]);

        await SaveConfig(fixture, dto);
        await using (var context = fixture.CreateContext())
        {
            var folder = await context.ManualReplayFolders.SingleAsync();
            MauiConfigPersistence.SetDetectedProfile(
                folder,
                "Alpha",
                new() { Region = 1, Realm = 1, Id = 123 },
                DateTime.UtcNow,
                replayCount: 2);
            await context.SaveChangesAsync();
        }

        var reloaded = await LoadConfig(fixture);
        reloaded.ManualReplayFolders[0].Folder = @"C:\Replays\Bravo";
        await SaveConfig(fixture, reloaded);

        reloaded = await LoadConfig(fixture);
        var changedFolder = reloaded.ManualReplayFolders.Single();
        Assert.AreEqual(@"C:\Replays\Bravo", changedFolder.Folder);
        Assert.IsNull(changedFolder.DetectedName);
        Assert.IsNull(changedFolder.DetectedToonId);
        Assert.AreEqual(0, changedFolder.DetectedReplayCount);
    }

    [TestMethod]
    public async Task DetectProfileCandidate_UsesStrongestRecentToonIdInsideManualFolder()
    {
        await using var fixture = await SqliteFixture.Create();
        var folder = MauiConfigPersistence.NormalizeFolderPath(
            Path.Combine(Path.GetTempPath(), "dsstats-manual-detect"));
        var prefixSiblingFolder = folder + "-sibling";

        await using (var context = fixture.CreateContext())
        {
            var alpha = CreatePlayer("Alpha", region: 1, realm: 1, id: 123);
            var bravo = CreatePlayer("Bravo", region: 1, realm: 1, id: 456);
            var ignored = CreatePlayer("Ignored", region: 1, realm: 1, id: 789);
            context.Players.AddRange(alpha, bravo, ignored);

            context.Replays.AddRange(
                CreateReplay(Path.Combine(folder, "Direct Strike (3).SC2Replay"), DateTime.UtcNow.AddMinutes(-1), alpha),
                CreateReplay(Path.Combine(folder, "Direct Strike (2).SC2Replay"), DateTime.UtcNow.AddMinutes(-2), alpha),
                CreateReplay(Path.Combine(folder, "Direct Strike (1).SC2Replay"), DateTime.UtcNow.AddMinutes(-3), bravo),
                CreateReplay(Path.Combine(prefixSiblingFolder, "Direct Strike (9).SC2Replay"), DateTime.UtcNow, ignored));
            await context.SaveChangesAsync();
        }

        await using (var context = fixture.CreateContext())
        {
            var candidate = await MauiManualReplayFolderDetection.DetectProfileCandidate(context, folder);

            Assert.IsNotNull(candidate);
            Assert.AreEqual("Alpha", candidate.Name);
            Assert.AreEqual(123, candidate.ToonId.Id);
            Assert.AreEqual(2, candidate.ReplayCount);
        }
    }

    [TestMethod]
    public void IsPathInFolder_RejectsSiblingPathWithSamePrefix()
    {
        var folder = MauiConfigPersistence.NormalizeFolderPath(
            Path.Combine(Path.GetTempPath(), "dsstats-prefix"));
        var inside = Path.Combine(folder, "Direct Strike.SC2Replay");
        var sibling = Path.Combine(folder + "-sibling", "Direct Strike.SC2Replay");

        Assert.IsTrue(MauiManualReplayFolderDetection.IsPathInFolder(inside, folder));
        Assert.IsFalse(MauiManualReplayFolderDetection.IsPathInFolder(sibling, folder));
    }

    [TestMethod]
    public async Task RefreshDiscoveredProfiles_UpsertsByToonIdAndPreservesActive()
    {
        await using var fixture = await SqliteFixture.Create();
        await using var context = fixture.CreateContext();
        var config = new MauiConfig
        {
            Sc2Profiles =
            [
                CreateEntityProfile("Existing", @"C:\Old", active: false, region: 1, realm: 1, id: 123),
            ]
        };
        context.MauiConfig.Add(config);
        await context.SaveChangesAsync();

        var changed = MauiConfigPersistence.RefreshDiscoveredProfiles(
            config,
            [
                CreateEntityProfile("Existing Updated", @"C:\New", active: true, region: 1, realm: 1, id: 123),
                CreateEntityProfile("Fresh", @"C:\Fresh", active: true, region: 1, realm: 1, id: 456),
            ],
            context);
        await context.SaveChangesAsync();

        Assert.IsTrue(changed);

        var reloaded = await LoadConfig(fixture);
        Assert.AreEqual(2, reloaded.Sc2Profiles.Count);
        var existing = reloaded.Sc2Profiles.Single(profile => profile.ToonId.Id == 123);
        Assert.AreEqual("Existing Updated", existing.Name);
        Assert.AreEqual(@"C:\New", existing.Folder);
        Assert.IsFalse(existing.Active);
        Assert.IsTrue(reloaded.Sc2Profiles.Single(profile => profile.ToonId.Id == 456).Active);
    }

    [TestMethod]
    public async Task RefreshDiscoveredProfiles_MigratesInvalidProfilesToManualReplayFolders()
    {
        await using var fixture = await SqliteFixture.Create();
        await using var context = fixture.CreateContext();
        var config = new MauiConfig
        {
            Sc2Profiles =
            [
                CreateEntityProfile("Manual", $" {Path.Combine(Path.GetTempPath(), "dsstats-manual")}\\", active: false, region: 0, realm: 0, id: 0),
                CreateEntityProfile("Known", @"C:\Known", active: true, region: 1, realm: 1, id: 123),
            ]
        };
        context.MauiConfig.Add(config);
        await context.SaveChangesAsync();

        var changed = MauiConfigPersistence.RefreshDiscoveredProfiles(
            config,
            [CreateEntityProfile("Known", @"C:\Known", active: true, region: 1, realm: 1, id: 123)],
            context);
        await context.SaveChangesAsync();

        Assert.IsTrue(changed);

        var reloaded = await LoadConfig(fixture);
        Assert.AreEqual(1, reloaded.Sc2Profiles.Count);
        Assert.AreEqual(123, reloaded.Sc2Profiles[0].ToonId.Id);
        Assert.AreEqual(1, reloaded.ManualReplayFolders.Count);
        Assert.AreEqual(
            MauiConfigPersistence.NormalizeFolderPath(Path.Combine(Path.GetTempPath(), "dsstats-manual")),
            reloaded.ManualReplayFolders[0].Folder);
        Assert.IsFalse(reloaded.ManualReplayFolders[0].Active);
    }

    private static async Task SaveConfig(SqliteFixture fixture, MauiConfigDto dto)
    {
        await using var context = fixture.CreateContext();
        var config = await context.MauiConfig
            .Include(entity => entity.Sc2Profiles)
            .Include(entity => entity.ManualReplayFolders)
            .FirstOrDefaultAsync();

        if (config is null)
        {
            config = new MauiConfig();
            var assignments = MauiConfigPersistence.ApplyConfig(config, dto, context);
            context.MauiConfig.Add(config);
            await context.SaveChangesAsync();
            MauiConfigPersistence.SyncGeneratedManualReplayFolderIds(assignments);
            return;
        }

        var folderIdAssignments = MauiConfigPersistence.ApplyConfig(config, dto, context);
        await context.SaveChangesAsync();
        MauiConfigPersistence.SyncGeneratedManualReplayFolderIds(folderIdAssignments);
    }

    private static async Task<MauiConfigDto> LoadConfig(SqliteFixture fixture)
    {
        await using var context = fixture.CreateContext();
        var config = await context.MauiConfig
            .Include(entity => entity.Sc2Profiles)
            .Include(entity => entity.ManualReplayFolders)
            .AsNoTracking()
            .SingleAsync();

        return config.ToDto();
    }

    private static MauiConfigDto CreateConfig(
        Sc2ProfileDto[]? sc2Profiles = null,
        MauiReplayFolderDto[]? manualFolders = null)
        => new()
        {
            Version = "3.0.6",
            CPUCores = 2,
            AutoDecode = true,
            CheckForUpdates = true,
            ReplayStartName = "Direct Strike",
            Culture = "en",
            Sc2Profiles = sc2Profiles?.ToList() ?? [],
            ManualReplayFolders = manualFolders?.ToList() ?? [],
        };

    private static MauiReplayFolderDto CreateManualFolder(string folder)
        => new()
        {
            Folder = folder,
            Active = true,
        };

    private static Sc2Profile CreateEntityProfile(
        string name,
        string folder,
        bool active,
        int region,
        int realm,
        int id)
        => new()
        {
            Name = name,
            Folder = folder,
            Active = active,
            ToonId = new()
            {
                Region = region,
                Realm = realm,
                Id = id,
            }
        };

    private static Player CreatePlayer(
        string name,
        int region,
        int realm,
        int id)
        => new()
        {
            Name = name,
            ToonId = new()
            {
                Region = region,
                Realm = realm,
                Id = id,
            }
        };

    private static Replay CreateReplay(
        string fileName,
        DateTime gametime,
        params Player[] players)
        => new()
        {
            FileName = fileName,
            Title = "Direct Strike",
            Version = "5.0",
            Gametime = gametime,
            ReplayHash = Guid.NewGuid().ToString("N"),
            CompatHash = Guid.NewGuid().ToString("N"),
            PlayerCount = players.Length,
            Players = players
                .Select((player, index) => new ReplayPlayer
                {
                    Name = player.Name,
                    Player = player,
                    TeamId = index % 2 + 1,
                    GamePos = index + 1,
                })
                .ToList(),
        };

    private sealed class SqliteFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly DbContextOptions<DsstatsContext> options;

        private SqliteFixture(SqliteConnection connection, DbContextOptions<DsstatsContext> options)
        {
            this.connection = connection;
            this.options = options;
        }

        public static async Task<SqliteFixture> Create()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<DsstatsContext>()
                .UseSqlite(connection, sqlite => sqlite.MigrationsAssembly("dsstats.migrations.sqlite"))
                .Options;

            await using var context = new DsstatsContext(options);
            await context.Database.MigrateAsync();

            return new SqliteFixture(connection, options);
        }

        public DsstatsContext CreateContext() => new(options);

        public async ValueTask DisposeAsync()
        {
            await connection.DisposeAsync();
        }
    }
}
