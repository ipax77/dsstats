using dsstats.shared;

namespace dsstats.db8.Ratings;

public class PlayerNgRating
{
    public int PlayerNgRatingId { get; set; }
    public RatingCalcType RatingCalcType { get; set; }
    public RatingType RatingType { get; set; }
    public double Rating { get; set; }
    public int Games { get; set; }
    public int Wins { get; set; }
    public double Consistency { get; set; }
    public double Confidence { get; set; }
    public int Pos { get; set; }
    public int PlayerId { get; set; }
    public virtual Player? Player { get; set; }
    public int Mvp { get; set; }
    public int MainCount { get; set; }
    public Commander MainCmdr { get; set; }
}

public class ReplayNgRating
{
    public int ReplayNgRatingId { get; set; }
    public RatingType RatingType { get; set; }
    public LeaverType LeaverType { get; set; }
    public float Exp2Win { get; set; }
    public int AvgRating { get; set; }
    public bool IsPreRating { get; set; }
    public int ReplayId { get; set; }
    public virtual Replay? Replay { get; set; }
}

public class ReplayPlayerNgRating
{
    public int ReplayPlayerNgRatingId { get; set; }
    public float Rating { get; set; }
    public float Change { get; set; }
    public int Games {  get; set; }
    public float Consistency { get; set; }
    public float Confidence { get; set; }
    public int ReplayPlayerId { get; set; }
    public virtual ReplayPlayer? ReplayPlayer { get; set; }
}
