using dsstats.shared;
using System.ComponentModel.DataAnnotations;

namespace dsstats.db;

public class ReplayPlayer
{
    public int ReplayPlayerId { get; set; }
    [MaxLength(20)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(10)]
    public string? Clan { get; set; }
    public Commander Race { get; set; }
    public Commander SelectedRace { get; set; }
    public Commander OppRace { get; set; }
    public int TeamId { get; set; }
    public int GamePos { get; set; }
    public int Duration { get; set; }
    public PlayerResult Result { get; set; }
    public int Apm { get; set; }
    public int Messages { get; set; }
    public int Pings { get; set; }
    public int[] TierUpgrades { get; set; } = [];
    public int[] Refineries { get; set; } = [];
    public bool IsMvp { get; set; }
    public bool IsUploader { get; set; }
    public int ReplayId { get; set; }
    public Replay? Replay { get; set; }
    public int PlayerId { get; set; }
    public Player? Player { get; set; }
    public ICollection<Spawn> Spawns { get; set; } = [];
    public ICollection<PlayerUpgrade> Upgrades { get; set; } = [];
    public ICollection<ReplayPlayerRating> Ratings { get; set; } = [];
}
