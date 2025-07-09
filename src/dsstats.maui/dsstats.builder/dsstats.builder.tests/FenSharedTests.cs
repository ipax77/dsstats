using dsstats.shared;
using dsstats.shared.DsFen;

namespace dsstats.builder.tests;

[TestClass]
public sealed class FenSharedTests
{
    // [TestMethod]
    // public void FenRoundTrip_FromSpawnDto()
    // {
    //     var spawn = new SpawnDto
    //     {
    //         Units = new List<SpawnUnitDto>
    //     {
    //         new() { Unit = new() { Name = "Zergling" }, Poss = "91,80" },
    //         new() { Unit = new() { Name = "Mutalisk" }, Poss = "92,81" },
    //     }
    //     };

    //     var cmdr = Commander.Zerg;
    //     int team = 2;

    //     var fen = DsFen.GetFen(spawn, cmdr, team);

    //     var newSpawn = new SpawnDto { Units = [] };
    //     DsFen.ApplyFen(fen, newSpawn, out cmdr, out team);

    //     var newFen = DsFen.GetFen(newSpawn, cmdr, team);

    //     Assert.AreEqual(fen, newFen);
    // }

    [TestMethod]
    public void CanApplyRlFenString()
    {
        string fen = "2:Terran;10q15/9q16/8q17/7q18/6q15q3/5q17w2/4q18qe1/3q19qqw/2q20qq1/1q20qq2/q20qq3/19eqq4/19qw5/18qq6/17qq7/16qq8/15qq9/14qq10/12eqq11/11qqw12/11qq13/5q4qq14/6wqqqq15/7eqq16/8w17|26/19z6/26/26/26/26/26/26/26/21d4/26/19d6/26/15f1d8/26/15d10/26/13d12/2z23/11d14/26/9d16/26/26/26";
        var cmdr = Commander.None;
        int team = 0;
        var spawn = new SpawnDto();
        DsFen.ApplyFen(fen, spawn, out cmdr, out team);

        var marines = spawn.Units
            .Where(u => u.Unit.Name == "Marine")
            .FirstOrDefault();
        Assert.IsNotNull(marines);
        Assert.AreEqual(49, marines.Count);
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
