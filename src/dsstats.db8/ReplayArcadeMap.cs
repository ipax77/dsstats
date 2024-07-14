using Microsoft.EntityFrameworkCore;

namespace dsstats.db8;

public class ReplayArcadeMatch
{
    public int ReplayArcadeMatchId { get; set; }
    public int ReplayId { get; set; }
    public int ArcadeReplayId { get; set; }
    [Precision(0)]
    public DateTime MatchTime { get; set; }
}
