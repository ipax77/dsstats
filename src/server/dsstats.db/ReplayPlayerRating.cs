
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db;

public class ReplayPlayerRating
{
    public int ReplayPlayerRatingId { get; set; }
    public RatingType RatingType { get; set; }
    [Precision(7, 2)]
    public double RatingBefore { get; set; }
    [Precision(7, 2)]
    public double RatingDelta { get; set; }   // RatingAfter - RatingBefore
    [Precision(7, 2)]
    public double ExpectedDelta { get; set; } // Expected rating change given rating diff
    public int Games { get; set; }
    public int ReplayRatingId { get; set; }
    public ReplayRating? ReplayRating { get; set; }
    public int ReplayPlayerId { get; set; }
    public ReplayPlayer? ReplayPlayer { get; set; }
    public int PlayerId { get; set; }
    public Player? Player { get; set; }
}