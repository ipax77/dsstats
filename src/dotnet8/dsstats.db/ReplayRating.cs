using dsstats.shared;

namespace dsstats.db;

public partial class ReplayRating
{
    public ReplayRating()
    {
        RepPlayerRatings = new HashSet<RepPlayerRating>();
    }

    public int ReplayRatingId { get; set; }

    public RatingType RatingType { get; set; }

    public LeaverType LeaverType { get; set; }

    public float ExpectationToWin { get; set; }

    public int ReplayId { get; set; }

    public bool IsPreRating { get; set; }
    public virtual ICollection<RepPlayerRating> RepPlayerRatings { get; set; }
}
