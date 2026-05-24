namespace dsstats.shared;

public sealed class ReplayUserRatingDto
{
    public double Average { get; set; }
    public int VoteCount { get; set; }
    public int? CurrentVote { get; set; }
    public DateTime? NextAllowedVoteAt { get; set; }
}

public sealed class ReplayUserRatingRequest
{
    public int Score { get; set; }
}
