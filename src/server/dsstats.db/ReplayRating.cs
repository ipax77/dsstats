
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db;

public class ReplayRating
{
    public int ReplayRatingId { get; set; }
    public RatingType RatingType { get; set; }
    public LeaverType LeaverType { get; set; }
    [Precision(5, 2)]
    public double ExpectedWinProbability { get; set; }
    public bool IsPreRating { get; set; }
    public int AvgRating { get; set; }
    public int ReplayId { get; set; }
    public Replay? Replay { get; set; }
    public ICollection<ReplayPlayerRating> ReplayPlayerRatings { get; set; } = [];
}
