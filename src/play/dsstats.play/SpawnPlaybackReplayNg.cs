namespace dsstats.play;

public sealed record SpawnPlaybackReplayNg(
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
    IReadOnlyList<SpawnPlaybackBinaryPayloadNg> BinaryPayloads);


public sealed record SpawnPlaybackPlayerNg(
    string Name,
    int TeamId,
    int GamePos,
    string Commander,
    IReadOnlyList<int> RefineryGameloops,
    IReadOnlyList<int> TierUpgradeGameloops);

public sealed record SpawnPlaybackUnitKindNg(
    string Name,
    string Commander,
    double Radius,
    string Color);

public sealed record SpawnPlaybackBinaryPayloadNg(
    string DatasetId,
    byte[] Bytes,
    int Count,
    SpawnPlaybackBinaryDataFormatNg Format,
    int XOffset = 0,
    int YOffset = 0,
    int? ByteStride = null);

public enum SpawnPlaybackBinaryDataFormatNg
{
    Int32Y,
    Int32Rows,
    Float32Rows
}
