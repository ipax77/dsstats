using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using dsstats.shared8;

namespace dsstats.db;

public sealed class ReplayRating
{
    public int ReplayRatingId { get; set; }
    public RatingNgType RatingType { get; set; }
    public LeaverType LeaverType { get; set; }
    [Precision(5, 2)]
    public decimal ExpectationToWin { get; set; }
    public bool IsPreRating { get; set; }
    public int AvgRating { get; set; }
    public int ReplayId { get; set; }
    public Replay? Replay { get; set; }
}

