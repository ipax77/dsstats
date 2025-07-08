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
        int team = 2;
        SpawnDto spawn = new()
        {
            Units = new List<SpawnUnitDto>
            {
                new() { Unit = new() { Name = "Marine" }, Poss = "93,84,95,82,94,83,92,85,91,86,98,79,99,78,88,89,97,80,90,87,87,90,85,92,86,91,85,91,84,92,100,77,100,76,99,77,86,90,87,89,98,78,97,79,96,80,88,88,89,87,95,81,94,82,90,86,93,83,92,84,91,85,83,91,84,91,99,76,99,75,81,90,98,73,87,88,79,76,80,75,81,74,82,73,83,72,84,71,85,70,78,77,77,78,86,69,76,79" },
            }
        };
        var positions = spawn.Units.First().Poss.Split(',').Select(int.Parse).OrderBy(o => o).ToList();
        string fen = DsFen.GetFen(spawn, cmdr, team);
        Assert.IsNotNull(fen);
        Assert.IsTrue(fen.Length > 0);

        var reSpawn = new SpawnDto();
        DsFen.ApplyFen(fen, reSpawn, out cmdr, out team);
        Assert.AreEqual(Commander.Terran, cmdr);
        Assert.AreEqual(2, team);
        Assert.IsNotNull(reSpawn.Units);
        Assert.IsTrue(reSpawn.Units.Count > 0);
        var rePositions = reSpawn.Units.First().Poss.Split(',').Select(int.Parse).OrderBy(o => o).ToList();
        Assert.AreEqual(positions.Count, rePositions.Count);
        for (int i = 0; i < positions.Count; i++)
        {
            Assert.AreEqual(positions[i], rePositions[i]);
        }
    }

}
