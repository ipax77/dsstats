using System.Buffers;
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
    private const int UnitRowArrayPoolThreshold = 8192;

    public static SpawnPlaybackReplayNg Create(
        ReplayDto replay,
        SpawnPlaybackSidecarDto sidecar)
    {
        ArgumentNullException.ThrowIfNull(replay);
        ArgumentNullException.ThrowIfNull(sidecar);

        ReplayPlayerGamePosLookup replayPlayersByGamePos = CreateReplayPlayerLookup(replay);
        SpawnPlaybackPlayerSidecarDto[] sidecarPlayers = CopySortedSidecarPlayers(sidecar);
        CreateCounts counts = CountRows(sidecarPlayers, replayPlayersByGamePos);
        bool returnUnitRowsToPool = counts.UnitCount >= UnitRowArrayPoolThreshold;
        UnitBinaryRow[] unitRows = counts.UnitCount == 0
            ? []
            : returnUnitRowsToPool
                ? ArrayPool<UnitBinaryRow>.Shared.Rent(counts.UnitCount)
                : GC.AllocateUninitializedArray<UnitBinaryRow>(counts.UnitCount);

        try
        {
            byte[] killGameloopBytes = AllocateBytes(counts.KillCount, sizeof(int));
            List<SpawnPlaybackPlayerNg> players = new(sidecarPlayers.Length);
            List<SpawnPlaybackUnitKindNg> unitKinds = [];
            Dictionary<UnitKindKey, int> unitKindIndexes = [];
            Dictionary<(Commander Commander, string UnitName), (double Radius, string Color)> displayCache = [];
            Dictionary<PathKey, int> pathIndexes = [];
            List<PathKey> paths = [];
            Dictionary<PlayerUnitSummaryKey, int> killsByPlayerUnit = [];
            List<(int Gameloop, int Delta)> spawnEvents = [];
            SpawnPlaybackLandmark[] landmarks = GetReplayDtoLandmarks(replay);
            DeathCluster[] deathClusters = CreateDeathClusters(sidecarPlayers, replayPlayersByGamePos, counts.DeathCount);

            int unitsWithDiedEvent = 0;
            int unitsWithDiedPosition = 0;
            int unitRowCount = 0;
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

                var units = sidecarPlayer.Units;
                Dictionary<int, SpawnRange> spawnRanges = [];
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
                        sidecarUnit.SpawnGameloop,
                        sidecarUnit.DiedX,
                        sidecarUnit.DiedY,
                        lifetimeGameloops,
                        deathClusters);
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

                    unitRows[unitRowCount++] = new(
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
            Span<UnitBinaryRow> populatedUnitRows = unitRows.AsSpan(0, unitRowCount);
            populatedUnitRows.Sort(CompareUnitRows);
            byte[] unitRowBytes = CreateUnitRowBytes(populatedUnitRows);
            SpawnPlaybackSnapshot[] snapshots = GetSnapshots(sidecar);
            SpawnPlaybackStats stats = new(
                players.Count,
                sidecar.Snapshots.Count,
                unitRowCount,
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
                    new(SpawnPlaybackBinaryPayloads.UnitRowsDatasetId, unitRowBytes, unitRowCount, SpawnPlaybackBinaryDataFormatNg.Int32Rows, ByteStride: SpawnPlaybackBinaryPayloads.UnitRowByteStride),
                    new(SpawnPlaybackBinaryPayloads.PathRowsDatasetId, pathRowBytes, paths.Count, SpawnPlaybackBinaryDataFormatNg.Int32Rows, ByteStride: SpawnPlaybackBinaryPayloads.PathRowByteStride),
                    new(SpawnPlaybackBinaryPayloads.PathPointsDatasetId, pathPointBytes, pathPointCount, SpawnPlaybackBinaryDataFormatNg.Int32Rows, ByteStride: SpawnPlaybackBinaryPayloads.PathPointByteStride),
                    new(SpawnPlaybackBinaryPayloads.KillGameloopsDatasetId, killGameloopBytes, killIndex, SpawnPlaybackBinaryDataFormatNg.Int32Y)
                ]);
        }
        finally
        {
            if (returnUnitRowsToPool)
            {
                ArrayPool<UnitBinaryRow>.Shared.Return(unitRows);
            }
        }
    }

    private static ReplayPlayerGamePosLookup CreateReplayPlayerLookup(ReplayDto replay)
    {
        int minGamePos = 0;
        int maxGamePos = -1;
        for (int i = 0; i < replay.Players.Count; i++)
        {
            int gamePos = replay.Players[i].GamePos;
            if (i == 0)
            {
                minGamePos = gamePos;
                maxGamePos = gamePos;
                continue;
            }

            minGamePos = Math.Min(minGamePos, gamePos);
            maxGamePos = Math.Max(maxGamePos, gamePos);
        }

        ReplayPlayerDto?[] playersByGamePos = maxGamePos < minGamePos
            ? []
            : new ReplayPlayerDto?[checked(maxGamePos - minGamePos + 1)];
        for (int i = 0; i < replay.Players.Count; i++)
        {
            var player = replay.Players[i];
            int index = player.GamePos - minGamePos;
            if (playersByGamePos[index] is not null)
            {
                throw new ArgumentException($"An item with the same key has already been added. Key: {player.GamePos}");
            }

            playersByGamePos[index] = player;
        }

        return new(playersByGamePos, minGamePos);
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

    private static CreateCounts CountRows(
        SpawnPlaybackPlayerSidecarDto[] sidecarPlayers,
        ReplayPlayerGamePosLookup replayPlayersByGamePos)
    {
        int unitCount = 0;
        int killCount = 0;
        int deathCount = 0;
        for (int playerIndex = 0; playerIndex < sidecarPlayers.Length; playerIndex++)
        {
            var sidecarPlayer = sidecarPlayers[playerIndex];
            if (!replayPlayersByGamePos.TryGetValue(sidecarPlayer.GamePos, out _))
            {
                continue;
            }

            unitCount = checked(unitCount + sidecarPlayer.Units.Count);
            for (int unitIndex = 0; unitIndex < sidecarPlayer.Units.Count; unitIndex++)
            {
                var unit = sidecarPlayer.Units[unitIndex];
                killCount = checked(killCount + unit.KillGameloops.Count);
                if (unit.DiedGameloop is not null
                    && unit.DiedX is not null
                    && unit.DiedY is not null)
                {
                    deathCount++;
                }
            }
        }

        return new(unitCount, killCount, deathCount);
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

    private readonly record struct CreateCounts(
        int UnitCount,
        int KillCount,
        int DeathCount);

    private readonly struct ReplayPlayerGamePosLookup
    {
        private readonly ReplayPlayerDto?[] playersByGamePos;
        private readonly int minGamePos;

        public ReplayPlayerGamePosLookup(ReplayPlayerDto?[] playersByGamePos, int minGamePos)
        {
            this.playersByGamePos = playersByGamePos;
            this.minGamePos = minGamePos;
        }

        public bool TryGetValue(int gamePos, out ReplayPlayerDto player)
        {
            long index = (long)gamePos - minGamePos;
            if ((ulong)index < (ulong)playersByGamePos.Length
                && playersByGamePos[index] is ReplayPlayerDto foundPlayer)
            {
                player = foundPlayer;
                return true;
            }

            player = null!;
            return false;
        }
    }
}
