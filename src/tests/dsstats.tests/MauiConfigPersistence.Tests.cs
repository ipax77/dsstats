using dsstats.db;
using dsstats.shared.Maui;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace dsstats.tests;

[TestClass]
public sealed class MauiConfigPersistenceTests
{
    [TestMethod]
    public async Task SaveConfig_PreservesMultipleFolderOnlyProfiles()
    {
        await using var fixture = await SqliteFixture.Create();
        var dto = CreateConfig(
            CreateProfile("Alpha", @"C:\Replays\Alpha"),
            CreateProfile("Bravo", @"C:\Replays\Bravo"));

        await SaveConfig(fixture, dto);

        Assert.AreEqual(2, dto.Sc2Profiles.Count(profile => profile.Sc2ProfileId > 0));
        Assert.AreNotEqual(dto.Sc2Profiles[0].Sc2ProfileId, dto.Sc2Profiles[1].Sc2ProfileId);
        var savedProfileIds = dto.Sc2Profiles
            .Select(profile => profile.Sc2ProfileId)
            .ToArray();

        var reloaded = await LoadConfig(fixture);
        Assert.AreEqual(2, reloaded.Sc2Profiles.Count);
        CollectionAssert.AreEquivalent(
            new[] { @"C:\Replays\Alpha", @"C:\Replays\Bravo" },
            reloaded.Sc2Profiles.Select(profile => profile.Folder).ToArray());

        await SaveConfig(fixture, dto);

        reloaded = await LoadConfig(fixture);
        Assert.AreEqual(2, reloaded.Sc2Profiles.Count);
        CollectionAssert.AreEquivalent(
            savedProfileIds,
            reloaded.Sc2Profiles.Select(profile => profile.Sc2ProfileId).ToArray());
    }

    [TestMethod]
    public async Task SaveConfig_PreservesValidToonIdProfileAndFolderOnlyProfile()
    {
        await using var fixture = await SqliteFixture.Create();
        var dto = CreateConfig(
            CreateProfile("Known", @"C:\Replays\Known", region: 1, realm: 1, id: 123),
            CreateProfile("Manual", @"C:\Replays\Manual"));

        await SaveConfig(fixture, dto);

        var reloaded = await LoadConfig(fixture);
        Assert.AreEqual(2, reloaded.Sc2Profiles.Count);
        Assert.IsTrue(reloaded.Sc2Profiles.Any(profile => profile.ToonId.Id == 123));
        Assert.IsTrue(reloaded.Sc2Profiles.Any(profile => profile.ToonId.Id == 0));
    }

    [TestMethod]
    public async Task SaveConfig_RemovesOnlyMissingFolderOnlyProfile()
    {
        await using var fixture = await SqliteFixture.Create();
        var dto = CreateConfig(
            CreateProfile("Alpha", @"C:\Replays\Alpha"),
            CreateProfile("Bravo", @"C:\Replays\Bravo"));

        await SaveConfig(fixture, dto);

        var removedProfileId = dto.Sc2Profiles[0].Sc2ProfileId;
        var remainingProfileId = dto.Sc2Profiles[1].Sc2ProfileId;
        dto.Sc2Profiles.RemoveAt(0);
        await SaveConfig(fixture, dto);

        var reloaded = await LoadConfig(fixture);
        Assert.AreEqual(1, reloaded.Sc2Profiles.Count);
        Assert.AreEqual("Bravo", reloaded.Sc2Profiles[0].Name);
        Assert.AreEqual(remainingProfileId, reloaded.Sc2Profiles[0].Sc2ProfileId);
        Assert.AreNotEqual(removedProfileId, reloaded.Sc2Profiles[0].Sc2ProfileId);
    }

    private static async Task SaveConfig(SqliteFixture fixture, MauiConfigDto dto)
    {
        await using var context = fixture.CreateContext();
        var config = await context.MauiConfig
            .Include(entity => entity.Sc2Profiles)
            .FirstOrDefaultAsync();

        if (config is null)
        {
            config = new MauiConfig();
            var assignments = MauiConfigPersistence.ApplyConfig(config, dto, context);
            context.MauiConfig.Add(config);
            await context.SaveChangesAsync();
            MauiConfigPersistence.SyncGeneratedProfileIds(assignments);
            return;
        }

        var profileIdAssignments = MauiConfigPersistence.ApplyConfig(config, dto, context);
        await context.SaveChangesAsync();
        MauiConfigPersistence.SyncGeneratedProfileIds(profileIdAssignments);
    }

    private static async Task<MauiConfigDto> LoadConfig(SqliteFixture fixture)
    {
        await using var context = fixture.CreateContext();
        var config = await context.MauiConfig
            .Include(entity => entity.Sc2Profiles)
            .AsNoTracking()
            .SingleAsync();

        return config.ToDto();
    }

    private static MauiConfigDto CreateConfig(params Sc2ProfileDto[] profiles)
        => new()
        {
            Version = "3.0.6",
            CPUCores = 2,
            AutoDecode = true,
            CheckForUpdates = true,
            ReplayStartName = "Direct Strike",
            Culture = "en",
            Sc2Profiles = profiles.ToList()
        };

    private static Sc2ProfileDto CreateProfile(
        string name,
        string folder,
        int region = 0,
        int realm = 0,
        int id = 0)
        => new()
        {
            Name = name,
            Folder = folder,
            Active = true,
            ToonId = new()
            {
                Region = region,
                Realm = realm,
                Id = id
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
