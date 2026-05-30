using dsstats.dbServices.Stats;
using dsstats.shared;

namespace dsstats.tests;

[TestClass]
public sealed class DashboardStatsServiceTests
{
    [TestMethod]
    public void NormalizeUploadStats_GroupsKnownUploadSources()
    {
        var stats = DashboardStatsService.NormalizeUploadStats(
        [
            ("myds1.9", 2),
            ("ser3.0.6", 3),
            ("ma3.7", 4),
        ]);

        AssertUploadSource(stats, "mydsstats", 2, ("1.9", 2));
        AssertUploadSource(stats, "service", 3, ("3.0.6", 3));
        AssertUploadSource(stats, "maui", 4, ("3.7", 4));
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
