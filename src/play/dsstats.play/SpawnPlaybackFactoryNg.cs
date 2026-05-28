using System.Buffers.Binary;
using dsstats.shared;
using dsstats.shared.Units;

namespace dsstats.play;

public static class SpawnPlaybackFactoryNg
{
    public const double GameloopsPerSecond = 22.4;
    public const int DefaultStepSeconds = 5;
    public const int MaxUnitLifetimeGameloops = 2096;
    public const int MapWidth = 256;
    public const int MapHeight = 240;
    public const double DefaultSpeedPerGameloop =  0.140625;

    private const int PlanetaryX = 160;
    private const int PlanetaryY = 152;
    private const int BunkerX = 146;
    private const int BunkerY = 138;
    private const int NexusX = 96;
    private const int NexusY = 88;
    private const int CannonX = 110;
    private const int CannonY = 102;

    private const string UnitRowsDatasetId = "unitRows";
    private const string PathRowsDatasetId = "pathRows";
    private const string PathPointsDatasetId = "pathPoints";
    private const string KillGameloopsDatasetId = "killGameloops";

    private const int UnitRowIntCount = 11;
    private const int PathRowIntCount = 3;
    private const int PathPointFloatCount = 3;
    private const int UnitRowByteStride = UnitRowIntCount * sizeof(int);
    private const int PathRowByteStride = PathRowIntCount * sizeof(int);
    private const int PathPointByteStride = PathPointFloatCount * sizeof(float);

    private const int UnitFlagDiedGameloop = 1;
    private const int UnitFlagDiedPosition = 2;
    private const int UnitFlagFallbackTarget = 4;

