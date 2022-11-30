
namespace pax.dsstats.shared;

public record ReplayDsRDto
{
    public int ReplayId { get; init; }
    public string ReplayHash { get; init; } = null!;
    public DateTime GameTime { get; init; }
    public byte Playercount { get; init; }
    public int Maxleaver { get; init; }
    public int WinnerTeam { get; set; }
    public int Duration { get; init; }
    public int Maxkillsum { get; init; }
    public GameMode GameMode { get; init; }
    public List<ReplayPlayerDsRDto> ReplayPlayers { get; init; } = new();
}

public record ReplayPlayerDsRDto
{
    public int ReplayPlayerId { get; init; }
    public int GamePos { get; init; }
    public int Team { get; init; }
    public PlayerResult PlayerResult { get; init; }
    public PlayerDsRDto Player { get; init; } = null!;
    public Commander Race { get; init; }
    public Commander OppRace { get; init; }
    public int Duration { get; init; }
    public bool IsUploader { get; init; }
    public int Kills { get; init; }
}

public record PlayerDsRDto
{
    public int PlayerId { get; init; }
    public string Name { get; init; } = null!;
    public int ToonId { get; init; }
    public int RegionId { get; init; }
    public int NotUploadCount { get; init; }
}

public record PlayerRatingDto
{
    public int PlayerId { get; init; }
    public string Name { get; init; } = null!;
    public int ToonId { get; init; }
    public int RegionId { get; init; }
    public Commander Main { get; set; }
    public float MainPercentage { get; set; }
    public RatingStatsDto CmdrRatingStats { get; init; } = new();
    public RatingStatsDto StdRatingStats { get; init; } = new();
}

public record RatingStatsDto
{
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Mvp { get; set; }
    public int TeamGames { get; set; }

    public int MmrGames { get; set; }
    public double Mmr { get; set; }
    public double Consistency { get; set; }
    public double Confidence { get; set; }
}
