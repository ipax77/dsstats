
namespace dsstats.shared;

public record ReplayRatingDto
{
    public RatingType RatingType { get; set; }
    public LeaverType LeaverType { get; init; }
    public double ExpectationToWin { get; init; } // WinnerTeam
    public int ReplayId { get; set; }
    public bool IsPreRating { get; set; }
    public List<RepPlayerRatingDto> RepPlayerRatings { get; set; } = new();
}

public record RepPlayerRatingDto
{
    public int GamePos { get; init; }
    public int Rating { get; init; }
    public double RatingChange { get; init; }
    public int Games { get; init; }
    public double Consistency { get; init; }
    public double Confidence { get; init; }
    public int ReplayPlayerId { get; init; }
}