    public static SpawnPlaybackReplayNg Create(
        ReplayDto replay,
        SpawnPlaybackSidecarDto sidecar)
    {
        ArgumentNullException.ThrowIfNull(replay);
        ArgumentNullException.ThrowIfNull(sidecar);

        Dictionary<int, ReplayPlayerDto> replayPlayersByGamePos = CreateReplayPlayerLookup(replay);
        SpawnPlaybackPlayerSidecarDto[] sidecarPlayers = CopySortedSidecarPlayers(sidecar);
        (int unitCount, int killCount) = CountRows(sidecarPlayers, replayPlayersByGamePos);

        byte[] unitRowBytes = AllocateBytes(unitCount, UnitRowByteStride);
        byte[] killGameloopBytes = AllocateBytes(killCount, sizeof(int));
        List<SpawnPlaybackPlayerNg> players = new(sidecarPlayers.Length);
        List<SpawnPlaybackUnitKindNg> unitKinds = [];
        Dictionary<UnitKindKey, int> unitKindIndexes = [];
        Dictionary<(Commander Commander, string UnitName), (double Radius, string Color)> displayCache = [];
        Dictionary<PathKey, int> pathIndexes = [];
        List<PathKey> paths = [];
        Dictionary<PlayerUnitSummaryKey, int> killsByPlayerUnit = [];
        List<(int Gameloop, int Delta)> spawnEvents = [];
        SpawnPlaybackLandmark[] landmarks = GetReplayDtoLandmarks(replay);

        int unitsWithDiedEvent = 0;
        int unitsWithDiedPosition = 0;
        int unitRowIndex = 0;
        int killIndex = 0;
        BoundsBuilder bounds = new();

        for (int sidecarPlayerIndex = 0; sidecarPlayerIndex < sidecarPlayers.Length; sidecarPlayerIndex++)
        {
            var sidecarPlayer = sidecarPlayers[sidecarPlayerIndex];
            if (!replayPlayersByGamePos.TryGetValue(sidecarPlayer.GamePos, out var replayPlayer))
            {
                continue;
            }

            int playerIndex = players.Count;
            var commander = replayPlayer.Race;
            players.Add(new(
                replayPlayer.Name,
                replayPlayer.TeamId,
                replayPlayer.GamePos,
                commander.ToString(),
                GetRefineryGameloops(replayPlayer.Refineries),
                GetTierUpgradeGameloops(replayPlayer.TierUpgrades)));

            Dictionary<int, SpawnRange> spawnRanges = [];
            var units = sidecarPlayer.Units;
            for (int unitIndex = 0; unitIndex < units.Count; unitIndex++)
            {
                var sidecarUnit = units[unitIndex];
                int expiresGameloop = GetExpiresGameloop(sidecarUnit.SpawnGameloop, sidecarUnit.DiedGameloop);
                int targetX;
                int targetY;
                int flags = 0;

                if (sidecarUnit.DiedGameloop is not null)
                {
                    flags |= UnitFlagDiedGameloop;
                    unitsWithDiedEvent++;
                }

                if (sidecarUnit.DiedX is int diedX && sidecarUnit.DiedY is int diedY)
                {
                    flags |= UnitFlagDiedPosition;
                    unitsWithDiedPosition++;
                    targetX = diedX;
                    targetY = diedY;
                }
                else
                {
                    flags |= UnitFlagFallbackTarget;
                    (targetX, targetY) = GetRouteTarget(sidecarUnit.SpawnX, sidecarUnit.SpawnY);
                }

                int unitKindIndex = GetUnitKindIndex(unitKinds, unitKindIndexes, displayCache, commander, sidecarUnit.Name);
                int pathIndex = GetPathIndex(pathIndexes, paths, sidecarUnit.SpawnX, sidecarUnit.SpawnY, targetX, targetY);
                int unitKillOffset = killIndex;
                int unitKillCount = sidecarUnit.KillGameloops.Count;
                WriteKillGameloops(killGameloopBytes, sidecarUnit.KillGameloops, ref killIndex);
                if (unitKillCount > 0)
                {
                    var summaryKey = new PlayerUnitSummaryKey(playerIndex, unitKindIndex);
                    killsByPlayerUnit.TryGetValue(summaryKey, out int existingKills);
                    killsByPlayerUnit[summaryKey] = existingKills + unitKillCount;
                }

                WriteUnitRow(
                    unitRowBytes,
                    unitRowIndex,
                    sidecarUnit.UnitIndex,
                    playerIndex,
                    unitKindIndex,
                    sidecarUnit.SpawnNumber,
                    sidecarUnit.SpawnGameloop,
                    expiresGameloop,
                    pathIndex,
                    unitKillOffset,
                    unitKillCount,
                    sidecarUnit.DiedGameloop ?? -1,
                    flags);
                unitRowIndex++;

                bounds.AddPoint(sidecarUnit.SpawnX, sidecarUnit.SpawnY);
                bounds.AddPoint(targetX, targetY);
                AddSpawnRange(spawnRanges, sidecarUnit.SpawnNumber, sidecarUnit.SpawnGameloop, expiresGameloop);
            }

            foreach (var pair in spawnRanges)
            {
                spawnEvents.Add((pair.Value.StartGameloop, 1));
                spawnEvents.Add((pair.Value.EndGameloop, -1));
            }
        }

        for (int i = 0; i < landmarks.Length; i++)
        {
            bounds.AddPoint(landmarks[i].X, landmarks[i].Y);
        }

        byte[] pathRowBytes = CreatePathRowBytes(paths);
        byte[] pathPointBytes = CreatePathPointBytes(paths);
        SpawnPlaybackSnapshot[] snapshots = GetSnapshots(sidecar);
        SpawnPlaybackStats stats = new(
            players.Count,
            sidecar.Snapshots.Count,
            unitRowIndex,
            unitsWithDiedEvent,
            unitsWithDiedPosition,
            2);

        return new(
            Math.Max(1, sidecar.DurationGameloop),
            Math.Max(1, sidecar.StepGameloops),
            bounds.ToBounds(),
            stats,
            GetSummary(replay, players, unitKinds, killsByPlayerUnit),
            GetMiddleControl(replay),
            landmarks,
            snapshots,
            players,
            unitKinds,
            [
                new(UnitRowsDatasetId, unitRowBytes, unitRowIndex, SpawnPlaybackBinaryDataFormatNg.Int32Rows, ByteStride: UnitRowByteStride),
                new(PathRowsDatasetId, pathRowBytes, paths.Count, SpawnPlaybackBinaryDataFormatNg.Int32Rows, ByteStride: PathRowByteStride),
                new(PathPointsDatasetId, pathPointBytes, paths.Count * 2, SpawnPlaybackBinaryDataFormatNg.Float32Rows, ByteStride: PathPointByteStride),
                new(KillGameloopsDatasetId, killGameloopBytes, killIndex, SpawnPlaybackBinaryDataFormatNg.Int32Y)
            ]);
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

    private static Dictionary<int, ReplayPlayerDto> CreateReplayPlayerLookup(ReplayDto replay)
    {
        Dictionary<int, ReplayPlayerDto> playersByGamePos = new(replay.Players.Count);
        for (int i = 0; i < replay.Players.Count; i++)
        {
            var player = replay.Players[i];
            playersByGamePos.Add(player.GamePos, player);
        }

        return playersByGamePos;
    }

    private static SpawnPlaybackPlayerSidecarDto[] CopySortedSidecarPlayers(SpawnPlaybackSidecarDto sidecar)
    {
        SpawnPlaybackPlayerSidecarDto[] players = new SpawnPlaybackPlayerSidecarDto[sidecar.Players.Count];
        for (int i = 0; i < players.Length; i++)
        {
            players[i] = sidecar.Players[i];
        }

        Array.Sort(players, static (left, right) => left.GamePos.CompareTo(right.GamePos));
        return players;
    }

    private static (int UnitCount, int KillCount) CountRows(
        SpawnPlaybackPlayerSidecarDto[] sidecarPlayers,
        Dictionary<int, ReplayPlayerDto> replayPlayersByGamePos)
    {
        int unitCount = 0;
        int killCount = 0;
        for (int playerIndex = 0; playerIndex < sidecarPlayers.Length; playerIndex++)
        {
            var sidecarPlayer = sidecarPlayers[playerIndex];
            if (!replayPlayersByGamePos.ContainsKey(sidecarPlayer.GamePos))
            {
                continue;
            }

            unitCount = checked(unitCount + sidecarPlayer.Units.Count);
            for (int unitIndex = 0; unitIndex < sidecarPlayer.Units.Count; unitIndex++)
            {
                killCount = checked(killCount + sidecarPlayer.Units[unitIndex].KillGameloops.Count);
            }
        }

        return (unitCount, killCount);
    }

    private static int GetUnitKindIndex(
        List<SpawnPlaybackUnitKindNg> unitKinds,
        Dictionary<UnitKindKey, int> unitKindIndexes,
        Dictionary<(Commander Commander, string UnitName), (double Radius, string Color)> displayCache,
        Commander commander,
        string unitName)
    {
        var unitInfo = UnitMap.GetUnitInfo(unitName, commander);
        var key = new UnitKindKey(commander, unitInfo.Name);
        if (unitKindIndexes.TryGetValue(key, out int index))
        {
            return index;
        }

        var displayInfo = GetDisplayInfo(displayCache, commander, unitInfo.Name);
        index = unitKinds.Count;
        unitKindIndexes.Add(key, index);
        unitKinds.Add(new(unitInfo.Name, commander.ToString(), displayInfo.Radius, displayInfo.Color));
        return index;
    }

    private static (double Radius, string Color) GetDisplayInfo(
        Dictionary<(Commander Commander, string UnitName), (double Radius, string Color)> cache,
        Commander commander,
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

    private static int GetPathIndex(
        Dictionary<PathKey, int> pathIndexes,
        List<PathKey> paths,
        int spawnX,
        int spawnY,
        int targetX,
        int targetY)
    {
        var key = new PathKey(spawnX, spawnY, targetX, targetY);
        if (pathIndexes.TryGetValue(key, out int index))
        {
            return index;
        }

        index = paths.Count;
        pathIndexes.Add(key, index);
        paths.Add(key);
        return index;
    }

    private static void AddSpawnRange(
        Dictionary<int, SpawnRange> spawnRanges,
        int spawnNumber,
        int spawnGameloop,
        int expiresGameloop)
    {
        if (spawnRanges.TryGetValue(spawnNumber, out var range))
        {
            spawnRanges[spawnNumber] = new(
                Math.Min(range.StartGameloop, spawnGameloop),
                Math.Max(range.EndGameloop, expiresGameloop));
            return;
        }

        spawnRanges.Add(spawnNumber, new(spawnGameloop, expiresGameloop));
    }

    private static void WriteUnitRow(
        byte[] bytes,
        int rowIndex,
        int unitIndex,
        int playerIndex,
        int unitKindIndex,
        int spawnNumber,
        int spawnGameloop,
        int expiresGameloop,
        int pathIndex,
        int killOffset,
        int killCount,
        int diedGameloop,
        int flags)
    {
        int offset = checked(rowIndex * UnitRowByteStride);
        WriteInt32(bytes, offset, unitIndex);
        WriteInt32(bytes, offset + sizeof(int), playerIndex);
        WriteInt32(bytes, offset + 2 * sizeof(int), unitKindIndex);
        WriteInt32(bytes, offset + 3 * sizeof(int), spawnNumber);
        WriteInt32(bytes, offset + 4 * sizeof(int), spawnGameloop);
        WriteInt32(bytes, offset + 5 * sizeof(int), expiresGameloop);
        WriteInt32(bytes, offset + 6 * sizeof(int), pathIndex);
        WriteInt32(bytes, offset + 7 * sizeof(int), killOffset);
        WriteInt32(bytes, offset + 8 * sizeof(int), killCount);
        WriteInt32(bytes, offset + 9 * sizeof(int), diedGameloop);
        WriteInt32(bytes, offset + 10 * sizeof(int), flags);
    }

    private static void WriteKillGameloops(byte[] bytes, IReadOnlyList<int> killGameloops, ref int writeIndex)
    {
        for (int i = 0; i < killGameloops.Count; i++)
        {
            WriteInt32(bytes, checked(writeIndex * sizeof(int)), killGameloops[i]);
            writeIndex++;
        }
    }

    private static byte[] CreatePathRowBytes(List<PathKey> paths)
    {
        byte[] bytes = AllocateBytes(paths.Count, PathRowByteStride);
        for (int i = 0; i < paths.Count; i++)
        {
            int offset = i * PathRowByteStride;
            WriteInt32(bytes, offset, i * 2);
            WriteInt32(bytes, offset + sizeof(int), 2);
            WriteInt32(bytes, offset + 2 * sizeof(int), 0);
        }

        return bytes;
    }

    private static byte[] CreatePathPointBytes(List<PathKey> paths)
    {
        byte[] bytes = AllocateBytes(paths.Count * 2, PathPointByteStride);
        for (int i = 0; i < paths.Count; i++)
        {
            var path = paths[i];
            int firstOffset = i * 2 * PathPointByteStride;
            WritePathPoint(bytes, firstOffset, path.SpawnX, path.SpawnY, 0);
            WritePathPoint(bytes, firstOffset + PathPointByteStride, path.TargetX, path.TargetY, 1);
        }

        return bytes;
    }

    private static void WritePathPoint(byte[] bytes, int offset, float x, float y, float t)
    {
        WriteSingle(bytes, offset, x);
        WriteSingle(bytes, offset + sizeof(float), y);
        WriteSingle(bytes, offset + 2 * sizeof(float), t);
    }

    private static byte[] AllocateBytes(int count, int byteStride)
    {
        return count == 0
            ? []
            : GC.AllocateUninitializedArray<byte>(checked(count * byteStride));
    }

    private static void WriteInt32(byte[] bytes, int offset, int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset, sizeof(int)), value);
    }

