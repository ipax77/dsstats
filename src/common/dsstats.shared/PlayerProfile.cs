namespace dsstats.shared;

public sealed record PlayerProfileRequest
{
    public ToonIdDto ToonId { get; init; } = new();
    public RatingType RatingType { get; init; }
}

public sealed record PlayerOverview
{
    public ToonIdDto ToonId { get; init; } = new();
    public string Name { get; init; } = string.Empty;
    public int RegionId { get; init; }
    public RatingType RatingType { get; init; }
    public List<PlayerRatingListItem> Ratings { get; init; } = [];
    public int PercentileMaxRank { get; init; }
    public List<RatingAtDateTime> History { get; init; } = [];
    public StreakPlayerStats LongestWinStreak { get; init; } = new();
    public StreakPlayerStats LongestLoseStreak { get; init; } = new();
    public StreakPlayerStats? CurrentStreak { get; init; }
    public TopRating TopRating { get; init; } = new();
    public List<ReplayListDto> Replays { get; init; } = [];
}

public sealed record PlayerProfileDetails
{
    public RatingType RatingType { get; init; }
    public List<GameModeCount> GameModes { get; init; } = [];
    public List<CommanderCount> Commanders { get; init; } = [];
    public List<PosPlayerStats> Positions { get; init; } = [];
    public List<OtherPlayerStats> Teammates { get; init; } = [];
    public List<OtherPlayerStats> Opponents { get; init; } = [];
    public int AvgTeammateRating { get; init; }
    public int AvgOpponentRating { get; init; }
    public CmdrAvgGainResponse CommanderPerformance { get; init; } = new();
}
