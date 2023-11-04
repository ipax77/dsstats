using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db;

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