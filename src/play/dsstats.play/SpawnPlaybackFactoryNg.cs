using dsstats.shared;
using dsstats.shared.Units;

namespace dsstats.play;

public static partial class SpawnPlaybackFactoryNg
{
    public const double GameloopsPerSecond = 22.4;
    public const int DefaultStepSeconds = 5;
    public const int MaxUnitLifetimeGameloops = 2096;
    public const int MapWidth = 256;
    public const int MapHeight = 240;
    public const double DefaultSpeedPerGameloop = 0.140625;

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

        byte[] killGameloopBytes = AllocateBytes(killCount, sizeof(int));
        List<SpawnPlaybackPlayerNg> players = new(sidecarPlayers.Length);
        List<SpawnPlaybackUnitKindNg> unitKinds = [];
        List<UnitBinaryRow> unitRows = new(unitCount);
        Dictionary<UnitKindKey, int> unitKindIndexes = [];
        Dictionary<(Commander Commander, string UnitName), (double Radius, string Color)> displayCache = [];
        Dictionary<PathKey, int> pathIndexes = [];
        List<PathKey> paths = [];
        Dictionary<PlayerUnitSummaryKey, int> killsByPlayerUnit = [];
        List<(int Gameloop, int Delta)> spawnEvents = [];
        SpawnPlaybackLandmark[] landmarks = GetReplayDtoLandmarks(replay);

        int unitsWithDiedEvent = 0;
        int unitsWithDiedPosition = 0;
        int killIndex = 0;
        int pathPointCount = 0;
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
                int lifetimeGameloops = Math.Max(1, expiresGameloop - sidecarUnit.SpawnGameloop);
                int flags = 0;

                if (sidecarUnit.DiedGameloop is not null)
                {
                    flags |= UnitFlagDiedGameloop;
                    unitsWithDiedEvent++;
                }

                bool hasDiedPosition = sidecarUnit.DiedX is not null && sidecarUnit.DiedY is not null;
                int boundsTargetX;
                int boundsTargetY;
                if (hasDiedPosition)
                {
                    flags |= UnitFlagDiedPosition;
                    unitsWithDiedPosition++;
                    boundsTargetX = sidecarUnit.DiedX!.Value;
                    boundsTargetY = sidecarUnit.DiedY!.Value;
                }
                else
                {
                    flags |= UnitFlagFallbackTarget;
                    boundsTargetX = MapWidth - sidecarUnit.SpawnX;
                    boundsTargetY = MapHeight - sidecarUnit.SpawnY;
                }

                int unitKindIndex = GetUnitKindIndex(unitKinds, unitKindIndexes, displayCache, commander, sidecarUnit.Name);
                PathKey path = CreatePath(
                    sidecarUnit.SpawnX,
                    sidecarUnit.SpawnY,
                    sidecarUnit.DiedX,
                    sidecarUnit.DiedY,
                    lifetimeGameloops);
                int pathIndex = GetPathIndex(pathIndexes, paths, path, ref pathPointCount);
                int unitKillOffset = killIndex;
                int unitKillCount = sidecarUnit.KillGameloops.Count;
                WriteKillGameloops(killGameloopBytes, sidecarUnit.KillGameloops, ref killIndex);
                if (unitKillCount > 0)
                {
                    var summaryKey = new PlayerUnitSummaryKey(playerIndex, unitKindIndex);
                    killsByPlayerUnit.TryGetValue(summaryKey, out int existingKills);
                    killsByPlayerUnit[summaryKey] = existingKills + unitKillCount;
                }

                unitRows.Add(new(
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
                    flags));

                bounds.AddPoint(sidecarUnit.SpawnX, sidecarUnit.SpawnY);
                bounds.AddPoint(boundsTargetX, boundsTargetY);
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
        byte[] pathPointBytes = CreatePathPointBytes(paths, pathPointCount);
        unitRows.Sort(CompareUnitRows);
        byte[] unitRowBytes = CreateUnitRowBytes(unitRows);
        SpawnPlaybackSnapshot[] snapshots = GetSnapshots(sidecar);
        SpawnPlaybackStats stats = new(
            players.Count,
            sidecar.Snapshots.Count,
            unitRows.Count,
            unitsWithDiedEvent,
            unitsWithDiedPosition,
            GetMaxSimultaneousActiveSpawns(spawnEvents));

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
                new(SpawnPlaybackBinaryPayloads.UnitRowsDatasetId, unitRowBytes, unitRows.Count, SpawnPlaybackBinaryDataFormatNg.Int32Rows, ByteStride: SpawnPlaybackBinaryPayloads.UnitRowByteStride),
                new(SpawnPlaybackBinaryPayloads.PathRowsDatasetId, pathRowBytes, paths.Count, SpawnPlaybackBinaryDataFormatNg.Int32Rows, ByteStride: SpawnPlaybackBinaryPayloads.PathRowByteStride),
                new(SpawnPlaybackBinaryPayloads.PathPointsDatasetId, pathPointBytes, pathPointCount, SpawnPlaybackBinaryDataFormatNg.Int32Rows, ByteStride: SpawnPlaybackBinaryPayloads.PathPointByteStride),
                new(SpawnPlaybackBinaryPayloads.KillGameloopsDatasetId, killGameloopBytes, killIndex, SpawnPlaybackBinaryDataFormatNg.Int32Y)
            ]);
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
}
