namespace dsstats.play;

internal sealed record SpawnPlaybackReplayNgMetadata(
    int DurationGameloop,
    int StepGameloops,
    SpawnPlaybackBounds Bounds,
    SpawnPlaybackStats Stats,
    SpawnPlaybackSummary Summary,
    SpawnPlaybackMiddleControl MiddleControl,
    IReadOnlyList<SpawnPlaybackLandmark> Landmarks,
    IReadOnlyList<SpawnPlaybackSnapshot> Snapshots,
    IReadOnlyList<SpawnPlaybackPlayerNg> Players,
    IReadOnlyList<SpawnPlaybackUnitKindNg> UnitKinds,
    IReadOnlyList<SpawnPlaybackBinaryPayloadMetadataNg> BinaryPayloads);

internal sealed record SpawnPlaybackBinaryPayloadMetadataNg(
    string DatasetId,
    int Count,
    SpawnPlaybackBinaryDataFormatNg Format,
    int XOffset,
    int YOffset,
    int? ByteStride);

internal sealed record AliveUnitRow(
    int TeamId,
    string Commander,
    string UnitName,
    string HighlightKey,
    string TeamColor,
    string? UnitColor,
    int AliveCount,
    int CurrentKills);

internal readonly record struct AliveUnitKey(int TeamId, string Commander, string UnitName);

internal readonly record struct AliveUnitAccumulator(int AliveCount, int CurrentKills, string? UnitColor)
{
    public AliveUnitAccumulator Add(string unitColor, int kills)
    {
        return new(AliveCount + 1, CurrentKills + kills, UnitColor ?? unitColor);
    }
}

internal readonly record struct SpawnPlaybackAliveUnitSummary(
    int AliveUnitCount,
    int Team1AliveCount,
    int Team2AliveCount,
    int CurrentSpawnNumber);

internal sealed record SpawnPlaybackLifeCostEntry(string Key, int Cost, int Life);
