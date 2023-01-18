
namespace pax.dsstats.shared;

public record PlayerDetailsResult
{
    public List<PlayerRatingDetailDto> Ratings { get; init; } = new();
    public List<PlayerGameModeResult> GameModes { get; init; } = new();
    public List<PlayerMatchupInfo> Matchups { get; set; } = new();
}

public record PlayerRatingChange
{
    public RatingType RatingType { get; init; }
    public int Count { get; init; }
    public float Sum { get; init; }
}

public record PlayerDetailsGroupResult
{
    public List<PlayerTeamResult> Teammates { get; init; } = new();
    public List<PlayerTeamResult> Opponents { get; init; } = new();
}

public record PlayerTeamResult
{
    public string? Name { get; init; }
    public int ToonId { get; init; }
    public int Count { get; init; }
    public int Wins { get; init; }
}

public record PlayerGameModeResult
{
    public GameMode GameMode { get; init; }
    public int PlayerCount { get; init; }
    public int Count { get; init; }
}
