using dsstats.dbServices.Stats;
using dsstats.db;
using dsstats.shared;
using dsstats.shared.Upload;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.tests;

[TestClass]
public sealed class DashboardStatsServiceTests
{
    [TestMethod]
    public async Task GetDashboardStatsAsync_GroupsPersistedClientAndApiSources()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DsstatsContext>()
            .UseSqlite(connection)
            .Options;
        await using (var context = new DsstatsContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.UploadJobs.AddRange(
                new UploadJob
                {
                    Version = "ser3.1.2",
                    DecoderSource = ReplayDecoderSource.Service,
                    DecoderVersion = "3.1.2",
                    CreatedAt = DateTime.UtcNow,
                },
                new UploadJob
                {
                    Version = "myds3.1.1",
                    DecoderSource = ReplayDecoderSource.MyDsstats,
                    DecoderVersion = "3.1.1",
                    CreatedAt = DateTime.UtcNow,
                });
            context.ReplayUploadJobs.Add(new ReplayUploadJob
            {
                Guid = Guid.NewGuid(),
                Version = "api3.1.0",
                DecoderSource = ReplayDecoderSource.Api,
                DecoderVersion = "3.1.0",
                CreatedAt = DateTime.UtcNow,
            });
            await context.SaveChangesAsync();
        }

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new DashboardStatsService(new TestDbContextFactory<DsstatsContext>(options), cache);
        var response = await service.GetDashboardStatsAsync();

        Assert.AreEqual(3, response.Uploads);
        AssertUploadSource(response.UploadStats, "service", 1, ("3.1.2", 1));
        AssertUploadSource(response.UploadStats, "mydsstats", 1, ("3.1.1", 1));
        AssertUploadSource(response.UploadStats, "api", 1, ("3.1.0", 1));
    }

    [TestMethod]
    public void NormalizeUploadStats_GroupsKnownUploadSources()
    {
        var stats = DashboardStatsService.NormalizeUploadStats(
        [
            ("myds1.9", 2),
            ("ser3.0.6", 3),
            ("ma3.7", 4),
            ("api3.1.0", 5),
        ]);

        AssertUploadSource(stats, "mydsstats", 2, ("1.9", 2));
        AssertUploadSource(stats, "service", 3, ("3.0.6", 3));
        AssertUploadSource(stats, "maui", 4, ("3.7", 4));
        AssertUploadSource(stats, "api", 5, ("3.1.0", 5));
    }

    [TestMethod]
    public void NormalizeUploadStats_CountsLegacyAndUnknownVersionsAsMaui()
    {
        var stats = DashboardStatsService.NormalizeUploadStats(
        [
            ("3.0.3", 5),
            ((string?)null, 2),
            ("", 3),
            ("other-client", 7),
        ]);

        var maui = AssertUploadSource(
            stats,
            "maui",
            17,
            ("other-client", 7),
            ("3.0.3", 5),
            ("unknown", 5));
        Assert.AreEqual(3, maui.Versions.Count);
    }

    [TestMethod]
    public void NormalizeUploadStats_MergesNormalizedVersions()
    {
        var stats = DashboardStatsService.NormalizeUploadStats(
        [
            ("ma3.7", 4),
            ("MA3.7", 6),
            ("myds", 1),
            ("ser ", 2),
        ]);

        AssertUploadSource(stats, "maui", 10, ("3.7", 10));
        AssertUploadSource(stats, "mydsstats", 1, ("unknown", 1));
        AssertUploadSource(stats, "service", 2, ("unknown", 2));
        Assert.AreEqual(13, stats.Sum(x => x.Count));
    }

    [TestMethod]
    public void NormalizeUploadStats_PrefersPersistedDecoderFields()
    {
        var stats = DashboardStatsService.NormalizeUploadStats(
        [
            (ReplayDecoderSource.Service, "3.1.2", "ma9.9.9", 4),
            (ReplayDecoderSource.Api, "3.1.0", (string?)null, 3),
            ((ReplayDecoderSource?)null, (string?)null, "myds3.1.1", 2),
        ]);

        AssertUploadSource(stats, "service", 4, ("3.1.2", 4));
        AssertUploadSource(stats, "api", 3, ("3.1.0", 3));
        AssertUploadSource(stats, "mydsstats", 2, ("3.1.1", 2));
    }

    private static DashboardUploadSourceStats AssertUploadSource(
        List<DashboardUploadSourceStats> stats,
        string source,
        int count,
        params (string Version, int Count)[] versions)
    {
        var sourceStats = stats.Single(x => x.Source == source);
        Assert.AreEqual(count, sourceStats.Count);

        foreach (var (version, versionCount) in versions)
        {
            var versionStats = sourceStats.Versions.Single(x => x.Version == version);
            Assert.AreEqual(versionCount, versionStats.Count);
        }

        return sourceStats;
    }
}
