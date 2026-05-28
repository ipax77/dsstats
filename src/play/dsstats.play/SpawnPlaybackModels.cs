namespace dsstats.play;

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

public sealed record SpawnPlaybackStats(
    int PlayerCount,
    int SpawnCount,
    int UnitCount,
    int UnitsWithDiedEvent,
    int UnitsWithDiedPosition,
    int MaxSimultaneousActiveSpawns);
