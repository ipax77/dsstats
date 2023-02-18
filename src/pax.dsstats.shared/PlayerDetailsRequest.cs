
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