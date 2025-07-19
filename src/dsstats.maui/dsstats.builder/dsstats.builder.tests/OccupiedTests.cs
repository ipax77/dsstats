using dsstats.shared.DsFen;

namespace dsstats.builder.tests;

[TestClass]
public sealed class OccupiedTests
{
    // build area team1
    private List<RlPoint> polygon1 =
    [
        new RlPoint(154, 163),   // Left
        new RlPoint(165, 174),    // Top
        new RlPoint(182, 157),  // Right
        new RlPoint(171, 146),   // Bottom
    ];

    private List<RlPoint> normalizedPolygon = new()
    {
        new RlPoint(0, 0),         // Top
        new RlPoint(17, -17),      // Right
        new RlPoint(6, -28),       // Bottom
        new RlPoint(-11, -11),    // Left
    };

    [TestMethod]
    public void Footprint_1x1_Centered()
    {
        var buildArea = new BuildArea(1);
        var center = new RlPoint(0, 0);
        var footprint = buildArea.GetFootprint(center, 1);

        var expected = new List<RlPoint>
        {
            new RlPoint(0, 0)
        };

        CollectionAssert.AreEquivalent(expected, footprint);
    }

    [TestMethod]
    public void Footprint_2x2_BottomRightCorner()
    {
        var buildArea = new BuildArea(1);
        var center = new RlPoint(0, 0); // bottom-right of 2x2
        var footprint = buildArea.GetFootprint(center, 2);

        var expected = new List<RlPoint>
        {
            new RlPoint(-1, 0),
            new RlPoint(-1, 1),
            new RlPoint(0, 0),
            new RlPoint(0, 1)
        };

        CollectionAssert.AreEquivalent(expected, footprint);
    }

    [TestMethod]
    public void Footprint_3x3_Centered()
    {
        var buildArea = new BuildArea(1);
        var center = new RlPoint(0, 0);
        var footprint = buildArea.GetFootprint(center, 3);

        var expected = new List<RlPoint>
        {
            new RlPoint(-1, -1), new RlPoint(0, -1), new RlPoint(1, -1),
            new RlPoint(-1,  0), new RlPoint(0,  0), new RlPoint(1,  0),
            new RlPoint(-1,  1), new RlPoint(0,  1), new RlPoint(1,  1),
        };

        CollectionAssert.AreEquivalent(expected, footprint);
    }

    [TestMethod]
    public void Footprint_4x4_BottomRightOfCentral2x2()
    {
        var buildArea = new BuildArea(1);
        var center = new RlPoint(0, 0); // bottom-right of central 2x2
        var footprint = buildArea.GetFootprint(center, 4);

        var expected = new List<RlPoint>
        {
            new RlPoint(-2, 2), new RlPoint(-2, 1), new RlPoint(-2, 0), new RlPoint(-2, -1),
            new RlPoint(-1, 2), new RlPoint(-1, 1), new RlPoint(-1, 0), new RlPoint(-1, -1),
            new RlPoint(0, 2), new RlPoint(0, 1), new RlPoint( 0, 0), new RlPoint(0, -1),
            new RlPoint(1, 2), new RlPoint(1, 1), new RlPoint( 1, 0), new RlPoint( 1, -1),
        };

        CollectionAssert.AreEquivalent(expected, footprint);
    }

    [TestMethod]
    public void CanRepositionUnits()
    {
        int team = 1;
        var buildArea = new BuildArea(team);
        buildArea.PlaceUnits("Stalker", "170,149,171,148", team); // x,y coordinates, Stalker: size 2x2
        var build = CmdrBuildFactory.Create(shared.Commander.Protoss);
        ArgumentNullException.ThrowIfNull(build);
        var buildUnits = buildArea.GetBuildUnits(build, team); // normalized
        // pos1: (5, -25);
        // pos2: (6, -26);
        var fixedBuildUnints = buildArea.FixUnitPositions(buildUnits);
        Assert.AreEqual(2, fixedBuildUnints.Count);
        var pos1 = fixedBuildUnints[0].Pos;
        var pos2 = fixedBuildUnints[1].Pos;
        var distance = pos1.DistanceTo(pos2);
        Assert.IsTrue(distance >= 2.0, $"Units overlapping: {pos1.X},{pos1.Y} => {pos2.X},{pos2.Y}");
    }

    [TestMethod]
    public void CanHandleMultipleUnitsWithDifferentSizes()
    {
        int team = 1;
        var buildArea = new BuildArea(team);
        // Place mixed units with size 1 and 2
        buildArea.PlaceUnits("Zealot", "168,151,169,152", team); // size 1x1
        buildArea.PlaceUnits("Stalker", "170,149,171,148", team); // size 2x2

        var build = CmdrBuildFactory.Create(shared.Commander.Protoss);
        ArgumentNullException.ThrowIfNull(build);
        var buildUnits = buildArea.GetBuildUnits(build, team);

        var fixedBuildUnits = buildArea.FixUnitPositions(buildUnits);

        // Check no overlap and all units inside polygon
        Assert.IsTrue(fixedBuildUnits.Count >= 4);

        foreach (var unitA in fixedBuildUnits)
        {
            foreach (var unitB in fixedBuildUnits)
            {
                if (unitA == unitB) continue;
                Assert.IsTrue(unitA.Pos.DistanceTo(unitB.Pos) >= 1.0,
                    $"Units overlapping at {unitA.Pos} and {unitB.Pos}");
            }
        }
    }

