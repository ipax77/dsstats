namespace dsstats.shared.Maui;

public record MauiRatingRequest
{
    public List<PlayerId> PlayerIds { get; set; } = new();
    public RatingType RatingType { get; set; }
    public RatingCalcType RatingCalcType { get; set; }
}

public record MauiRatingResponse
{
    public RatingType RatingType { get; set; }
    public RatingCalcType RatingCalcType { get; set; }
    public List<MauiRatingInfo> RatingInfos { get; set; } = new();
}

public record MauiRatingInfo
{
    public RequestNames RequestNames { get; set; } = null!;
    public int Rating { get; set; }
    public int Pos { get; set; }
}
