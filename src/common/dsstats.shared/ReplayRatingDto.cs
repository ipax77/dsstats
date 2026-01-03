namespace dsstats.shared;

public class ReplayRatingDto
{
    public RatingType RatingType { get; set; }
    public LeaverType LeaverType { get; set; }
    public double ExpectedWinProbability { get; set; }
    public bool IsPreRating { get; set; }
    public int AvgRating { get; set; }
    public List<ReplayPlayerRatingDto> ReplayPlayerRatings { get; set; } = [];
}

public class ReplayPlayerRatingDto
{
    public RatingType RatingType { get; set; }
    public double RatingBefore { get; set; }
    public double RatingDelta { get; set; }
    public int Games { get; set; }
    public ToonIdDto ToonId { get; set; } = null!;
}

public class ReplayDetails
{
    public string ReplayHash { get; set; } = string.Empty;
    public ReplayDto Replay { get; set; } = null!;
    public List<ReplayRatingDto> ReplayRatings { get; set; } = [];
}