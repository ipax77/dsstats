using dsstats.shared;
using dsstats.shared.Builder;

namespace dsstats.tests;

[TestClass]
public sealed class SpawnBuilderFenTests
{
    [TestMethod]
    [DataRow(1, 165, 174, 11, 28)]
    [DataRow(1, 182, 157, 28, 11)]
    [DataRow(1, 171, 146, 17, 0)]
    [DataRow(1, 154, 163, 0, 17)]
    [DataRow(2, 84, 93, 11, 28)]
    public void GridCornersRoundTrip(int team, int mapX, int mapY, int cellX, int cellY)
    {
        Assert.IsTrue(SpawnBuilderFen.TryGetCell(team, mapX, mapY, out var cell));
        Assert.AreEqual(new SpawnBuilderFen.Cell(cellX, cellY), cell);
        Assert.IsTrue(SpawnBuilderFen.TryGetMapPosition(team, cell, out var restoredX, out var restoredY));
        Assert.AreEqual(mapX, restoredX);
        Assert.AreEqual(mapY, restoredY);
    }

    [TestMethod]
    public void Editor_UsesSeparateGroundAndAirCollisionLayers()
    {
        var editor = new SpawnBuildEditor(Commander.Terran, 1);
        var marine = BuilderUnitCatalog.GetUnits(Commander.Terran).Single(u => u.Name == "Marine");
        var medivac = BuilderUnitCatalog.GetUnits(Commander.Terran).Single(u => u.Name == "Medivac");

        Assert.IsTrue(editor.Execute(new AddUnit(marine, new(14, 14)), out _));
        Assert.IsTrue(editor.Execute(new AddUnit(medivac, new(14, 14)), out _));
        Assert.IsFalse(editor.Execute(new AddUnit(marine, new(14, 14)), out var error));
        StringAssert.Contains(error, "collides");
    }

    [TestMethod]
    public void Editor_RejectsFootprintsOutsideBoundary()
    {
        var editor = new SpawnBuildEditor(Commander.Protoss, 1);
        var carrier = BuilderUnitCatalog.GetUnits(Commander.Protoss).Single(u => u.Name == "Carrier");

        Assert.IsFalse(editor.Execute(new AddUnit(carrier, new(24, 16)), out var error));
        StringAssert.Contains(error, "outside");
    }

    [TestMethod]
    public void Editor_RoundTripsThroughFen()
    {
        var editor = new SpawnBuildEditor(Commander.Zerg, 2);
        var zergling = BuilderUnitCatalog.GetUnits(Commander.Zerg).Single(u => u.Name == "Zergling");
        Assert.IsTrue(editor.Execute(new AddUnit(zergling, new(14, 14)), out _));

        var fen = SpawnBuilderFen.Encode(editor.Commander, editor.Team, editor.ToSpawn());
        var restored = SpawnBuildEditor.From(SpawnBuilderFen.Decode(fen));

        Assert.AreEqual(1, restored.Units.Count);
        Assert.AreEqual(new BuildCell(14, 14), restored.Units[0].Cell);
    }
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
        Assert.IsTrue(fen.Length < 300);
        Assert.IsTrue(SpawnBuilderFen.TryDecode(fen, out _));
    }

    [TestMethod]
    public void DecodeRejectsUnsupportedOrMalformedFen()
    {
        Assert.IsFalse(SpawnBuilderFen.TryDecode(null, out _));
        Assert.IsFalse(SpawnBuilderFen.TryDecode(string.Empty, out _));
        Assert.IsFalse(SpawnBuilderFen.TryDecode("   ", out _));
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
    public void MirroringBuilderRequestPreservesUpgradesAndPreparation()
    {
        BuilderRequest request = new(
            Commander.Terran,
            1,
            new SpawnDto
            {
                Units = [new() { Name = "Marine", Count = 1, Positions = [165, 174] }]
            },
            [
                new UpgradeDto { Name = "Stimpack", Gameloop = 180 },
                new UpgradeDto { Name = "TerranInfantryWeaponsLevel1", Gameloop = 240 }
            ],
            new BuilderPreparationOptions(ResetResearch: true),
            Mirror: true);

        var mirrored = SpawnBuilderFen.Mirror(request);

        Assert.AreEqual(2, mirrored.Team);
        Assert.IsFalse(mirrored.Mirror);
        CollectionAssert.AreEqual(
            new[] { "Stimpack", "TerranInfantryWeaponsLevel1" },
            mirrored.Upgrades!.Select(upgrade => upgrade.Name).ToArray());
        Assert.IsTrue(mirrored.Preparation!.ResetResearch);
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
