using System.Reflection;
using s2protocol.NET;
using s2protocol.NET.Models;
using Sc2DirectStrike.Parser;

namespace Sc2DirectStrike.Tests;

public sealed partial class ParseTests
{
    [TestMethod]
    [DataRow("testdata/Direct Strike (10060).SC2Replay")]
    [DataRow("testdata/Direct Strike (10096).SC2Replay")]
    [DataRow("testdata/Direct Strike (10124).SC2Replay")]
    [DataRow("testdata/Direct Strike (10143).SC2Replay")]
    [DataRow("testdata/Direct Strike (600).SC2Replay")]
    [DataRow("testdata/Direct Strike TE (999).SC2Replay")]
    [DataRow("testdata/Direct Strike TE (1904).SC2Replay")]
    [DataRow("testdata/Direct Strike TE (1910).SC2Replay")]
    public async Task CanSetPlayerSpawnsFromTrackerEvents(string replayName)
    {
        Sc2Replay replay = await GetReplay(replayName);

        DirectStrikeReplay dsReplay = Sc2DirectStrikeParser.Parse(replay);

        Assert.IsTrue(dsReplay.Players.Any(player => player.Spawns.Count > 0));
        foreach (DirectStrikePlayer player in dsReplay.Players)
        {
            Assert.IsTrue(player.Spawns.Select(spawn => spawn.StartGameloop).SequenceEqual(player.Spawns.Select(spawn => spawn.StartGameloop).Order()));
            for (int i = 0; i < player.Spawns.Count; i++)
            {
                DirectStrikePlayerSpawn spawn = player.Spawns[i];
                Assert.IsNotEmpty(spawn.Units);
                Assert.AreEqual(i + 1, spawn.Number);
                Assert.AreEqual(spawn.Units.Min(unit => unit.Gameloop), spawn.StartGameloop);
                Assert.AreEqual(spawn.Units.Max(unit => unit.Gameloop), spawn.EndGameloop);
                Assert.IsLessThanOrEqualTo(112, spawn.EndGameloop - spawn.StartGameloop);
                Assert.IsTrue(spawn.Units.Select(unit => unit.Gameloop).SequenceEqual(spawn.Units.Select(unit => unit.Gameloop).Order()));
                Assert.IsTrue(spawn.Units.All(unit => !string.IsNullOrWhiteSpace(unit.Name)));
            }
        }
    }

    [TestMethod]
    public async Task CanSetBrawlPlayerSpawnsAsPerPlayerWaves()
    {
        Sc2Replay replay = await GetReplay("testdata/Direct Strike (10096).SC2Replay");

        DirectStrikeReplay dsReplay = Sc2DirectStrikeParser.Parse(replay);

        Assert.AreEqual(GameMode.BrawlCommanders, dsReplay.GameMode);
        DirectStrikePlayerSpawn[] firstPlayerSpawns = [.. dsReplay.Players
            .Where(player => player.Spawns.Count > 0)
            .Select(player => player.Spawns[0])];
        Assert.IsGreaterThanOrEqualTo(2, firstPlayerSpawns.Length);
    }

    [TestMethod]
    public async Task IncludesAbathurSwarmHostsInKitchenSinkBrawl()
    {
        Sc2Replay replay = await GetReplay("testdata/Direct Strike (10915).SC2Replay");

        DirectStrikeReplay dsReplay = Sc2DirectStrikeParser.Parse(replay);
        ReplayDto dto = DirectStrikeReplayDtoMapper.Map(replay, dsReplay);

        Assert.AreEqual(GameMode.BrawlCommanders, dsReplay.GameMode);

        DirectStrikePlayer pax = dsReplay.Players.Single(player => player.Name == "PAX");
        Assert.AreEqual(Commander.Abathur, pax.Commander);
        Assert.AreEqual(5, pax.GamePos);
        SpawnDto paxAll = dto.Players
            .Single(player => player.Name == pax.Name)
            .Spawns
            .Single(spawn => spawn.Breakpoint == Breakpoint.All);
        Assert.AreEqual(5, paxAll.Units.Single(unit => unit.Name == "SwarmHostMP").Count);
        Assert.AreEqual(3, paxAll.Units.Single(unit => unit.Name == "SwarmHostMP").Special);

        DirectStrikePlayer mourissou = dsReplay.Players.Single(player => player.Name == "Mourissou");
        Assert.AreEqual(Commander.Abathur, mourissou.Commander);
        Assert.AreEqual(3, mourissou.GamePos);
        SpawnDto mourissouAll = dto.Players
            .Single(player => player.Name == mourissou.Name)
            .Spawns
            .Single(spawn => spawn.Breakpoint == Breakpoint.All);
        Assert.AreEqual(9, mourissouAll.Units.Single(unit => unit.Name == "SwarmHostMP").Count);
    }

