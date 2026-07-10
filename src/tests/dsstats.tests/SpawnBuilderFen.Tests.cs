using dsstats.shared;
using dsstats.shared.Builder;

namespace dsstats.tests;

[TestClass]
public sealed class SpawnBuilderFenTests
{
    [TestMethod]
    [DataRow(Commander.Protoss, 2, "Stalker", "Carrier", 84, 93)]
    [DataRow(Commander.Terran, 1, "MarineLightweight", "Battlecruiser", 165, 174)]
    [DataRow(Commander.Zerg, 2, "Roach", "Mutalisk", 84, 93)]
    public void RoundTripPreservesStandardCommanderSpawn(
        Commander commander,
        int team,
        string groundName,
        string airName,
        int x,
        int y)
    {
        SpawnDto spawn = new()
        {
            Units =
            [
                new() { Name = groundName, Count = 1, Positions = [x, y] },
                new() { Name = airName, Count = 1, Positions = [x, y] }
            ]
        };

        var fen = SpawnBuilderFen.Encode(commander, team, spawn);
        var decoded = SpawnBuilderFen.Decode(fen);

        Assert.AreEqual(commander, decoded.Commander);
        Assert.AreEqual(team, decoded.Team);
        Assert.AreEqual(2, decoded.Spawn.Units.Count);
        foreach (var unit in decoded.Spawn.Units)
        {
            Assert.AreEqual(1, unit.Count);
            CollectionAssert.AreEqual(new[] { x, y }, unit.Positions);
        }
    }

    [TestMethod]
    public void EncodeUsesCompactRunLengthRows()
    {
        SpawnDto spawn = new()
        {
            Units = [new() { Name = "Zealot", Count = 1, Positions = [84, 93] }]
        };

        var fen = SpawnBuilderFen.Encode(Commander.Protoss, 2, spawn);

        StringAssert.StartsWith(fen, "DSF1 1 2 ");
        Assert.IsTrue(fen.Length < 180);
        Assert.IsTrue(SpawnBuilderFen.TryDecode(fen, out _));
    }

    [TestMethod]
    public void DecodeRejectsUnsupportedOrMalformedFen()
    {
        Assert.IsFalse(SpawnBuilderFen.TryDecode("DSF2 1 2 25|25", out _));
        Assert.IsFalse(SpawnBuilderFen.TryDecode("DSF1 10 2 25|25", out _));
        Assert.IsFalse(SpawnBuilderFen.TryDecode("DSF1 1 3 25|25", out _));
    }

    [TestMethod]
    public void MirroringTwiceRestoresSpawn()
    {
        SpawnDto spawn = new()
        {
            Units = [new() { Name = "Stalker", Count = 2, Positions = [84, 93, 90, 87] }]
        };
        var fen = SpawnBuilderFen.Encode(Commander.Protoss, 2, spawn);

        var restored = SpawnBuilderFen.Decode(SpawnBuilderFen.Mirror(SpawnBuilderFen.Mirror(fen)));

        Assert.AreEqual(2, restored.Team);
        CollectionAssert.AreEquivalent(spawn.Units[0].Positions!, restored.Spawn.Units[0].Positions!);
    }

    [TestMethod]
    public void TerranToggleFormsRemainDistinctAndUseCorrectLayers()
    {
        SpawnDto spawn = new()
        {
            Units =
            [
                new() { Name = "Thor", Count = 1, Positions = [84, 93] },
                new() { Name = "ThorAP", Count = 1, Positions = [85, 92] },
                new() { Name = "VikingFighter", Count = 1, Positions = [84, 93] },
                new() { Name = "VikingAssault", Count = 1, Positions = [86, 91] }
            ]
        };

        var fen = SpawnBuilderFen.Encode(Commander.Terran, 2, spawn);
        var decoded = SpawnBuilderFen.Decode(fen);

        CollectionAssert.AreEquivalent(
            new[] { "Thor", "ThorAP", "Viking", "VikingAssault" },
            decoded.Spawn.Units.Select(unit => unit.Name).ToArray());
        Assert.IsTrue(BuilderUnitCatalog.TryGetUnit(Commander.Terran, "Viking", out var fighter));
        Assert.IsTrue(fighter.IsAir);
        Assert.IsTrue(fighter.IsDefaultToggleState);
        Assert.IsTrue(BuilderUnitCatalog.TryGetUnit(Commander.Terran, "VikingAssault", out var assault));
        Assert.IsFalse(assault.IsAir);
        Assert.IsFalse(assault.IsDefaultToggleState);
        Assert.IsTrue(BuilderUnitCatalog.TryGetUnit(Commander.Terran, "ThorAP", out var thorAp));
        Assert.IsFalse(thorAp.IsDefaultToggleState);
    }

    [TestMethod]
    public void RoundTripPreservesBuilderUpgradesInAcquisitionOrder()
    {
        SpawnDto spawn = new()
        {
            Units = [new() { Name = "Marine", Count = 1, Positions = [84, 93] }]
        };
        UpgradeDto[] upgrades =
        [
            new() { Name = "ShieldWall", Gameloop = 120 },
            new() { Name = "StimPack", Gameloop = 180 },
            new() { Name = "TerranInfantryWeaponsLevel1", Gameloop = 240 }
        ];

        var fen = SpawnBuilderFen.Encode(Commander.Terran, 2, spawn, upgrades);
        var decoded = SpawnBuilderFen.Decode(fen);

        CollectionAssert.AreEqual(
            new[] { "ShieldWall", "Stimpack", "TerranInfantryWeaponsLevel1" },
            decoded.Upgrades.Select(upgrade => upgrade.Name).ToArray());
    }

    [TestMethod]
    public void SpawnPlaybackAdvertisesBuilderFenVersion()
    {
        SpawnPlaybackInfoDto info = new();

        Assert.AreEqual(SpawnBuilderFen.FormatVersion, info.BuilderFenVersion);
    }
}
