namespace dsstats.db;

public partial class ArcadePlayerRating
{
    public int ArcadePlayerRatingId { get; set; }

    public int RatingType { get; set; }

    public double Rating { get; set; }

    public int Pos { get; set; }

    public int Games { get; set; }

    public int Wins { get; set; }

    public int Mvp { get; set; }

    public int TeamGames { get; set; }

    public int MainCount { get; set; }

    public int Main { get; set; }

    public double Consistency { get; set; }

    public double Confidence { get; set; }

    public bool IsUploader { get; set; }

    public int ArcadePlayerId { get; set; }
    public virtual ArcadePlayer? ArcadePlayer { get; set; }

    public virtual ArcadePlayerRatingChange? ArcadePlayerRatingChange { get; set; }
}
