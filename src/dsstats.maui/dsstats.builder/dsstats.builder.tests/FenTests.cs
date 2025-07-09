using dsstats.shared.DsFen;

namespace dsstats.builder.tests;

[TestClass]
public class FenTests
{
    [TestMethod]
    public void CanGetFenString()
    {
        DsFenGrid grid = new()
        {
            Team = 1,
            Commander = shared.Commander.Protoss,
            Units = new Dictionary<BuildOption, List<DsPoint>>
            {
                { new('q'), new List<DsPoint> { new(1, 1), new(1, 2) } },
            }
        };
        string fen = DsFenBuilder.GetFenString(grid);
        Assert.IsNotNull(fen);
    }

    [TestMethod]
    public void CanRoundTripFen()
    {
        var buildOption = new BuildOption('q');
        DsFenGrid grid = new()
        {
            Team = 1,
            Commander = shared.Commander.Protoss,
            Units = new Dictionary<BuildOption, List<DsPoint>>
            {
                { buildOption, new List<DsPoint> { new(1, 1), new(1, 2) } },
            }
        };
        string fen = DsFenBuilder.GetFenString(grid);
        DsFenGrid newGrid = DsFenBuilder.GetGridFromString(fen);
        Assert.AreEqual(grid.Team, newGrid.Team);
        Assert.AreEqual(grid.Commander, newGrid.Commander);
        CollectionAssert.AreEquivalent(grid.Units[buildOption], newGrid.Units[buildOption]);
    }

    [TestMethod]
    public void CanGetFenString_WithAirUnits()
    {
        DsFenGrid grid = new()
        {
            Team = 2,
            Commander = shared.Commander.Zerg,
            Units = new Dictionary<BuildOption, List<DsPoint>>
        {
            { new('g', IsAir: false), new List<DsPoint> { new(5, 5) } },
            { new('a', IsAir: true), new List<DsPoint> { new(5, 5) } },
        }
        };

        string fen = DsFenBuilder.GetFenString(grid);
        Assert.IsNotNull(fen);
        Assert.IsTrue(fen.Contains("|"));
    }

    [TestMethod]
    public void CanRoundTripFen_WithAirUnits()
    {
        var groundOption = new BuildOption('g', IsAir: false);
        var airOption = new BuildOption('a', IsAir: true);

        DsFenGrid grid = new()
        {
            Team = 3,
            Commander = shared.Commander.Terran,
            Units = new Dictionary<BuildOption, List<DsPoint>>
        {
            { groundOption, new List<DsPoint> { new(10, 10) } },
            { airOption, new List<DsPoint> { new(10, 10), new(2, 3) } }
        }
        };

        string fen = DsFenBuilder.GetFenString(grid);
        DsFenGrid parsed = DsFenBuilder.GetGridFromString(fen);

        Assert.AreEqual(grid.Team, parsed.Team);
        Assert.AreEqual(grid.Commander, parsed.Commander);
        Assert.IsTrue(parsed.Units.ContainsKey(groundOption));
        Assert.IsTrue(parsed.Units.ContainsKey(airOption));
        CollectionAssert.AreEquivalent(grid.Units[groundOption], parsed.Units[groundOption]);
        CollectionAssert.AreEquivalent(grid.Units[airOption], parsed.Units[airOption]);
    }

}
