
using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;

namespace pax.dsstats.dbng;

public class ComboPlayerRating
{
    public int ComboPlayerRatingId { get; set; }
    public RatingType RatingType { get; set; }
    public int Games { get; set; }
    public int Wins { get; set; }
    public double Rating { get; set; }
    public double Consistency { get; set; }
    public double Confidence { get; set; }
    public int Pos { get; set; }
    public int PlayerId { get; set; }
    public virtual Player Player { get; set; } = null!;
}

public class ComboReplayRating
{
    public int ComboReplayRatingId { get; set; }
    public RatingType RatingType { get; set; }
    public LeaverType LeaverType { get; set; }
    [Precision(5, 2)]
    public double ExpectationToWin { get; set; }
    public int ReplayId { get; set; }
    public virtual Replay Replay { get; set;} = null!;
    public bool IsPreRating { get; set; }
}

public class ComboReplayPlayerRating
{
    public int ComboReplayPlayerRatingId { get; set; }
    public int GamePos { get; set; }
    public int Rating { get; set; }
    [Precision(5, 2)]
    public double Change { get; set; }
    public int Games { get; set; }
    [Precision(5, 2)]
    public double Consistency { get; set; }
    [Precision(5, 2)]
    public double Confidence { get; set; }
    public int ReplayPlayerId { get; set; }
    public virtual ReplayPlayer ReplayPlayer { get; set; } = null!;
}