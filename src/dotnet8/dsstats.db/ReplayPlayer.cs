using dsstats.shared;

namespace dsstats.db;

public partial class ReplayPlayer
{
    public int ReplayPlayerId { get; set; }
    public string Name { get; set; } = null!;
    public string? Clan { get; set; }
    public int GamePos { get; set; }
    public int Team { get; set; }
    public PlayerResult PlayerResult { get; set; }
    public int Duration { get; set; }
    public Commander Race { get; set; }
    public Commander OppRace { get; set; }
    public int Apm { get; set; }
    public int Income { get; set; }
    public int Army { get; set; }
    public int Kills { get; set; }
    public int UpgradesSpent { get; set; }
    public bool IsUploader { get; set; }
    public bool IsLeaver { get; set; }
    public bool DidNotUpload { get; set; }
    public string TierUpgrades { get; set; } = null!;
    public string Refineries { get; set; } = null!;
    public string? LastSpawnHash { get; set; }
    public int Downloads { get; set; }
    public int Views { get; set; }
    public int ReplayId { get; set; }
    public virtual Replay Replay { get; set; } = null!;
    public int PlayerId { get; set; }
    public virtual Player Player { get; set; } = null!;
    public int? UpgradeId { get; set; }
    public virtual ICollection<PlayerUpgrade> PlayerUpgrades { get; set; } = new List<PlayerUpgrade>();
    public virtual ICollection<Spawn> Spawns { get; set; } = new List<Spawn>();
    public virtual Upgrade? Upgrade { get; set; }
    public virtual RepPlayerRating? ReplayPlayerRatingInfo { get; set; }
    public virtual ComboReplayPlayerRating? ComboReplayPlayerRating { get; set; }
}
