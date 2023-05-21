using pax.dsstats.shared;

namespace pax.dsstats.dbng;

public class ReplayRating
{
    public ReplayRating()
    {
        RepPlayerRatings = new HashSet<RepPlayerRating>();
    }
    public int ReplayRatingId { get; set; }
    public RatingType RatingType { get; set; }
    public LeaverType LeaverType { get; set; }
    public float ExpectationToWin { get; set; } // WinnerTeam
    public int ReplayId { get; set; }
    public Replay Replay { get; set; } = null!;
    public bool IsPreRating { get; set; }
    public virtual ICollection<RepPlayerRating> RepPlayerRatings { get; set; }
}

public class RepPlayerRating
{
    public int RepPlayerRatingId { get; set; }
    public int GamePos { get; set; }
    public float Rating { get; set; }
    public float RatingChange { get; set; }
    public int Games { get; set; }
    public float Consistency { get; set; }
    public float Confidence { get; set; }
    public int ReplayPlayerId { get; set; }
    public ReplayPlayer ReplayPlayer { get; set; } = null!;
    public int ReplayRatingInfoId { get; set; }
    public ReplayRating ReplayRatingInfo { get; set; } = null!;
}