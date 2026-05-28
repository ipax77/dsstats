namespace dsstats.play;

internal static class SpawnPlaybackAliveUnits
{
    public static SpawnPlaybackAliveUnitSummary UpdateRows(
        SpawnPlaybackReplayNg? playback,
        int renderGameloop,
        bool showAliveUnits,
        List<AliveUnitRow> team1AliveUnitRows,
        List<AliveUnitRow> team2AliveUnitRows,
        Dictionary<AliveUnitKey, AliveUnitAccumulator> aliveUnitAccumulators)
    {
        team1AliveUnitRows.Clear();
        team2AliveUnitRows.Clear();
        aliveUnitAccumulators.Clear();
        if (playback is null)
        {
            return default;
        }

        var unitRows = SpawnPlaybackBinaryPayloads.GetPayload(playback, SpawnPlaybackBinaryPayloads.UnitRowsDatasetId);
        if (unitRows is null)
        {
            return default;
        }

        int aliveUnitCount = 0;
        int team1AliveCount = 0;
        int team2AliveCount = 0;
        int currentSpawnNumber = 0;
        byte[] unitBytes = unitRows.Bytes;
        ReadOnlySpan<byte> killBytes = SpawnPlaybackBinaryPayloads.GetPayload(playback, SpawnPlaybackBinaryPayloads.KillGameloopsDatasetId)?.Bytes ?? [];
        for (int rowIndex = 0; rowIndex < unitRows.Count; rowIndex++)
        {
            int spawnGameloop = SpawnPlaybackBinaryPayloads.ReadUnitRowInt(unitBytes, rowIndex, SpawnPlaybackBinaryPayloads.UnitRowSpawnGameloopOffset);
            if (spawnGameloop > renderGameloop)
            {
                continue;
            }

            int spawnNumber = SpawnPlaybackBinaryPayloads.ReadUnitRowInt(unitBytes, rowIndex, SpawnPlaybackBinaryPayloads.UnitRowSpawnNumberOffset);
            currentSpawnNumber = Math.Max(currentSpawnNumber, spawnNumber);

            int expiresGameloop = SpawnPlaybackBinaryPayloads.ReadUnitRowInt(unitBytes, rowIndex, SpawnPlaybackBinaryPayloads.UnitRowExpiresGameloopOffset);
            if (expiresGameloop <= renderGameloop)
            {
                continue;
            }

            int playerIndex = SpawnPlaybackBinaryPayloads.ReadUnitRowInt(unitBytes, rowIndex, SpawnPlaybackBinaryPayloads.UnitRowPlayerIndexOffset);
            int unitKindIndex = SpawnPlaybackBinaryPayloads.ReadUnitRowInt(unitBytes, rowIndex, SpawnPlaybackBinaryPayloads.UnitRowUnitKindIndexOffset);
            if ((uint)playerIndex >= (uint)playback.Players.Count
                || (uint)unitKindIndex >= (uint)playback.UnitKinds.Count)
            {
                continue;
            }

            var player = playback.Players[playerIndex];
            var unitKind = playback.UnitKinds[unitKindIndex];
            aliveUnitCount++;
            if (player.TeamId == 1)
            {
                team1AliveCount++;
            }
            else if (player.TeamId == 2)
            {
                team2AliveCount++;
            }

            if (!showAliveUnits)
            {
                continue;
            }

            int killOffset = SpawnPlaybackBinaryPayloads.ReadUnitRowInt(unitBytes, rowIndex, SpawnPlaybackBinaryPayloads.UnitRowKillOffsetOffset);
            int killCount = SpawnPlaybackBinaryPayloads.ReadUnitRowInt(unitBytes, rowIndex, SpawnPlaybackBinaryPayloads.UnitRowKillCountOffset);
            var key = new AliveUnitKey(player.TeamId, player.Commander, unitKind.Name);
            aliveUnitAccumulators.TryGetValue(key, out var row);
            aliveUnitAccumulators[key] = row.Add(unitKind.Color, GetCurrentKills(killBytes, killOffset, killCount, renderGameloop));
        }

        if (showAliveUnits)
        {
            BuildRows(aliveUnitAccumulators, team1AliveUnitRows, team2AliveUnitRows);
        }

        return new(aliveUnitCount, team1AliveCount, team2AliveCount, currentSpawnNumber);
    }

    public static string CreateHighlightKey(int teamId, string commander, string unitName)
    {
        return $"{teamId}|{commander.Length}:{commander}|{unitName.Length}:{unitName}";
    }

    public static int GetCurrentKills(ReadOnlySpan<byte> killGameloops, int killOffset, int killCount, int currentGameloop)
    {
        int kills = 0;
        while (kills < killCount && SpawnPlaybackBinaryPayloads.ReadInt32(killGameloops, checked((killOffset + kills) * sizeof(int))) <= currentGameloop)
        {
            kills++;
        }

        return kills;
    }

    private static void BuildRows(
        Dictionary<AliveUnitKey, AliveUnitAccumulator> aliveUnitAccumulators,
        List<AliveUnitRow> team1AliveUnitRows,
        List<AliveUnitRow> team2AliveUnitRows)
    {
        foreach (var pair in aliveUnitAccumulators)
        {
            var row = new AliveUnitRow(
                pair.Key.TeamId,
                pair.Key.Commander,
                pair.Key.UnitName,
                CreateHighlightKey(pair.Key.TeamId, pair.Key.Commander, pair.Key.UnitName),
                GetTeamColor(pair.Key.TeamId),
                pair.Value.UnitColor,
                pair.Value.AliveCount,
                pair.Value.CurrentKills);
            if (pair.Key.TeamId == 1)
            {
                team1AliveUnitRows.Add(row);
            }
            else if (pair.Key.TeamId == 2)
            {
                team2AliveUnitRows.Add(row);
            }
        }

        team1AliveUnitRows.Sort(CompareAliveUnitRows);
        team2AliveUnitRows.Sort(CompareAliveUnitRows);
    }

    private static int CompareAliveUnitRows(AliveUnitRow left, AliveUnitRow right)
    {
        int unitCompare = string.Compare(left.UnitName, right.UnitName, StringComparison.Ordinal);
        return unitCompare != 0
            ? unitCompare
            : string.Compare(left.Commander, right.Commander, StringComparison.Ordinal);
    }

    private static string GetTeamColor(int teamId)
    {
        return teamId == 1 ? "#5DADEC" : "#F87171";
    }
}
