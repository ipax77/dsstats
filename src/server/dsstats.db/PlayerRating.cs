
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db;

public class PlayerRating
{
    public int PlayerRatingId { get; set; }
    public RatingType RatingType { get; set; }
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Mvps { get; set; }
    public Commander Main { get; set; }
    public int MainCount { get; set; }
    public int Change { get; set; }
    public double Rating { get; set; }
    public double Consistency { get; set; }
    public double Confidence { get; set; }
    public int Position { get; set; }
    [Precision(0)]
    public DateTime LastGame { get; set; }
    public int PlayerId { get; set; }
    public Player? Player { get; set; }
}
