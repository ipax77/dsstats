namespace pax.dsstats.shared;

public record RatingChangesResult
{
    public List<PlayerRatingStat> PlayerRatingStats { get; set; } = new();
}

public record RatingChangesRequest
{
    public RatingType RatingType { get; set; }
    public RatingChangeTimePeriod TimePeriod { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public List<TableOrder> Orders { get; set; } = new()
    {
        new()
        {
            Property = "RatingChange",
        },
    };
    public string Search { get; set; } = string.Empty;
}