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

}