    [TestMethod]
    public async Task IncludesCanonicalEndscreenSpawnNamesAndExcludesUnitProducedEntities()
    {
        Sc2Replay replay = await GetReplay("testdata/Direct Strike (600).SC2Replay");

        DirectStrikeReplay dsReplay = Sc2DirectStrikeParser.Parse(replay);

        DirectStrikeSpawnUnit[] odessaUnits = [.. dsReplay.Players
            .Single(player => player.Name == "Odessa")
            .Spawns.SelectMany(spawn => spawn.Units)];
        Assert.HasCount(440, odessaUnits);
        Assert.HasCount(28, odessaUnits.Where(unit => unit.Name == "SiegeTank"));

        DirectStrikeSpawnUnit[] heronUnits = [.. dsReplay.Players
            .Single(player => player.Name == "Heron")
            .Spawns.SelectMany(spawn => spawn.Units)];
        Assert.HasCount(385, heronUnits);
        Assert.HasCount(108, heronUnits.Where(unit => unit.Name == "SwarmlingLightweight"));

        Assert.IsFalse(dsReplay.Players
            .SelectMany(player => player.Spawns)
            .SelectMany(spawn => spawn.Units)
            .Any(unit => unit.Name is "ReaverStarlightDummy" or "PurifierInterceptor" or "ClolarionBomber"));
    }

    [TestMethod]
    public async Task IncludesCanonicalCommanderSuffixedSpawnUnitNames()
    {
        Sc2Replay replay = await GetReplay("testdata/Direct Strike (10147).SC2Replay");

        DirectStrikeReplay dsReplay = Sc2DirectStrikeParser.Parse(replay);

        int[] swannSiegeTankCounts = [.. dsReplay.Players
            .Where(player => player.Commander == Commander.Swann)
            .Select(player => player.Spawns
                .SelectMany(spawn => spawn.Units)
                .Count(unit => unit.Name == "SiegeTank"))
            .Order()];
        CollectionAssert.AreEqual(new[] { 23, 32 }, swannSiegeTankCounts);
    }

    [TestMethod]
    [DataRow("testdata/Direct Strike TE (999).SC2Replay")]
    public async Task CanExposePlayerBuildUnitNamesFromTrackerEvents(string replayName)
    {
        Sc2Replay replay = await GetReplay(replayName);

        DirectStrikeReplay dsReplay = Sc2DirectStrikeParser.Parse(replay);

        Dictionary<int, int> playerIndexesByControlPlayerId = GetPlayerIndexesByControlPlayerId(replay, dsReplay);
        ExpectedPlayerLayout[] layouts = GetExpectedPlayerLayouts(replay, dsReplay, playerIndexesByControlPlayerId);
        HashSet<string>[] expectedBuildUnitNamesByPlayer = GetExpectedBuildUnitNamesByPlayer(replay, dsReplay, playerIndexesByControlPlayerId, layouts);

        for (int i = 0; i < dsReplay.Players.Count; i++)
        {
            CollectionAssert.AreEquivalent(
                expectedBuildUnitNamesByPlayer[i].Order(StringComparer.Ordinal).ToArray(),
                dsReplay.Players[i].BuildUnitNames.ToArray());
        }

        DirectStrikePlayer pax = dsReplay.Players.Single(player => player.Name == "PAX");
        CollectionAssert.Contains(pax.BuildUnitNames.ToArray(), "Marine");
        DirectStrikePlayer apache = dsReplay.Players.Single(player => player.Name == "Apache");
        CollectionAssert.Contains(apache.BuildUnitNames.ToArray(), "Hydralisk");
        CollectionAssert.Contains(apache.BuildUnitNames.ToArray(), "LurkerMP");

        ReplayPlayerDto paxDto = DsstatsParser.ParseReplay(replay).Players.Single(player => player.Name == "PAX");
        SpawnDto min5 = paxDto.Spawns.Single(spawn => spawn.Breakpoint == Breakpoint.Min5);
        Assert.IsNotNull(min5.Units.SingleOrDefault(unit => unit.Name == "MarineLightweight"));
        ReplayPlayerDto apacheDto = DsstatsParser.ParseReplay(replay).Players.Single(player => player.Name == "Apache");
        Assert.IsTrue(apacheDto.Spawns.Single(spawn => spawn.Breakpoint == Breakpoint.Min10).Units.Any(unit => unit.Name == "LurkerMP"));
        Assert.IsTrue(apacheDto.Spawns.Single(spawn => spawn.Breakpoint == Breakpoint.All).Units.Any(unit => unit.Name == "LurkerMP"));
    }

