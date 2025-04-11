using Microsoft.EntityFrameworkCore;
using dsstats.shared8;

namespace dsstats.db;

public class ReplayPlayerRating
{
    public int ReplayPlayerRatingId { get; set; }
    public RatingNgType RatingType { get; set; }
    public int GamePos { get; set; }
    public int Rating { get; set; }
    [Precision(5, 2)]
    public decimal Change { get; set; }
    public int Games { get; set; }
    [Precision(5, 2)]
    public decimal Consistency { get; set; }
    [Precision(5, 2)]
    public decimal Confidence { get; set; }
    public int ReplayPlayerId { get; set; }
    public ReplayPlayer? ReplayPlayer { get; set; }
}

