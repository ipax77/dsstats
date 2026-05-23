using dsstats.shared.Units;
using ExternalDirectStrikeReplay = Sc2DirectStrike.Parser.DirectStrikeReplay;
using SharedCommander = dsstats.shared.Commander;

namespace dsstats.play;

public static class SpawnPlaybackFactory
{
    public const double GameloopsPerSecond = 22.4;
    public const int DefaultStepSeconds = 5;
    public const int MapWidth = 256;
    public const int MapHeight = 240;

    private static readonly SpawnPlaybackLandmark[] DefaultLandmarks =
    [
        new("Nexus", "Base", 1, 190, 184, 14, "#5DADEC", 0),
        new("Cannon", "Defense", 1, 151, 141, 9, "#F8D34A", 0),
        new("Bunker", "Defense", 2, 105, 99, 9, "#F59E0B", 0),
        new("Planetary", "Base", 2, 66, 56, 14, "#F87171", 0),
    ];

    public static SpawnPlaybackReplay Create(
        ExternalDirectStrikeReplay replay,
        IReadOnlyList<SpawnPlaybackLandmark>? landmarks = null,
        IReadOnlyDictionary<SpawnPlaybackUnitKey, IReadOnlyList<int>>? unitKillGameloops = null,
        IReadOnlyList<SpawnPlaybackBuildUnit>? buildUnits = null)
    {
        ArgumentNullException.ThrowIfNull(replay);

        Dictionary<(SharedCommander Commander, string UnitName), (double Radius, string Color)> displayCache = [];
        List<SpawnPlaybackPlayer> players = new(replay.Players.Count);
        IReadOnlyList<SpawnPlaybackLandmark> playbackLandmarks = landmarks?.Count > 0
            ? landmarks
            : DefaultLandmarks;
        int durationGameloop = Math.Max(1, ToGameloop(replay.Duration));

        int spawnCount = 0;
        int unitsWithDiedEvent = 0;
        int unitsWithDiedPosition = 0;
        List<(int Gameloop, int Delta)> spawnEvents = [];

        foreach (var player in replay.Players.OrderBy(p => p.TeamId).ThenBy(p => p.GamePos))
        {
            SharedCommander commander = (SharedCommander)(int)player.Commander;
            List<SpawnPlaybackUnit> units = new(player.Spawns.Sum(spawn => spawn.Units.Count));

            foreach (var spawn in player.Spawns)
            {
                if (spawn.Units.Count > 0)
                {
                    spawnCount++;
                    spawnEvents.Add((spawn.StartGameloop, 1));
                    spawnEvents.Add((spawn.EndGameloop, -1));
                }

                durationGameloop = Math.Max(durationGameloop, spawn.EndGameloop);

                foreach (var unit in spawn.Units)
                {
                    durationGameloop = Math.Max(durationGameloop, unit.Gameloop);
                    if (unit.DiedGameloop is int diedGameloop)
                    {
                        unitsWithDiedEvent++;
                        durationGameloop = Math.Max(durationGameloop, diedGameloop);
                    }
                    if (unit.DiedX is not null && unit.DiedY is not null)
                    {
                        unitsWithDiedPosition++;
                    }

                    var displayInfo = GetDisplayInfo(displayCache, commander, unit.Name);
                    var target = GetTarget(unit.X, unit.Y, unit.DiedX, unit.DiedY);
                    var unitInfo = UnitMap.GetUnitInfo(unit.Name, commander);

                    units.Add(new(
                        unit.UnitIndex,
                        unitInfo.Name,
                        spawn.Number,
                        unit.Gameloop,
                        unit.X,
                        unit.Y,
                        unit.DiedGameloop,
                        unit.DiedX,
                        unit.DiedY,
                        target.X,
                        target.Y,
                        displayInfo.Radius,
                        displayInfo.Color,
                        GetUnitKillGameloops(unitKillGameloops, unit)));
                }
            }

            players.Add(new(
                player.Name,
                player.TeamId,
                player.GamePos,
                commander.ToString(),
                GetRefineryGameloops(player.RefineryTimes),
                units));
        }

        return new(
            durationGameloop,
            (int)Math.Round(DefaultStepSeconds * GameloopsPerSecond),
            GetBounds(players, playbackLandmarks),
            new(
                replay.Players.Count,
                spawnCount,
                players.Sum(player => player.Units.Count),
                unitsWithDiedEvent,
                unitsWithDiedPosition,
                GetMaxSimultaneousActiveSpawns(spawnEvents)),
            GetMiddleControl(replay),
            playbackLandmarks,
            buildUnits ?? [],
            GetSnapshots(replay),
            players);
    }

