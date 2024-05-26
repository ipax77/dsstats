
using System.Text.Json.Serialization;

namespace dsstats.shared;

public record RatingsRequest
{
    public RatingType Type { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public List<TableOrder> Orders { get; set; } = new();
    public string? Search { get; set; }
    public bool Uploaders { get; set; } = true;
    public bool ComboRating { get; set; }
    public bool Arcade { get; set; }
    public bool Active { get; set; }
    public int Region { get; set; }
    [JsonIgnore]
    public PlayerId? PlayerId { get; set; }
    [JsonIgnore]
    public RatingChangeTimePeriod TimePeriod { get; set; } = RatingChangeTimePeriod.Past30Days;
}

public record RatingsResult
{
    public List<PlayerRatingDto> Players { get; set; } = new();
}

public record PlayerRatingDto
{
    public double Rating { get; init; }
    public int Pos { get; init; }
    public int Games { get; init; }
    public int Wins { get; init; }
    public int Mvp { get; init; }
    public int TeamGames { get; init; }
    public int MainCount { get; init; }
    public Commander Main { get; init; }
    public bool IsUploader { get; init; }
    public PlayerRatingPlayerDto Player { get; init; } = null!;
    public PlayerRatingChangeDto? PlayerRatingChange { get; init; }
}

public record ComboPlayerRatingDto
{
    public PlayerRatingDto ComboPlayerRating { get; set; } = new();
    public PlayerRatingDto PlayerRating { get; set; } = new();
    public PlayerRatingPlayerDto Player { get; init; } = null!;
    public PlayerRatingChangeDto? PlayerRatingChange { get; init; }
    public bool IsActive { get; init; }
}