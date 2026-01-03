namespace dsstats.shared;

public sealed class ReplayCalcDto
{
    public int ReplayId { get; init; }
    public DateTime Gametime { get; init; }
    public GameMode GameMode { get; init; }
    public int PlayerCount { get; init; }
    public int WinnerTeam { get; init; }
    public bool TE { get; init; }
    public bool IsArcade { get; init; }
    public List<PlayerCalcDto> Players { get; init; } = [];
}

public sealed class PlayerCalcDto
{
    public int ReplayPlayerId { get; init; }
    public bool IsLeaver { get; init; }
    public bool IsMvp { get; init; }
    public int Team { get; init; }
    public Commander Race { get; init; }
    public int PlayerId { get; init; }
    public PlayerRatingCalcDto Rating { get; set; } = new();
}

public sealed class PlayerRatingCalcDto
{
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Mvps { get; set; }
    public double Change { get; set; }
    public double Rating { get; set; }
    public double Consistency { get; set; }
    public double Confidence { get; set; }
    public DateTime LastGame { get; set; }
    public Dictionary<Commander, int> CmdrCounts { get; set; } = [];

    public PlayerRatingCalcDto ShallowCopy()
    {
        return new PlayerRatingCalcDto
        {
            Games = this.Games,
            Wins = this.Wins,
            Mvps = this.Mvps,
            Change = this.Change,
            Rating = this.Rating,
            Consistency = this.Consistency,
            Confidence = this.Confidence,
        };
    }
}