    public static SpawnPlaybackUnitKey GetUnitKey(
        int unitIndex,
        int spawnGameloop,
        int spawnX,
        int spawnY,
        string name)
    {
        return new(unitIndex, spawnGameloop, spawnX, spawnY, name);
    }

    private static (double Radius, string Color) GetDisplayInfo(
        Dictionary<(SharedCommander Commander, string UnitName), (double Radius, string Color)> cache,
        SharedCommander commander,
        string unitName)
    {
        var key = (commander, unitName);
        if (cache.TryGetValue(key, out var displayInfo))
        {
            return displayInfo;
        }

        displayInfo = UnitMapNg.GetColorAndRadius(unitName, commander);
        cache[key] = displayInfo;
        return displayInfo;
    }

    private static (double X, double Y) GetTarget(int spawnX, int spawnY, int? diedX, int? diedY)
    {
        if (diedX is int x && diedY is int y)
        {
            return (x, y);
        }

        return (MapWidth - spawnX, MapHeight - spawnY);
    }

    private static IReadOnlyList<int> GetUnitKillGameloops(
        IReadOnlyDictionary<SpawnPlaybackUnitKey, IReadOnlyList<int>>? unitKillGameloops,
        Sc2DirectStrike.Parser.DirectStrikeSpawnUnit unit)
    {
        if (unitKillGameloops is null)
        {
            return [];
        }

        var key = GetUnitKey(unit.UnitIndex, unit.Gameloop, unit.X, unit.Y, unit.Name);
        var killGameloops = unitKillGameloops.GetValueOrDefault(key);
        if (killGameloops is null || killGameloops.Count == 0)
        {
            return [];
        }

        int[] sortedKillGameloops = [.. killGameloops];
        Array.Sort(sortedKillGameloops);
        return sortedKillGameloops;
    }

    private static SpawnPlaybackMiddleControl GetMiddleControl(ExternalDirectStrikeReplay replay)
    {
        if (replay.FirstMiddleControlTeam is not (1 or 2) || replay.MiddleChanges.Length == 0)
        {
            return new(0, []);
        }

        int[] changeGameloops = new int[replay.MiddleChanges.Length];
        for (int i = 0; i < changeGameloops.Length; i++)
        {
            changeGameloops[i] = ToGameloop(replay.MiddleChanges[i]);
        }

        return new(replay.FirstMiddleControlTeam, changeGameloops);
    }

    private static int[] GetRefineryGameloops(TimeSpan[] refineryTimes)
    {
        if (refineryTimes.Length == 0)
        {
            return [];
        }

        int[] gameloops = new int[refineryTimes.Length];
        for (int i = 0; i < refineryTimes.Length; i++)
        {
            gameloops[i] = ToGameloop(refineryTimes[i]);
        }

        return gameloops;
    }

    private static IReadOnlyList<SpawnPlaybackSnapshot> GetSnapshots(ExternalDirectStrikeReplay replay)
    {
        List<Sc2DirectStrike.Parser.DirectStrikePlayer> team1Players = [];
        List<Sc2DirectStrike.Parser.DirectStrikePlayer> team2Players = [];
        foreach (var player in replay.Players)
        {
            if (player.TeamId == 1)
            {
                team1Players.Add(player);
            }
            else if (player.TeamId == 2)
            {
                team2Players.Add(player);
            }
        }

        team1Players.Sort(static (left, right) => left.GamePos.CompareTo(right.GamePos));
        team2Players.Sort(static (left, right) => left.GamePos.CompareTo(right.GamePos));

        int pairCount = Math.Max(team1Players.Count, team2Players.Count);
        if (pairCount == 0)
        {
            return [];
        }

        List<SpawnPlaybackSnapshot> snapshots = [];
        for (int pairIndex = 0; pairIndex < pairCount; pairIndex++)
        {
            IReadOnlyList<Sc2DirectStrike.Parser.DirectStrikePlayerSpawn> team1Spawns = pairIndex < team1Players.Count
                ? team1Players[pairIndex].Spawns
                : Array.Empty<Sc2DirectStrike.Parser.DirectStrikePlayerSpawn>();
            IReadOnlyList<Sc2DirectStrike.Parser.DirectStrikePlayerSpawn> team2Spawns = pairIndex < team2Players.Count
                ? team2Players[pairIndex].Spawns
                : Array.Empty<Sc2DirectStrike.Parser.DirectStrikePlayerSpawn>();

            AddPairedSnapshots(snapshots, team1Spawns, team2Spawns);
        }

        snapshots.Sort(static (left, right) =>
        {
            int startComparison = left.StartGameloop.CompareTo(right.StartGameloop);
            if (startComparison != 0)
            {
                return startComparison;
            }

            int endComparison = left.EndGameloop.CompareTo(right.EndGameloop);
            return endComparison != 0
                ? endComparison
                : left.SpawnNumber.CompareTo(right.SpawnNumber);
        });

        return snapshots;
    }

