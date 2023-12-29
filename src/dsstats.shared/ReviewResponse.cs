namespace dsstats.shared;

public record ReviewRequest
{

}

public record ReviewResponse
{
    public List<CommanderReviewInfo> CommanderInfos { get; set; } = new();
    public List<ReplayPlayerReviewDto> RatingInfos { get; set; } = new();
}

public record ReplayPlayerReviewDto
{
    public ReplayChartDto Replay { get; set; } = new();
    public RepPlayerRatingChartDto? ReplayPlayerRatingInfo { get; set; }
}

public record CommanderReviewInfo
{
    public Commander Cmdr { get; set; }
    public int Count { get; set; }
    public RatingType RatingType { get; set; }
}