    [TestMethod]
    [DataRow("testdata/Direct Strike (10060).SC2Replay")]
    [DataRow("testdata/Direct Strike (10096).SC2Replay")]
    [DataRow("testdata/Direct Strike (10124).SC2Replay")]
    [DataRow("testdata/Direct Strike (10143).SC2Replay")]
    [DataRow("testdata/Direct Strike (600).SC2Replay")]
    [DataRow("testdata/Direct Strike TE (999).SC2Replay")]
    [DataRow("testdata/Direct Strike TE (1904).SC2Replay")]
    [DataRow("testdata/Direct Strike TE (1910).SC2Replay")]
    public async Task SpawnUnitsRequireSamePlayerBuildAreaUnitType(string replayName)
    {
        Sc2Replay replay = await GetReplay(replayName);

        DirectStrikeReplay dsReplay = Sc2DirectStrikeParser.Parse(replay);

        Dictionary<int, int> playerIndexesByControlPlayerId = GetPlayerIndexesByControlPlayerId(replay, dsReplay);
        ExpectedPlayerLayout[] layouts = GetExpectedPlayerLayouts(replay, dsReplay, playerIndexesByControlPlayerId);
        HashSet<ExpectedSpawnUnit> exposedSpawnUnits = GetExposedSpawnUnits(dsReplay);
        HashSet<string>[] builtUnitNamesByPlayer = [.. Enumerable.Range(0, dsReplay.Players.Count).Select(_ => new HashSet<string>(StringComparer.Ordinal))];
        Dictionary<(int UnitTagIndex, int UnitTagRecycle), int> buildAreaPlayerIndexesByTag = [];
        List<ExpectedOrderedSpawnTrackerEvent> orderedTrackerEvents = GetExpectedOrderedSpawnTrackerEvents(replay);
        HashSet<ExpectedSpawnUnit> verifiedSpawnUnits = [];

        int index = 0;
        while (index < orderedTrackerEvents.Count)
        {
            int gameloop = orderedTrackerEvents[index].Gameloop;
            int nextIndex = index + 1;
            while (nextIndex < orderedTrackerEvents.Count && orderedTrackerEvents[nextIndex].Gameloop == gameloop)
            {
                nextIndex++;
            }

            for (int i = index; i < nextIndex; i++)
            {
                if (orderedTrackerEvents[i].BornEvent is { } bornEvent)
                {
                    TrackExpectedBuildUnitBornEvent(bornEvent, playerIndexesByControlPlayerId, layouts, buildAreaPlayerIndexesByTag, builtUnitNamesByPlayer);
                }
            }

            for (int i = index; i < nextIndex; i++)
            {
                if (orderedTrackerEvents[i].TypeChangeEvent is { } typeChangeEvent)
                {
                    TrackExpectedBuildUnitTypeChangeEvent(typeChangeEvent, buildAreaPlayerIndexesByTag, builtUnitNamesByPlayer);
                }
            }

            for (int i = index; i < nextIndex; i++)
            {
                if (orderedTrackerEvents[i].BornEvent is { } bornEvent)
                {
                    VerifyExpectedSpawnUnit(bornEvent, playerIndexesByControlPlayerId, dsReplay.Players, builtUnitNamesByPlayer, exposedSpawnUnits, verifiedSpawnUnits);
                }
            }

            index = nextIndex;
        }

        CollectionAssert.AreEquivalent(exposedSpawnUnits.ToArray(), verifiedSpawnUnits.ToArray());
    }

