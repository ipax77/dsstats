using dsstats.shared;
using dsstats.shared.DsFen;

namespace dsstats.builder.tests;

[TestClass]
public sealed class FenSharedTests
{
    [TestMethod]
    public void FenRoundTrip_FromSpawnDto()
    {
        var spawn = new SpawnDto
        {
            Units = new List<SpawnUnitDto>
        {
            new() { Unit = new() { Name = "Zergling" }, Poss = "91,80" },
            new() { Unit = new() { Name = "Mutalisk" }, Poss = "92,81" },
        }
        };

        var cmdr = Commander.Zerg;
        int team = 2;
        DsBuildRequest buildRequest = new()
        {
            Spawn = spawn,
            Commander = cmdr,
            Team = team
        };

        var fen = DsFen.GetFen(buildRequest);

        DsFen.ApplyFen(fen, out buildRequest);

        var newFen = DsFen.GetFen(buildRequest);

        Assert.AreEqual(fen, newFen);
    }

    [TestMethod]
    public void CanApplyFenString()
    {
        Commander cmdr = Commander.Terran;
        int team = 1;
        SpawnDto spawn = new()
        {
            Units = new List<SpawnUnitDto>
            {
                new() { Unit = new() { Name = "Marine" }, Poss = "160,160,161,160,163,160,165,159,167,154" },
            }
        };
        var points = DsFen.GetPoints(spawn.Units.First().Poss).OrderBy(o => o.X).ThenBy(o => o.Y).ToList();
        var polygon = DsFen.polygon1;
        foreach (var point in points)
        {
            Assert.IsTrue(polygon.IsPointInside(point), $"Point {point} is not inside the polygon.");
        }
        DsBuildRequest buildRequest = new()
        {
            Spawn = spawn,
            Commander = cmdr,
            Team = team
        };
        string fen = DsFen.GetFen(buildRequest);
        Assert.IsNotNull(fen);
        Assert.IsTrue(fen.Length > 10);

        DsBuildRequest reBuildRequest = new();
        DsFen.ApplyFen(fen, out reBuildRequest);
        Assert.AreEqual(Commander.Terran, reBuildRequest.Commander);
        Assert.AreEqual(1, reBuildRequest.Team);
        Assert.IsNotNull(reBuildRequest.Spawn.Units);
        Assert.IsTrue(reBuildRequest.Spawn.Units.Count > 0);
        var rePoints = DsFen.GetPoints(reBuildRequest.Spawn.Units.First().Poss).OrderBy(o => o.X).ThenBy(o => o.Y).ToList();
        Assert.AreEqual(points.Count, rePoints.Count);
        for (int i = 0; i < points.Count; i++)
        {
            Assert.AreEqual(points[i], rePoints[i]);
        }
    }

    [TestMethod]
    public void CanApplyComplexFenString()
    {
        string fen = "q1q22/1qqw2g1w1q1e4q1qqqq1q/w1qq3g1g5eq8/12qqw1g8/1qq14g2qq3/1g3qq18/1w2g2eq16/3g3w1qq14/11qq12/13qq10/13w1qq8/16qqq6/15g1g1qq4/2g2e18q/13w11/21g3/24w|25/25/1d23/25/19f5/4d17d2/25/25/25/25/25/25/25/25/25/25/25 1 2";
        DsBuildRequest buildRequest = new();
        DsFen.ApplyFen(fen, out buildRequest);
        Assert.AreEqual(Commander.Terran, buildRequest.Commander);
        Assert.AreEqual(1, buildRequest.Team);
        Assert.IsNotNull(buildRequest.Spawn.Units);
        Assert.IsTrue(buildRequest.Spawn.Units.Count > 0);
    }

    [TestMethod]
    public void FenUpgrades()
    {
        var spawn = new SpawnDto
        {
            Units = new List<SpawnUnitDto>
        {
            new() { Unit = new() { Name = "Zergling" }, Poss = "91,80" },
            new() { Unit = new() { Name = "Mutalisk" }, Poss = "92,81" },
        }
        };

        var cmdr = Commander.Zerg;
        int team = 2;
        DsBuildRequest buildRequest = new()
        {
            Spawn = spawn,
            Commander = cmdr,
            Team = team,
            Upgrades = new List<PlayerUpgradeDto>
            {
                new() { Upgrade = new() { Name = "MetabolikBoost" } },
                new() { Upgrade = new() { Name = "AdrenalGlands" } },
                new() { Upgrade = new() { Name = "MeleeAttacksLevel1" } },
            },
        };

        var fen = DsFen.GetFen(buildRequest);

        DsFen.ApplyFen(fen, out buildRequest);

        var newFen = DsFen.GetFen(buildRequest);

        Assert.AreEqual(fen, newFen);
    }
}
