using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db;

public class ArcadeReplay
{
    public int ArcadeReplayId { get; set; }
    public int RegionId { get; set; }
    public long BnetBucketId { get; set; }
    public long BnetRecordId { get; set; }
    public GameMode GameMode { get; set; }
    [Precision(0)]
    public DateTime CreatedAt { get; set; }
    public int Duration { get; set; }
    public int PlayerCount { get; set; }
    public int WinnerTeam { get; set; }
    [Precision(0)]
    public DateTime Imported { get; set; }
    public ICollection<ArcadeReplayPlayer> Players { get; set; } = [];
}

public class ArcadeReplayPlayer
{
    public int ArcadeReplayPlayerId { get; set; }
    public int SlotNumber { get; set; }
    public int Team { get; set; }
    public int ArcadeReplayId { get; set; }
    public ArcadeReplay? ArcadeReplay { get; set; }
    public int PlayerId { get; set; }
    public Player? Player { get; set; }
}

public class ReplayArcadeMatch
{
    public int ReplayArcadeMatchId { get; set; }
    public int ReplayId { get; set; }
    public int ArcadeReplayId { get; set; }
    [Precision(0)]
    public DateTime MatchTime { get; set; }
}

public class ArcadeReplayRating
{
    public int ArcadeReplayRatingId { get; set; }
    public int ExpectedWinProbability { get; set; }
    public int[] PlayerRatings { get; set; } = [];
    public int[] PlayerRatingDeltas { get; set; } = [];
    public int AvgRating { get; set; }
    public int ArcadeReplayId { get; set; }
    public ArcadeReplay? ArcadeReplay { get; set; }
}