    [TestMethod]
    [DataRow("testdata/Direct Strike (10060).SC2Replay")]
    [DataRow("testdata/Direct Strike (10096).SC2Replay")]
    [DataRow("testdata/Direct Strike (10124).SC2Replay")]
    [DataRow("testdata/Direct Strike (10143).SC2Replay")]
    [DataRow("testdata/Direct Strike TE (1904).SC2Replay")]
    [DataRow("testdata/Direct Strike TE (1910).SC2Replay")]
    public async Task CanSetPlayerSpawnSummaryStatsFromPlayerStats(string replayName)
    {
        Sc2Replay replay = await GetReplay(replayName);

        DirectStrikeReplay dsReplay = Sc2DirectStrikeParser.Parse(replay);

        foreach (DirectStrikePlayer player in dsReplay.Players)
        {
            foreach (DirectStrikePlayerSpawn spawn in player.Spawns)
            {
                DirectStrikePlayerStats? expected = player.Stats.FirstOrDefault(stats => stats.Gameloop >= spawn.EndGameloop);
                Assert.AreSame(expected, spawn.SummaryStats);
                if (spawn.SummaryStats is { } summaryStats)
                {
                    Assert.IsGreaterThanOrEqualTo(spawn.EndGameloop, summaryStats.Gameloop);
                    Assert.IsFalse(player.Stats.Any(stats => stats.Gameloop >= spawn.EndGameloop && stats.Gameloop < summaryStats.Gameloop));
                }
            }
        }
    }

