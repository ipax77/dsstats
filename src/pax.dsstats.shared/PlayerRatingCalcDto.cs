using pax.dsstats.shared;

namespace pax.dsstats.shared;

public record PlayerRatingCalcDto
{
    public RatingType RatingType { get; init; }
    public int PlayerId { get; init; }
    public int Games { get; init; }
    public int Wins { get; init; }
    public int Mvp { get; init; }
    public int TeamGames { get; init; }
    public double Rating { get; init; }
    public int MainCount { get; init; }
    public Commander Main { get; init; }
    public string MmrOverTime { get; init; } = string.Empty;
    public double Consistency { get; init; }
    public double Confidence { get; init; }
    public bool IsUploader { get; init; }
}
