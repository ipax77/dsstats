using Microsoft.EntityFrameworkCore;

namespace dsstats.db;

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