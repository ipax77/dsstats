
using dsstats.shared;

namespace dsstats.db;

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



