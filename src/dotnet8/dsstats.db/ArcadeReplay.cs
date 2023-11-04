namespace dsstats.db;

public partial class ArcadeReplay
{
    public ArcadeReplay()
    {
        ArcadeReplayPlayers = new HashSet<ArcadeReplayPlayer>();
    }
    public int ArcadeReplayId { get; set; }

    public int RegionId { get; set; }

    public int GameMode { get; set; }
    public DateTime CreatedAt { get; set; }

    public int Duration { get; set; }

    public int PlayerCount { get; set; }

    public bool TournamentEdition { get; set; }

    public int WinnerTeam { get; set; }

    public long BnetBucketId { get; set; }

    public long BnetRecordId { get; set; }
    public DateTime Imported { get; set; }
    public string ReplayHash { get; set; } = null!;
    public virtual ArcadeReplayRating? ArcadeReplayRating { get; set; }
    public virtual ICollection<ArcadeReplayPlayer> ArcadeReplayPlayers { get; set; }
}