    private static void WriteSingle(byte[] bytes, int offset, float value)
    {
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(offset, sizeof(float)), value);
    }

    private static SpawnPlaybackSummary GetSummary(
        ReplayDto replay,
        IReadOnlyList<SpawnPlaybackPlayerNg> players,
        IReadOnlyList<SpawnPlaybackUnitKindNg> unitKinds,
        Dictionary<PlayerUnitSummaryKey, int> killsByPlayerUnit)
    {
        ReplayPlayerDto[] replayPlayers = new ReplayPlayerDto[replay.Players.Count];
        for (int i = 0; i < replayPlayers.Length; i++)
        {
            replayPlayers[i] = replay.Players[i];
        }

        Array.Sort(replayPlayers, static (left, right) =>
        {
            int teamComparison = left.TeamId.CompareTo(right.TeamId);
            return teamComparison != 0
                ? teamComparison
                : left.GamePos.CompareTo(right.GamePos);
        });

        List<SpawnPlaybackPlayerSummary> playerSummaries = new(replayPlayers.Length);
        int totalKills = 0;
        for (int i = 0; i < replayPlayers.Length; i++)
        {
            var replayPlayer = replayPlayers[i];
            int kills = GetAllKills(replayPlayer);
            totalKills += kills;
            playerSummaries.Add(new(
                replayPlayer.Name,
                replayPlayer.TeamId,
                replayPlayer.GamePos,
                replayPlayer.Race.ToString(),
                kills));
        }

        List<SpawnPlaybackTopUnitSummary> topUnits = GetTopUnitSummaries(players, unitKinds, killsByPlayerUnit);
        return new(totalKills, playerSummaries, topUnits);
    }

    private static int GetAllKills(ReplayPlayerDto replayPlayer)
    {
        for (int i = 0; i < replayPlayer.Spawns.Count; i++)
        {
            var spawn = replayPlayer.Spawns[i];
            if (spawn.Breakpoint == Breakpoint.All)
            {
                return spawn.KilledValue;
            }
        }

        return 0;
    }

    private static List<SpawnPlaybackTopUnitSummary> GetTopUnitSummaries(
        IReadOnlyList<SpawnPlaybackPlayerNg> players,
        IReadOnlyList<SpawnPlaybackUnitKindNg> unitKinds,
        Dictionary<PlayerUnitSummaryKey, int> killsByPlayerUnit)
    {
        List<SpawnPlaybackTopUnitSummary> topUnits = new(killsByPlayerUnit.Count);
        foreach (var pair in killsByPlayerUnit)
        {
            var player = players[pair.Key.PlayerIndex];
            var unitKind = unitKinds[pair.Key.UnitKindIndex];
            topUnits.Add(new(
                player.Name,
                player.TeamId,
                player.GamePos,
                unitKind.Name,
                pair.Value));
        }

        topUnits.Sort(static (left, right) =>
        {
            int killsComparison = right.Kills.CompareTo(left.Kills);
            if (killsComparison != 0)
            {
                return killsComparison;
            }

            int teamComparison = left.TeamId.CompareTo(right.TeamId);
            if (teamComparison != 0)
            {
                return teamComparison;
            }

            int gamePosComparison = left.GamePos.CompareTo(right.GamePos);
            return gamePosComparison != 0
                ? gamePosComparison
                : string.Compare(left.UnitName, right.UnitName, StringComparison.Ordinal);
        });

        if (topUnits.Count > 5)
        {
            topUnits.RemoveRange(5, topUnits.Count - 5);
        }

        return topUnits;
    }

    private static (int X, int Y) GetRouteTarget(int spawnX, int spawnY)
    {
        return (MapWidth - spawnX, MapHeight - spawnY);
    }

    private static int GetExpiresGameloop(int spawnGameloop, int? diedGameloop)
    {
        return diedGameloop ?? spawnGameloop + MaxUnitLifetimeGameloops;
    }

    private static SpawnPlaybackMiddleControl GetMiddleControl(ReplayDto replay)
    {
        if (replay.MiddleChanges.Count < 2 || replay.MiddleChanges[0] is not (1 or 2))
        {
            return new(0, []);
        }

        int[] changeGameloops = new int[replay.MiddleChanges.Count - 1];
        for (int i = 1; i < replay.MiddleChanges.Count; i++)
        {
            changeGameloops[i - 1] = ToGameloop(replay.MiddleChanges[i]);
        }

        return new(replay.MiddleChanges[0], changeGameloops);
    }

    private static SpawnPlaybackLandmark[] GetReplayDtoLandmarks(ReplayDto replay)
    {
        return
        [
            new("Planetary", "Base", 1, PlanetaryX, PlanetaryY, 14, "#5DADEC", 0),
            new("Bunker", "Defense", 1, BunkerX, BunkerY, 9, "#F8D34A", 0, ToGameloopOrNull(replay.Bunker)),
            new("Cannon", "Defense", 2, CannonX, CannonY, 9, "#F59E0B", 0, ToGameloopOrNull(replay.Cannon)),
            new("Nexus", "Base", 2, NexusX, NexusY, 14, "#F87171", 0),
        ];
    }

    private static SpawnPlaybackSnapshot[] GetSnapshots(SpawnPlaybackSidecarDto sidecar)
    {
        SpawnPlaybackSnapshot[] snapshots = new SpawnPlaybackSnapshot[sidecar.Snapshots.Count];
        for (int i = 0; i < snapshots.Length; i++)
        {
            var snapshot = sidecar.Snapshots[i];
            snapshots[i] = new(snapshot.SpawnNumber, snapshot.StartGameloop, snapshot.EndGameloop);
        }

        return snapshots;
    }

    private static int[] GetRefineryGameloops(IReadOnlyList<int> refinerySeconds)
    {
        if (refinerySeconds.Count == 0)
        {
            return [];
        }

        int[] gameloops = new int[refinerySeconds.Count];
        for (int i = 0; i < gameloops.Length; i++)
        {
            gameloops[i] = ToGameloop(refinerySeconds[i]);
        }

        return gameloops;
    }

    private static int[] GetTierUpgradeGameloops(IReadOnlyList<int> tierUpgradeSeconds)
    {
        if (tierUpgradeSeconds.Count == 0)
        {
            return [];
        }

        int[] gameloops = new int[tierUpgradeSeconds.Count];
        for (int i = 0; i < gameloops.Length; i++)
        {
            gameloops[i] = ToGameloop(tierUpgradeSeconds[i]);
        }

        Array.Sort(gameloops);
        return gameloops;
    }

    private static int ToGameloop(int seconds)
    {
        return seconds <= 0
            ? 0
            : (int)Math.Round(seconds * GameloopsPerSecond);
    }

    private static int? ToGameloopOrNull(int seconds)
    {
        return seconds <= 0 ? null : ToGameloop(seconds);
    }

    private readonly record struct UnitKindKey(Commander Commander, string UnitName);

    private readonly record struct PlayerUnitSummaryKey(int PlayerIndex, int UnitKindIndex);

    private readonly record struct PathKey(int SpawnX, int SpawnY, int TargetX, int TargetY);

    private readonly record struct SpawnRange(int StartGameloop, int EndGameloop);

    private struct BoundsBuilder
    {
        private double minX = double.MaxValue;
        private double minY = double.MaxValue;
        private double maxX = double.MinValue;
        private double maxY = double.MinValue;

        public BoundsBuilder()
        {
        }

        public void AddPoint(double x, double y)
        {
            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
        }

        public readonly SpawnPlaybackBounds ToBounds()
        {
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
        }
    }
}
