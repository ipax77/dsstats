namespace dsstats.shared.Arcade;

public class ArcadeReplayDto
{
    public int RegionId { get; set; }
    public long BnetBucketId { get; set; }
    public long BnetRecordId { get; set; }
    public GameMode GameMode { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Duration { get; set; }
    public int PlayerCount { get; set; }
    public int WinnerTeam { get; set; }
    public List<ArcadeReplayPlayerDto> Players { get; set; } = [];
}

public class ArcadeReplayPlayerDto
{
    public int SlotNumber { get; set; }
    public int Team { get; set; }
    public int ArcadeReplayId { get; set; }
    public PlayerDto Player { get; set; } = new();
}

public sealed record ArcadeReplayKey(int RegionId, long BnetBucketId, long BnetRecordId);
