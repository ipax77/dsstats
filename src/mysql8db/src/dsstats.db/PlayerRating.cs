using dsstats.shared;

namespace dsstats.db;

public sealed class PlayerRating
{
    public int PlayerRatingId { get; set; }
    public RatingNgType RatingType { get; set; }
    public int Games { get; set; }
    public int DsstatsGames { get; set; }
    public int ArcadeGames { get; set; }
    public int Wins { get; set; }
    public int Mvp { get; set; }
    public double Rating { get; set; } = 1000;
    public double Consistency { get; set; }
    public double Confidence { get; set; }
    public Commander Main { get; set; }
    public int MainCount { get; set; }
    public int Pos { get; set; }
    public int PlayerId { get; set; }
    public Player? Player { get; set; }
}

