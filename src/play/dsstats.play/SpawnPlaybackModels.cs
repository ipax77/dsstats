namespace dsstats.play;

public sealed record SpawnPlaybackReplay(
    int DurationGameloop,
    int StepGameloops,
    SpawnPlaybackBounds Bounds,
    SpawnPlaybackStats Stats,
    SpawnPlaybackSummary Summary,
    SpawnPlaybackMiddleControl MiddleControl,
    IReadOnlyList<SpawnPlaybackLandmark> Landmarks,
    IReadOnlyList<SpawnPlaybackBuildUnit> BuildUnits,
    IReadOnlyList<SpawnPlaybackSnapshot> Snapshots,
    IReadOnlyList<SpawnPlaybackPlayer> Players);

public sealed record SpawnPlaybackPlayer(
    string Name,
    int TeamId,
    int GamePos,
    string Commander,
    IReadOnlyList<int> RefineryGameloops,
    IReadOnlyList<int> TierUpgradeGameloops,
    IReadOnlyList<SpawnPlaybackUnit> Units);

public sealed record SpawnPlaybackUnit(
    int UnitIndex,
    string Name,
    int SpawnNumber,
    int SpawnGameloop,
    double SpawnX,
    double SpawnY,
    int? DiedGameloop,
    double? DiedX,
    double? DiedY,
    double TargetX,
    double TargetY,
    int ExpiresGameloop,
    double Radius,
    string Color,
    IReadOnlyList<int> KillGameloops);

public sealed record SpawnPlaybackBounds(double MinX, double MinY, double MaxX, double MaxY);

public sealed record SpawnPlaybackSnapshot(
    int SpawnNumber,
    int StartGameloop,
    int EndGameloop);

public sealed record SpawnPlaybackMiddleControl(
    int FirstTeamId,
    IReadOnlyList<int> ChangeGameloops);

public sealed record SpawnPlaybackLandmark(
    string Name,
    string Kind,
    int TeamId,
    double X,
    double Y,
    double Radius,
    string Color,
    int Kills,
    int? DiedGameloop = null);

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

public sealed record SpawnPlaybackSummary(
    int TotalKills,
    IReadOnlyList<SpawnPlaybackPlayerSummary> Players,
    IReadOnlyList<SpawnPlaybackTopUnitSummary> TopUnits);

public sealed record SpawnPlaybackPlayerSummary(
    string PlayerName,
    int TeamId,
    int GamePos,
    string Commander,
    int Kills);

public sealed record SpawnPlaybackTopUnitSummary(
    string PlayerName,
    int TeamId,
    int GamePos,
    string UnitName,
    int Kills);

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