    [TestMethod]
    public void UnitsNearPolygonEdgeAreRepositionedInside()
    {
        int team = 1;
        var buildArea = new BuildArea(team);

        // Intentionally place units partially outside the polygon boundary
        buildArea.PlaceUnits("Stalker", "180,160,181,159", team); // near right edge
        buildArea.PlaceUnits("Stalker", "154,163,155,162", team); // near left edge

        var build = CmdrBuildFactory.Create(shared.Commander.Protoss);
        ArgumentNullException.ThrowIfNull(build);
        var buildUnits = buildArea.GetBuildUnits(build, team);

        var fixedBuildUnits = buildArea.FixUnitPositions(buildUnits);

        // All units should be inside polygon and not overlapping
        var polygonSet = BuildArea.GetPointsInPolygon(normalizedPolygon);

        foreach (var unit in fixedBuildUnits)
        {
            Assert.IsTrue(polygonSet.ContainsKey(unit.Pos), $"Unit {unit.Pos} outside polygon");
        }

        // Check distance between units to avoid overlap
        for (int i = 0; i < fixedBuildUnits.Count; i++)
        {
            for (int j = i + 1; j < fixedBuildUnits.Count; j++)
            {
                Assert.IsTrue(fixedBuildUnits[i].Pos.DistanceTo(fixedBuildUnits[j].Pos) >= 2.0,
                    $"Units overlapping: {fixedBuildUnits[i].Pos} and {fixedBuildUnits[j].Pos}");
            }
        }
    }

    [TestMethod]
    public void CanHandleOverlappingUnitsRepositioning()
    {
        int team = 1;
        var buildArea = new BuildArea(team);

        // Overlapping units initially
        buildArea.PlaceUnits("Stalker", "170,149,171,148", team); // two 2x2 units overlapping exactly
        buildArea.PlaceUnits("Immortal", "170,149,171,148", team);

        var build = CmdrBuildFactory.Create(shared.Commander.Protoss);
        ArgumentNullException.ThrowIfNull(build);
        var buildUnits = buildArea.GetBuildUnits(build, team);

        var fixedBuildUnits = buildArea.FixUnitPositions(buildUnits);

        Assert.AreEqual(4, fixedBuildUnits.Count); // 2 units * 2 (assuming each unit has two build points)

        for (int i = 0; i < fixedBuildUnits.Count; i++)
        {
            for (int j = i + 1; j < fixedBuildUnits.Count; j++)
            {
                Assert.IsTrue(fixedBuildUnits[i].Pos.DistanceTo(fixedBuildUnits[j].Pos) >= 2.0,
                    $"Units overlapping: {fixedBuildUnits[i].Pos} and {fixedBuildUnits[j].Pos}");
            }
        }
    }

    [TestMethod]
    public void CanHandleAirAndGroundUnitsTogether()
    {
        int team = 1;
        var buildArea = new BuildArea(team);

        buildArea.PlaceUnits("Phoenix", "168,153", team); // air unit, size 1x1
        buildArea.PlaceUnits("Stalker", "170,149,171,148", team); // ground unit, size 2x2

        var build = CmdrBuildFactory.Create(shared.Commander.Protoss);
        ArgumentNullException.ThrowIfNull(build);
        var buildUnits = buildArea.GetBuildUnits(build, team);

        var fixedBuildUnits = buildArea.FixUnitPositions(buildUnits);

        // Check air and ground units do not overlap and are inside polygon
        var polygonSet = BuildArea.GetPointsInPolygon(normalizedPolygon);

        foreach (var unit in fixedBuildUnits)
        {
            Assert.IsTrue(polygonSet.ContainsKey(unit.Pos), $"Unit {unit.Pos} outside polygon");
        }

        for (int i = 0; i < fixedBuildUnits.Count; i++)
        {
            for (int j = i + 1; j < fixedBuildUnits.Count; j++)
            {
                Assert.IsTrue(fixedBuildUnits[i].Pos.DistanceTo(fixedBuildUnits[j].Pos) >= 1.0,
                    $"Units overlapping: {fixedBuildUnits[i].Pos} and {fixedBuildUnits[j].Pos}");
            }
        }
    }

    [TestMethod]
    public void UnitsInitiallyNonOverlappingRemainUnchanged()
    {
        int team = 1;
        var buildArea = new BuildArea(team);
        buildArea.PlaceUnits("Zealot", "160,155", team); // size 1x1
        buildArea.PlaceUnits("Stalker", "165,160,167,159", team); // size 2x2

        var build = CmdrBuildFactory.Create(shared.Commander.Protoss);
        ArgumentNullException.ThrowIfNull(build);
        var buildUnits = buildArea.GetBuildUnits(build, team);

        var originalPositions = buildUnits
            .Select(u => u.Pos)
            .OrderBy(p => p.X)
            .ThenBy(p => p.Y)
            .ToList();

        var fixedBuildUnits = buildArea.FixUnitPositions(buildUnits);

        var fixedPositions = fixedBuildUnits
            .Select(u => u.Pos)
            .OrderBy(p => p.X)
            .ThenBy(p => p.Y)
            .ToList();

        Assert.AreEqual(originalPositions.Count, fixedPositions.Count);

        for (int i = 0; i < fixedPositions.Count; i++)
        {
            Assert.AreEqual(originalPositions[i], fixedPositions[i],
                $"Unit {i} position changed unnecessarily: expected {originalPositions[i]}, got {fixedPositions[i]}");
        }
    }
}