    [TestMethod]
    public void PolygonContainsInsideAndBoundaryPoints()
    {
        Type parserType = typeof(Sc2DirectStrikeParser);
        Type posType = parserType.GetNestedType("Pos", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not find private Pos type.");
        Type polygonType = parserType.GetNestedType("Polygon", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not find private Polygon type.");

        object polygon = CreateParserPolygon(posType, polygonType, new(165, 174), new(182, 157), new(171, 146), new(154, 163));

        Assert.IsTrue(InvokeParserPolygonContains(posType, polygonType, polygon, new(168, 160)));
        Assert.IsTrue(InvokeParserPolygonContains(posType, polygonType, polygon, new(165, 174)));
        Assert.IsTrue(InvokeParserPolygonContains(posType, polygonType, polygon, new(176, 152)));
        Assert.IsFalse(InvokeParserPolygonContains(posType, polygonType, polygon, new(150, 150)));
    }

    private static ExpectedPlayerLayout[] GetExpectedPlayerLayouts(
        Sc2Replay replay,
        DirectStrikeReplay dsReplay,
        Dictionary<int, int> playerIndexesByControlPlayerId)
    {
        ExpectedPlayerLayout[] layouts = [.. Enumerable.Range(0, dsReplay.Players.Count).Select(_ => new ExpectedPlayerLayout())];
        foreach (SUnitBornEvent bornEvent in replay.TrackerEvents?.SUnitBornEvents ?? [])
        {
            if (bornEvent.Gameloop != 0
                || !playerIndexesByControlPlayerId.TryGetValue(bornEvent.ControlPlayerId, out int playerIndex))
            {
                continue;
            }

            ExpectedPos position = new(bornEvent.X, bornEvent.Y);
            switch (bornEvent.UnitTypeName)
            {
                case "StagingAreaFootprintSouth":
                case "AreaMarkerSouth":
                    layouts[playerIndex].South = position;
                    break;
                case "StagingAreaFootprintWest":
                case "AreaMarkerWest":
                    layouts[playerIndex].West = position;
                    break;
                case "StagingAreaFootprintNorth":
                case "AreaMarkerNorth":
                    layouts[playerIndex].North = position;
                    break;
                case "StagingAreaFootprintEast":
                case "AreaMarkerEast":
                    layouts[playerIndex].East = position;
                    break;
            }
        }

        return layouts;
    }

    private static HashSet<ExpectedSpawnUnit> GetExposedSpawnUnits(DirectStrikeReplay dsReplay)
    {
        return [.. dsReplay.Players.SelectMany((player, playerIndex) => player.Spawns
            .SelectMany(spawn => spawn.Units)
            .Select(unit => new ExpectedSpawnUnit(
                playerIndex,
                unit.UnitIndex,
                unit.Name,
                unit.Gameloop,
                unit.X,
                unit.Y,
                unit.DiedGameloop,
                unit.DiedX,
                unit.DiedY)))];
    }

    private static HashSet<string>[] GetExpectedBuildUnitNamesByPlayer(
        Sc2Replay replay,
        DirectStrikeReplay dsReplay,
        Dictionary<int, int> playerIndexesByControlPlayerId,
        ExpectedPlayerLayout[] layouts)
    {
        HashSet<string>[] builtUnitNamesByPlayer = [.. Enumerable.Range(0, dsReplay.Players.Count).Select(_ => new HashSet<string>(StringComparer.Ordinal))];
        Dictionary<(int UnitTagIndex, int UnitTagRecycle), int> buildAreaPlayerIndexesByTag = [];
        List<ExpectedOrderedSpawnTrackerEvent> orderedTrackerEvents = GetExpectedOrderedSpawnTrackerEvents(replay);

        int index = 0;
        while (index < orderedTrackerEvents.Count)
        {
            int gameloop = orderedTrackerEvents[index].Gameloop;
            int nextIndex = index + 1;
            while (nextIndex < orderedTrackerEvents.Count && orderedTrackerEvents[nextIndex].Gameloop == gameloop)
            {
                nextIndex++;
            }

            for (int i = index; i < nextIndex; i++)
            {
                if (orderedTrackerEvents[i].BornEvent is { } bornEvent)
                {
                    TrackExpectedBuildUnitBornEvent(bornEvent, playerIndexesByControlPlayerId, layouts, buildAreaPlayerIndexesByTag, builtUnitNamesByPlayer);
                }
            }

            for (int i = index; i < nextIndex; i++)
            {
                if (orderedTrackerEvents[i].TypeChangeEvent is { } typeChangeEvent)
                {
                    TrackExpectedBuildUnitTypeChangeEvent(typeChangeEvent, buildAreaPlayerIndexesByTag, builtUnitNamesByPlayer);
                }
            }

            index = nextIndex;
        }

        return builtUnitNamesByPlayer;
    }

    private static List<ExpectedOrderedSpawnTrackerEvent> GetExpectedOrderedSpawnTrackerEvents(Sc2Replay replay)
    {
        ICollection<SUnitBornEvent> unitBornEvents = replay.TrackerEvents?.SUnitBornEvents ?? [];
        ICollection<SUnitTypeChangeEvent> unitTypeChangeEvents = replay.TrackerEvents?.SUnitTypeChangeEvents ?? [];
        List<ExpectedOrderedSpawnTrackerEvent> orderedTrackerEvents = new(unitBornEvents.Count + unitTypeChangeEvents.Count);
        int index = 0;
        foreach (SUnitBornEvent unitBornEvent in unitBornEvents)
        {
            orderedTrackerEvents.Add(new(unitBornEvent.Gameloop, index, unitBornEvent, null));
            index++;
        }

        foreach (SUnitTypeChangeEvent unitTypeChangeEvent in unitTypeChangeEvents)
        {
            orderedTrackerEvents.Add(new(unitTypeChangeEvent.Gameloop, index, null, unitTypeChangeEvent));
            index++;
        }

        orderedTrackerEvents.Sort(static (left, right) =>
        {
            int gameloopComparison = left.Gameloop.CompareTo(right.Gameloop);
            return gameloopComparison != 0 ? gameloopComparison : left.Index.CompareTo(right.Index);
        });

        return orderedTrackerEvents;
    }

    private static void TrackExpectedBuildUnitBornEvent(
        SUnitBornEvent bornEvent,
        Dictionary<int, int> playerIndexesByControlPlayerId,
        ExpectedPlayerLayout[] layouts,
        Dictionary<(int UnitTagIndex, int UnitTagRecycle), int> buildAreaPlayerIndexesByTag,
        HashSet<string>[] builtUnitNamesByPlayer)
    {
        if (bornEvent.Gameloop == 0
            || !playerIndexesByControlPlayerId.TryGetValue(bornEvent.ControlPlayerId, out int playerIndex))
        {
            return;
        }

        if (!layouts[playerIndex].Contains(new(bornEvent.X, bornEvent.Y)))
        {
            return;
        }

        builtUnitNamesByPlayer[playerIndex].Add(bornEvent.UnitTypeName);
        buildAreaPlayerIndexesByTag[(bornEvent.UnitTagIndex, bornEvent.UnitTagRecycle)] = playerIndex;
    }

    private static void TrackExpectedBuildUnitTypeChangeEvent(
        SUnitTypeChangeEvent typeChangeEvent,
        Dictionary<(int UnitTagIndex, int UnitTagRecycle), int> buildAreaPlayerIndexesByTag,
        HashSet<string>[] builtUnitNamesByPlayer)
    {
        if (typeChangeEvent.Gameloop == 0
            || !buildAreaPlayerIndexesByTag.TryGetValue((typeChangeEvent.UnitTagIndex, typeChangeEvent.UnitTagRecycle), out int playerIndex))
        {
            return;
        }

        builtUnitNamesByPlayer[playerIndex].Add(typeChangeEvent.UnitTypeName);
    }

    private static void VerifyExpectedSpawnUnit(
        SUnitBornEvent bornEvent,
        Dictionary<int, int> playerIndexesByControlPlayerId,
        IReadOnlyList<DirectStrikePlayer> players,
        HashSet<string>[] builtUnitNamesByPlayer,
        HashSet<ExpectedSpawnUnit> exposedSpawnUnits,
        HashSet<ExpectedSpawnUnit> verifiedSpawnUnits)
    {
        if (bornEvent.Gameloop == 0
            || !playerIndexesByControlPlayerId.TryGetValue(bornEvent.ControlPlayerId, out int playerIndex))
        {
            return;
        }

        ExpectedSpawnUnit unit = new(
            playerIndex,
            bornEvent.UnitIndex,
            bornEvent.UnitTypeName,
            bornEvent.Gameloop,
            bornEvent.X,
            bornEvent.Y,
            bornEvent.SUnitDiedEvent?.Gameloop,
            bornEvent.SUnitDiedEvent?.X,
            bornEvent.SUnitDiedEvent?.Y);
        if (exposedSpawnUnits.Contains(unit))
        {
            Assert.IsTrue(
                ExpectedIsAllowedSpawnUnitName(builtUnitNamesByPlayer[playerIndex], bornEvent.UnitTypeName, players[playerIndex].Commander),
                $"{bornEvent.UnitTypeName} must canonically match a player build-area unit name.");
            verifiedSpawnUnits.Add(unit);
        }
    }

    private static object CreateParserPolygon(Type posType, Type polygonType, params ExpectedPos[] points)
    {
        Array parserPoints = Array.CreateInstance(posType, points.Length);
        for (int i = 0; i < points.Length; i++)
        {
            parserPoints.SetValue(Activator.CreateInstance(posType, points[i].X, points[i].Y), i);
        }

        return Activator.CreateInstance(polygonType, [parserPoints])
            ?? throw new InvalidOperationException("Could not create private Polygon instance.");
    }

    private static bool InvokeParserPolygonContains(Type posType, Type polygonType, object polygon, ExpectedPos point)
    {
        object parserPoint = Activator.CreateInstance(posType, point.X, point.Y)
            ?? throw new InvalidOperationException("Could not create private Pos instance.");
        object? result = polygonType.GetMethod("Contains", BindingFlags.Public | BindingFlags.Instance)?.Invoke(polygon, [parserPoint]);
        Assert.IsNotNull(result);

        return (bool)result;
    }

    private static bool ExpectedPolygonContains(ExpectedPos position, params ExpectedPos?[] points)
    {
        ExpectedPos[] polygonPoints = [.. points.OfType<ExpectedPos>()];
        if (polygonPoints.Length != 4)
        {
            return false;
        }

        bool inside = false;
        for (int i = 0, j = polygonPoints.Length - 1; i < polygonPoints.Length; j = i++)
        {
            ExpectedPos pi = polygonPoints[i];
            ExpectedPos pj = polygonPoints[j];
            if (ExpectedIsOnSegment(pj, pi, position))
            {
                return true;
            }

            bool intersects = (pi.Y > position.Y) != (pj.Y > position.Y)
                && position.X < ((double)(pj.X - pi.X) * (position.Y - pi.Y) / (pj.Y - pi.Y)) + pi.X;
            if (intersects)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static bool ExpectedIsOnSegment(ExpectedPos start, ExpectedPos end, ExpectedPos position)
    {
        long crossProduct = ((long)position.Y - start.Y) * (end.X - start.X)
            - ((long)position.X - start.X) * (end.Y - start.Y);
        if (crossProduct != 0)
        {
            return false;
        }

        return position.X >= Math.Min(start.X, end.X)
            && position.X <= Math.Max(start.X, end.X)
            && position.Y >= Math.Min(start.Y, end.Y)
            && position.Y <= Math.Max(start.Y, end.Y);
    }

    private static bool ExpectedIsAllowedSpawnUnitName(HashSet<string> builtUnitNames, string spawnUnitName, Commander commander)
    {
        string canonicalSpawnUnitName = GetExpectedCanonicalSpawnUnitName(spawnUnitName, commander);
        foreach (string builtUnitName in builtUnitNames)
        {
            if (string.Equals(GetExpectedCanonicalSpawnUnitName(builtUnitName, commander), canonicalSpawnUnitName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetExpectedCanonicalSpawnUnitName(string unitTypeName, Commander commander)
    {
        string canonicalName = unitTypeName;
        string? commanderAffix = commander is Commander.None or Commander.Protoss or Commander.Terran or Commander.Zerg or Commander.Random
            ? null
            : commander.ToString();
        bool changed;
        do
        {
            changed = false;
            if (commanderAffix is not null && canonicalName.Length > commanderAffix.Length)
            {
                if (canonicalName.StartsWith(commanderAffix, StringComparison.Ordinal))
                {
                    canonicalName = canonicalName[commanderAffix.Length..];
                    changed = true;
                    continue;
                }

                if (canonicalName.EndsWith(commanderAffix, StringComparison.Ordinal))
                {
                    canonicalName = canonicalName[..^commanderAffix.Length];
                    changed = true;
                    continue;
                }
            }

            if (canonicalName.EndsWith("Lightweight", StringComparison.Ordinal)
                && canonicalName.Length > "Lightweight".Length)
            {
                canonicalName = canonicalName[..^"Lightweight".Length];
                changed = true;
            }
            else if (canonicalName.EndsWith("Starlight", StringComparison.Ordinal)
                && canonicalName.Length > "Starlight".Length)
            {
                canonicalName = canonicalName[..^"Starlight".Length];
                changed = true;
            }
        }
        while (changed);

        return canonicalName;
    }

    private readonly record struct ExpectedPos(int X, int Y);

    private readonly record struct ExpectedOrderedSpawnTrackerEvent(int Gameloop, int Index, SUnitBornEvent? BornEvent, SUnitTypeChangeEvent? TypeChangeEvent);

    private readonly record struct ExpectedSpawnUnit(int PlayerIndex, int UnitIndex, string Name, int Gameloop, int X, int Y, int? DiedGameloop, int? DiedX, int? DiedY);

    private sealed class ExpectedPlayerLayout
    {
        public ExpectedPos? East { get; set; }
        public ExpectedPos? North { get; set; }
        public ExpectedPos? South { get; set; }
        public ExpectedPos? West { get; set; }

        public bool Contains(ExpectedPos position)
        {
            return ExpectedPolygonContains(position, South, East, North, West);
        }
    }
}
