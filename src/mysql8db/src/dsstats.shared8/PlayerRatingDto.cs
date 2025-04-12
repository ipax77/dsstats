using dsstats.shared;

namespace dsstats.shared8;

public sealed record PlayerRatingDto
{
    public RatingNgType RatingType { get; set; }
    public int Games { get; set; }
    public int DsstatsGames { get; set; }
    public int ArcadeGames { get; set; }
    public int Wins { get; set; }
    public int Mvp { get; set; }
    public double Rating { get; set; }
    public double Consistency { get; set; }
    public double Confidence { get; set; }
    public int Pos { get; set; }
}

public sealed record PlayerStatsResponse
{
    public RatingNgType RatingType { get; set; }
    public List<PlayerRatingDto> PlayerRatings { get; set; } = [];
    public List<PlayerCmdrAvgGain> PlayerCmdrAvgGains { get; set; } = [];
}