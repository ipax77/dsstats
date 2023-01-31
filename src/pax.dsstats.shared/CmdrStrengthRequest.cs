namespace pax.dsstats.shared;

public record CmdrStrengthRequest
{
    public RatingType RatingType { get; set; }
    public TimePeriod TimePeriod { get; set; }
    public Commander Interest { get; set; }
}

public record CmdrStrengthResult
{
    public List<CmdrStrengthItem> Items { get; init; } = new();
}

public record CmdrStrengthItem
{
    public Commander Commander { get; init; }
    public int Matchups { get; init; }
    public double AvgRating { get; init; }
    public double AvgRatingGain { get; init; }
    public int Wins { get; init; }
}

