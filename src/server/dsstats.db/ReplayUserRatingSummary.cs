using Microsoft.EntityFrameworkCore;

namespace dsstats.db;

public sealed class ReplayUserRatingSummary
{
    public int ReplayUserRatingSummaryId { get; set; }
    public int ReplayId { get; set; }
    public Replay? Replay { get; set; }
    public int VoteCount { get; set; }
    public int ScoreSum { get; set; }

    [Precision(0)]
    public DateTime UpdatedAt { get; set; }
}
