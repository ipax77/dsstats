using dsstats.shared;
using s2protocol.NET;
using ExternalDirectStrikeReplay = Sc2DirectStrike.Parser.DirectStrikeReplay;

namespace dsstats.parser;

public static class SpawnPlaybackSidecarFactory
{
    public const double GameloopsPerSecond = 22.4;
    public const int DefaultStepSeconds = 5;

    private const int SpawnPairWindowGameloops = 112;

    public static SpawnPlaybackSidecarDto? Create(Sc2Replay replay, ExternalDirectStrikeReplay directStrikeReplay)
    {
        ArgumentNullException.ThrowIfNull(replay);
        ArgumentNullException.ThrowIfNull(directStrikeReplay);

        if (!SpawnPlaybackEligibility.IsEligible(directStrikeReplay.Players.Count, directStrikeReplay.Duration))
        {
            return null;
        }

        int totalUnitCount = GetSpawnUnitCount(directStrikeReplay);
        if (totalUnitCount == 0)
        {
            return null;
        }

        var killGameloopsByTag = GetKillGameloopsByKillerTag(replay);
        var unitKillGameloops = GetUnitKillGameloops(replay.TrackerEvents?.SUnitBornEvents, killGameloopsByTag);
        var players = new List<SpawnPlaybackPlayerSidecarDto>(directStrikeReplay.Players.Count);
        int durationGameloop = Math.Max(1, ToGameloop(directStrikeReplay.Duration));

        foreach (var player in directStrikeReplay.Players.OrderBy(player => player.GamePos))
        {
            var units = new List<SpawnPlaybackUnitSidecarDto>(player.Spawns.Sum(spawn => spawn.Units.Count));
            foreach (var spawn in player.Spawns)
            {
                durationGameloop = Math.Max(durationGameloop, spawn.EndGameloop);
                foreach (var unit in spawn.Units)
                {
                    durationGameloop = Math.Max(durationGameloop, unit.Gameloop);
                    if (unit.DiedGameloop is int diedGameloop)
                    {
                        durationGameloop = Math.Max(durationGameloop, diedGameloop);
                    }

                    var key = GetUnitKey(unit.UnitIndex, unit.Gameloop, unit.X, unit.Y, unit.Name);
                    units.Add(new(
                        unit.UnitIndex,
                        unit.Name,
                        spawn.Number,
                        unit.Gameloop,
                        unit.X,
                        unit.Y,
                        unit.DiedGameloop,
                        unit.DiedX,
                        unit.DiedY,
                        unitKillGameloops.GetValueOrDefault(key) ?? []));
                }
            }

            players.Add(new(player.GamePos, units));
        }

        return new(
            durationGameloop,
            (int)Math.Round(DefaultStepSeconds * GameloopsPerSecond),
            players,
            GetSnapshots(directStrikeReplay));
    }

    private static int GetSpawnUnitCount(ExternalDirectStrikeReplay replay)
    {
        int count = 0;
        foreach (var player in replay.Players)
        {
            foreach (var spawn in player.Spawns)
            {
                count = checked(count + spawn.Units.Count);
            }
        }

        return count;
    }

    private static Dictionary<UnitKey, IReadOnlyList<int>> GetUnitKillGameloops(
        ICollection<s2protocol.NET.Models.SUnitBornEvent>? bornEvents,
        IReadOnlyDictionary<UnitTag, List<int>> killGameloopsByTag)
    {
        Dictionary<UnitKey, IReadOnlyList<int>> unitKillGameloops = [];
        if (bornEvents is null || bornEvents.Count == 0 || killGameloopsByTag.Count == 0)
        {
            return unitKillGameloops;
        }

        foreach (var bornEvent in bornEvents)
        {
            var tag = new UnitTag(bornEvent.UnitTagIndex, bornEvent.UnitTagRecycle);
            if (!killGameloopsByTag.TryGetValue(tag, out var killGameloops))
            {
                continue;
            }

            var key = GetUnitKey(
                bornEvent.UnitIndex,
                bornEvent.Gameloop,
                bornEvent.X,
                bornEvent.Y,
                bornEvent.UnitTypeName);
            unitKillGameloops[key] = killGameloops;
        }

        return unitKillGameloops;
    }

