
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using dsstats.shared;

namespace dsstats.db;

public sealed class ReplayPlayer
{
    public int ReplayPlayerId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(50)]
    public string? Clan { get; set; }
    public int GamePos { get; set; }
    public int Team { get; set; }
    public PlayerResult PlayerResult { get; set; }
    public int Duration { get; set; }
    public Commander Race { get; set; }
    public int APM { get; set; }
    public int Income { get; set; }
    public int Army { get; set; }
    public int Kills { get; set; }
    public int UpgradesSpent { get; set; }
    [MaxLength(300)]
    public string Refineries { get; set; } = string.Empty;
    [MaxLength(300)]
    public string TierUpgrades { get; set; } = string.Empty;
    public string? LastSpawnHash { get; set; }
    public bool IsUploader { get; set; }
    public int? OpponentId { get; set; }
    [ForeignKey("OpponentId")]
    public ReplayPlayer? Opponent { get; set; }
    public int PlayerId { get; set; }
    public Player? Player { get; set; }
    public int ReplayId { get; set; }
    public Replay? Replay { get; set; }
    public ICollection<ReplayPlayerRating> ReplayPlayerRatings { get; set; } = [];
    public ICollection<PlayerUpgrade> PlayerUpgrades { get; set; } = [];
    public ICollection<Spawn> Spawns { get; set; } = [];
}

