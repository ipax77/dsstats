using System.Text.Json.Serialization;

namespace pax.dsstats.shared;

public record RatingsRequest
{
    public RatingType Type { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public List<TableOrder> Orders { get; set; } = new();
    public string? Search { get; set; }
    public int? ToonId { get; set; }
    public int RegionId { get; set; }
    public int RealmId { get; set; }
    public bool Uploaders { get; set; } = true;
    [JsonIgnore]
    public RatingChangeTimePeriod TimePeriod { get; set; } = RatingChangeTimePeriod.Past10Days;
}

public record RatingsResult
{
    public List<PlayerRatingDto> Players { get; set; } = new();
}

public record ToonIdRatingRequest
{
    public RatingType RatingType { get; set; }
    public List<int> ToonIds { get; set; } = new();
}

public record ToonIdRatingResponse
{
    public List<PlayerRatingDetailDto> Ratings { get; set; } = new();
}
