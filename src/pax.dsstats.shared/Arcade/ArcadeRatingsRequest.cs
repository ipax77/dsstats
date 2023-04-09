using System.Text.Json.Serialization;

namespace pax.dsstats.shared.Arcade;

public record ArcadeRatingsRequest
{
    public RatingType Type { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public List<TableOrder> Orders { get; set; } = new();
    public string? Search { get; set; }
    public int PlayerId { get; set; }
    public int RegionId { get; set; }
    [JsonIgnore]
    public RatingChangeTimePeriod TimePeriod { get; set; } = RatingChangeTimePeriod.Past10Days;
}