    private static void AddPairedSnapshots(
        List<SpawnPlaybackSnapshot> snapshots,
        IReadOnlyList<Sc2DirectStrike.Parser.DirectStrikePlayerSpawn> team1Spawns,
        IReadOnlyList<Sc2DirectStrike.Parser.DirectStrikePlayerSpawn> team2Spawns)
    {
        int team1Index = 0;
        int team2Index = 0;
        while (team1Index < team1Spawns.Count || team2Index < team2Spawns.Count)
        {
            int team1Number = team1Index < team1Spawns.Count ? team1Spawns[team1Index].Number : int.MaxValue;
            int team2Number = team2Index < team2Spawns.Count ? team2Spawns[team2Index].Number : int.MaxValue;
            int spawnNumber = Math.Min(team1Number, team2Number);

            Sc2DirectStrike.Parser.DirectStrikePlayerSpawn? team1Spawn = null;
            Sc2DirectStrike.Parser.DirectStrikePlayerSpawn? team2Spawn = null;
            if (team1Number == spawnNumber)
            {
                team1Spawn = team1Spawns[team1Index];
                team1Index++;
            }
            if (team2Number == spawnNumber)
            {
                team2Spawn = team2Spawns[team2Index];
                team2Index++;
            }

            if ((team1Spawn?.Units.Count ?? 0) == 0 && (team2Spawn?.Units.Count ?? 0) == 0)
            {
                continue;
            }

            int startGameloop = Math.Min(
                team1Spawn?.StartGameloop ?? int.MaxValue,
                team2Spawn?.StartGameloop ?? int.MaxValue);
            int endGameloop = Math.Max(
                team1Spawn?.EndGameloop ?? 0,
                team2Spawn?.EndGameloop ?? 0);
            snapshots.Add(new(spawnNumber, startGameloop, endGameloop));
        }
    }

    private static SpawnPlaybackBounds GetBounds(
        IReadOnlyList<SpawnPlaybackPlayer> players,
        IReadOnlyList<SpawnPlaybackLandmark> landmarks)
    {
        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;

        foreach (var unit in players.SelectMany(player => player.Units))
        {
            AddPoint(unit.SpawnX, unit.SpawnY);
            AddPoint(unit.TargetX, unit.TargetY);
            if (unit.DiedX is double diedX && unit.DiedY is double diedY)
            {
                AddPoint(diedX, diedY);
            }
        }

        foreach (var landmark in landmarks)
        {
            AddPoint(landmark.X, landmark.Y);
        }

        if (minX == double.MaxValue)
        {
            return new(0, 0, MapWidth, MapHeight);
        }

        const double padding = 8;
        return new(
            Math.Floor(Math.Max(0, minX - padding)),
            Math.Floor(Math.Max(0, minY - padding)),
            Math.Ceiling(Math.Min(MapWidth, maxX + padding)),
            Math.Ceiling(Math.Min(MapHeight, maxY + padding)));

        void AddPoint(double x, double y)
        {
            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
        }
    }

    private static int GetMaxSimultaneousActiveSpawns(List<(int Gameloop, int Delta)> spawnEvents)
    {
        int activeSpawns = 0;
        int maxActiveSpawns = 0;

        foreach (var spawnEvent in spawnEvents.OrderBy(e => e.Gameloop).ThenBy(e => e.Delta))
        {
            activeSpawns = Math.Max(0, activeSpawns + spawnEvent.Delta);
            maxActiveSpawns = Math.Max(maxActiveSpawns, activeSpawns);
        }

        return maxActiveSpawns;
    }

    private static int ToGameloop(TimeSpan value)
    {
        return value <= TimeSpan.Zero
            ? 0
            : (int)Math.Round(value.TotalSeconds * GameloopsPerSecond);
    }
}
