namespace dsstats.play;

public sealed record SpawnPlaybackReplay(
    int DurationGameloop,
    int StepGameloops,
    SpawnPlaybackBounds Bounds,
    SpawnPlaybackStats Stats,
    IReadOnlyList<SpawnPlaybackLandmark> Landmarks,
    IReadOnlyList<SpawnPlaybackBuildUnit> BuildUnits,
    IReadOnlyList<SpawnPlaybackPlayer> Players);

public sealed record SpawnPlaybackPlayer(
    string Name,
    int TeamId,
    int GamePos,
    string Commander,
    IReadOnlyList<SpawnPlaybackUnit> Units);

public sealed record SpawnPlaybackUnit(
    int UnitIndex,
    string Name,
    int SpawnGameloop,
    double SpawnX,
    double SpawnY,
    int? DiedGameloop,
    double? DiedX,
    double? DiedY,
    double TargetX,
    double TargetY,
    double Radius,
    string Color,
    int Kills);

public sealed record SpawnPlaybackBounds(double MinX, double MinY, double MaxX, double MaxY);

public sealed record SpawnPlaybackLandmark(
    string Name,
    string Kind,
    int TeamId,
    double X,
    double Y,
    double Radius,
    string Color,
    int Kills);

public sealed record SpawnPlaybackBuildUnit(
    string PlayerName,
    int TeamId,
    int GamePos,
    string Name,
    int BuiltGameloop,
    double X,
    double Y,
    int SpawnedUnitCount,
    int Kills,
    double Radius,
    string Color);

public readonly record struct SpawnPlaybackUnitKey(
    int UnitIndex,
    int SpawnGameloop,
    int SpawnX,
    int SpawnY,
    string Name);

public sealed record SpawnPlaybackStats(
    int PlayerCount,
    int SpawnCount,
    int UnitCount,
    int UnitsWithDiedEvent,
    int UnitsWithDiedPosition,
    int MaxSimultaneousActiveSpawns);
