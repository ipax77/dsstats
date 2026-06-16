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
                CreateEntityProfile("Manual", @"C:\Manual", active: false, region: 0, realm: 0, id: 0),
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
        Assert.AreEqual(@"C:\Manual", reloaded.ManualReplayFolders[0].Folder);
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
