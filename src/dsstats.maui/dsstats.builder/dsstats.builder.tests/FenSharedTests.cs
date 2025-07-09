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

        var fen = DsFen.GetFen(spawn, cmdr, team);

        var newSpawn = new SpawnDto { Units = [] };
        DsFen.ApplyFen(fen, newSpawn, out cmdr, out team);

        var newFen = DsFen.GetFen(newSpawn, cmdr, team);

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

        string fen = DsFen.GetFen(spawn, cmdr, team);
        Assert.IsNotNull(fen);
        Assert.IsTrue(fen.Length > 10);

        SpawnDto reSpawn = new SpawnDto();
        DsFen.ApplyFen(fen, reSpawn, out cmdr, out team);
        Assert.AreEqual(Commander.Terran, cmdr);
        Assert.AreEqual(1, team);
        Assert.IsNotNull(reSpawn.Units);
        Assert.IsTrue(reSpawn.Units.Count > 0);
        var rePoints = DsFen.GetPoints(reSpawn.Units.First().Poss).OrderBy(o => o.X).ThenBy(o => o.Y).ToList();
        Assert.AreEqual(points.Count, rePoints.Count);
        for (int i = 0; i < points.Count; i++)
        {
            Assert.AreEqual(points[i], rePoints[i]);
        }
    }

}
