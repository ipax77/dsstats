
using pax.dsstats.shared.Raven;

namespace pax.dsstats.shared;

public record RatingsRequest
{
    public RatingType Type { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public List<TableOrder> Orders { get; set; } = new();
    public string? Search { get; set; }
    public int? ToonId { get; set; }
}

public record RatingsResult
{
    public int Count { get; set; }
    public List<RavenPlayerDto> Players { get; set; } = new();
}