
using pax.dsstats.shared;

namespace pax.dsstats.shared;

public record RatingsRequest
{
    public RatingType Type { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public List<TableOrder> Orders { get; set; } = new();
    public string? Search { get; set; }
    public int? ToonId { get; set; }
    public bool Uploaders { get; set; } = true;
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
