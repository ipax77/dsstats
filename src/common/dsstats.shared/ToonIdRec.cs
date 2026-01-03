namespace dsstats.shared;

public sealed record ToonIdRec(int Region, int Realm, int Id);
public sealed record PlayerInfo(int PlayerId, string Name);

public sealed record ReplayMatchDto
{
    public int ReplayId { get; init; }
    public DateTime Gametime { get; init; }
    public GameMode GameMode { get; init; }
    public int Duration { get; init; }
    public int PlayerCount { get; init; }
    public int WinnerTeam { get; init; }
    public List<PlayerMatchDto> Players { get; init; } = [];
}

public sealed record PlayerMatchDto
{
    public int ReplayPlayerId { get; init; }
    public int Team { get; init; }
    public ToonIdDto ToonId { get; init; } = new();
}