    private static Dictionary<UnitTag, List<int>> GetKillGameloopsByKillerTag(Sc2Replay replay)
    {
        Dictionary<UnitTag, List<int>> killGameloops = [];

        var diedEvents = replay.TrackerEvents?.SUnitDiedEvents;
        if (diedEvents is null || diedEvents.Count == 0)
        {
            return killGameloops;
        }

        foreach (var diedEvent in diedEvents)
        {
            if (diedEvent.KillerUnitTagIndex is int killerUnitTagIndex
                && diedEvent.KillerUnitTagRecycle is int killerUnitTagRecycle)
            {
                var tag = new UnitTag(killerUnitTagIndex, killerUnitTagRecycle);
                if (!killGameloops.TryGetValue(tag, out var gameloops))
                {
                    gameloops = [];
                    killGameloops[tag] = gameloops;
                }

                gameloops.Add(diedEvent.Gameloop);
            }
        }

        foreach (var gameloops in killGameloops.Values)
        {
            gameloops.Sort();
        }

        return killGameloops;
    }

    private static IReadOnlyList<SpawnPlaybackSnapshotSidecarDto> GetSnapshots(ExternalDirectStrikeReplay replay)
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

        List<SpawnPlaybackSnapshotSidecarDto> snapshots = [];
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
        List<SpawnPlaybackSnapshotSidecarDto> snapshots,
        IReadOnlyList<Sc2DirectStrike.Parser.DirectStrikePlayerSpawn> team1Spawns,
        IReadOnlyList<Sc2DirectStrike.Parser.DirectStrikePlayerSpawn> team2Spawns)
    {
        int team1Index = GetNextNonEmptySpawnIndex(team1Spawns, 0);
        int team2Index = GetNextNonEmptySpawnIndex(team2Spawns, 0);
        while (team1Index < team1Spawns.Count || team2Index < team2Spawns.Count)
        {
            if (team1Index >= team1Spawns.Count)
            {
                AddSnapshot(snapshots, team2Spawns[team2Index], null);
                team2Index = GetNextNonEmptySpawnIndex(team2Spawns, team2Index + 1);
                continue;
            }
            if (team2Index >= team2Spawns.Count)
            {
                AddSnapshot(snapshots, team1Spawns[team1Index], null);
                team1Index = GetNextNonEmptySpawnIndex(team1Spawns, team1Index + 1);
                continue;
            }

            var team1Spawn = team1Spawns[team1Index];
            var team2Spawn = team2Spawns[team2Index];
            int startDelta = team1Spawn.StartGameloop - team2Spawn.StartGameloop;
            if (Math.Abs(startDelta) <= SpawnPairWindowGameloops)
            {
                AddSnapshot(snapshots, team1Spawn, team2Spawn);
                team1Index = GetNextNonEmptySpawnIndex(team1Spawns, team1Index + 1);
                team2Index = GetNextNonEmptySpawnIndex(team2Spawns, team2Index + 1);
            }
            else if (startDelta < 0)
            {
                AddSnapshot(snapshots, team1Spawn, null);
                team1Index = GetNextNonEmptySpawnIndex(team1Spawns, team1Index + 1);
            }
            else
            {
                AddSnapshot(snapshots, team2Spawn, null);
                team2Index = GetNextNonEmptySpawnIndex(team2Spawns, team2Index + 1);
            }
        }
    }

    private static int GetNextNonEmptySpawnIndex(
        IReadOnlyList<Sc2DirectStrike.Parser.DirectStrikePlayerSpawn> spawns,
        int startIndex)
    {
        for (int i = startIndex; i < spawns.Count; i++)
        {
            if (spawns[i].Units.Count > 0)
            {
                return i;
            }
        }

        return spawns.Count;
    }

    private static void AddSnapshot(
        List<SpawnPlaybackSnapshotSidecarDto> snapshots,
        Sc2DirectStrike.Parser.DirectStrikePlayerSpawn firstSpawn,
        Sc2DirectStrike.Parser.DirectStrikePlayerSpawn? secondSpawn)
    {
        int spawnNumber = Math.Max(firstSpawn.Number, secondSpawn?.Number ?? 0);
        int startGameloop = Math.Min(firstSpawn.StartGameloop, secondSpawn?.StartGameloop ?? int.MaxValue);
        int endGameloop = Math.Max(firstSpawn.EndGameloop, secondSpawn?.EndGameloop ?? 0);
        snapshots.Add(new(spawnNumber, startGameloop, endGameloop));
    }

    private static int ToGameloop(TimeSpan value)
    {
        return value <= TimeSpan.Zero
            ? 0
            : (int)Math.Round(value.TotalSeconds * GameloopsPerSecond);
    }

    private static UnitKey GetUnitKey(int unitIndex, int spawnGameloop, int spawnX, int spawnY, string name)
    {
        return new(unitIndex, spawnGameloop, spawnX, spawnY, name);
    }

    private readonly record struct UnitTag(int UnitTagIndex, int UnitTagRecycle);

    private readonly record struct UnitKey(
        int UnitIndex,
        int SpawnGameloop,
        int SpawnX,
        int SpawnY,
        string Name);
}
