
namespace pax.dsstats.shared;

public record BuildRatingRequest
{
    public RatingType RatingType { get; set; }
    public TimePeriod TimePeriod { get; set; }
    public Commander Interest { get; set; }
    public Commander Vs { get; set; }
    public Breakpoint Breakpoint { get; set; }
    public int FromRating { get; set; }
    public int ToRating { get; set; }
}

public record BuildRatingResponse
{
    public int Count { get; set; }
    public double Winrate { get; set; }
    public double UpgradesSpent { get; set; }
    public List<BuildRatingUnit> Units { get; set; } = new();
}

public record BuildRatingUnit
{
    public string Name { get; set; } = string.Empty;
    public double Avg { get; set; }
}
