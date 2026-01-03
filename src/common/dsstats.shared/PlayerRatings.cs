using System.Text.Json.Serialization;

namespace dsstats.shared;

public sealed class PlayerRatingsRequest
{
    public RatingType RatingType { get; set; } = RatingType.All;
    public string Name { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public bool IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20_000;
    public int Skip { get; set; }
    public int Take { get; set; }
    public List<TableOrder> Orders { get; set; } = [];
    [JsonIgnore]
    public string? ToonIdString { get; set; }
}

public sealed class PlayerRatingListItem
{
    public RatingType RatingType { get; set; }
    public int PlayerId { get; set; }
    public string ToonIdString { get; set; } = string.Empty;
    public int Pos { get; set; }
    public int RegionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Commander Main { get; set; }
    public int MainCount { get; set; }
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Mvps { get; set; }
    public double Rating { get; set; }
    public int Change { get; set; }
    public double Cons { get; set; }
    public double Conf { get; set; }
}
