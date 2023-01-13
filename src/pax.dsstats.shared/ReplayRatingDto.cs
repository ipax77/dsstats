namespace pax.dsstats.shared;

public record ReplayRatingDto
{
    public RatingType RatingType { get; set; }
    public LeaverType LeaverType { get; init; }
    public int ReplayId { get; set; }
    public List<RepPlayerRatingDto> RepPlayerRatings { get; init; } = new();
}

public record RepPlayerRatingDto
{
    public int GamePos { get; init; }
    public float Rating { get; init; }
    public float RatingChange { get; init; }
    public int Games { get; init; }
    public float Consistency { get; init; }
    public float Confidence { get; init; }
    public int ReplayPlayerId { get; init; }
}
