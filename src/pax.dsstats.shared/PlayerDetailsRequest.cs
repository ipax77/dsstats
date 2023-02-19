
namespace pax.dsstats.shared;

public record PlayerDetailRequest
{
    public RequestNames RequestNames { get; set; } = new();
    public TimePeriod TimePeriod { get; set; }
    public RatingType RatingType { get; set; }
    public Commander Interest { get; set; }
    public bool Complete { get; set; }
}

public record PlayerDetailResponse
{
    public List<CmdrStrengthItem> CmdrStrengthItems { get; set; } = new();
}

public record PlayerDetailSummary
{
    public List<PlayerGameModeResult> GameModesPlayed { get; set; } = new();
    public List<PlayerRatingDetailDto> Ratings { get; set; } = new();
    public List<CommanderInfo> Commanders { get; set; } = new();
}

public record CommanderInfo
{
    public Commander Cmdr { get; set; }
    public int Count { get; set; }
}