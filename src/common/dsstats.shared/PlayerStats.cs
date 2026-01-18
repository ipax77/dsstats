namespace dsstats.shared;

public sealed record PlayerStatsRequest
{
    public ToonIdDto ToonId { get; set; } = new();
    public PlayerDto Player { get; set; } = new();
    public RatingType RatingType { get; set; }
    public TimePeriod TimePeriod { get; set; } = TimePeriod.Last90Days;
}

public sealed record PlayerStatsResponse
{
    public string Name { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public ToonIdDto ToonId { get; set; } = new();
    public List<PlayerRatingListItem> Ratings { get; set; } = [];
    public List<RatingDetails> RatingDetails { get; set; } = [];
}

public sealed record RatingDetails
{
    public RatingType RatingType { get; set; }
    public int PercentileMaxRank { get; set; }
    public List<GameModeCount> GameModes { get; set; } = [];
    public List<CommanderCount> Commanders { get; set; } = [];
    public List<RatingAtDateTime> Ratings { get; set; } = [];
    public List<ReplayListDto> Replays { get; set; } = [];
    public List<CmdrAvgGainResponse> AvgGainResponses { get; set; } = [];
    public List<OtherPlayerStats> TeammateStats { get; set; } = [];
    public List<OtherPlayerStats> OpponentStats { get; set; } = [];
    public List<PosPlayerStats> PosStats { get; set; } = [];
    public int AvgTeammateRating { get; set; }
    public int AvgOpponentRating { get; set; }
    public StreakPlayerStats LongestWinStreak { get; set; } = new();
    public StreakPlayerStats LongestLoseStreak { get; set; } = new();
    public StreakPlayerStats? CurrentStreak { get; set; } = new();
    public TopRating TopRating { get; set; } = new();
}

public sealed record GameModeCount
{
    public GameMode GameMode { get; set; }
    public int Count { get; set; }
}

public sealed record CommanderCount
{
    public Commander Commander { get; set; }
    public int Count { get; set; }
}

public sealed record RatingAtDateTime
{
    public int Year { get; set; }
    public int Week { get; set; }
    public int Games { get; set; }
    public float Rating { get; set; }
}

public sealed record PlayerCmdrAvgGain
{
    public Commander Commander { get; init; }
    public int Count { get; set; }
    public int Wins { get; set; }
    public double AvgGain { get; set; }
}

public sealed record CmdrAvgGainResponse
{
    public TimePeriod TimePeriod { get; init; }
    public List<PlayerCmdrAvgGain> AvgGains { get; init; } = [];
}

public sealed record OtherPlayerStats
{
    public PlayerDto Player { get; set; } = null!;
    public int Count { get; set; }
    public int Wins { get; set; }
    public float AvgGain { get; set; }
}

public sealed record PosPlayerStats
{
    public int GamePos { get; set; }
    public int Count { get; set; }
    public int Wins { get; set; }
}

public sealed record StreakPlayerStats
{
    public int Count { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public sealed record TopRating
{
    public double Rating { get; set; }
    public DateTime DateAchieved { get; set; }
}