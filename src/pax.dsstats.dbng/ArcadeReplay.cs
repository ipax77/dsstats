using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;
using System.ComponentModel.DataAnnotations;

namespace pax.dsstats.dbng;

public class ArcadeReplay
{
    public ArcadeReplay()
    {
        ArcadeReplayPlayers = new HashSet<ArcadeReplayPlayer>();
    }

    [Key]
    public int ArcadeReplayId { get; set; }
    public int RegionId { get; set; }
    public int Id { get; set; }
    public GameMode GameMode { get; set; }
    [Precision(0)]
    public DateTime CreatedAt { get; set; }
    public int Duration { get; set; }
    public int PlayerCount { get; set; }
    public bool TournamentEdition { get; set; }
    public int WinnerTeam { get; set; }
    public ICollection<ArcadeReplayPlayer> ArcadeReplayPlayers { get; set; }
}

public class ArcadeReplayPlayer
{
    [Key]
    public int ArcadeReplayPlayerId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    public int SlotNumber { get; set; }
    public int Team { get; set; }
    public int Discriminator { get; set; }
    public PlayerResult PlayerResult { get; set; }
    public int ArcadePlayerId { get; set; }
    public ArcadePlayer ArcadePlayer { get; set; } = null!;
    public int ArcadeReplayId { get; set; }
    public ArcadeReplay ArcadeReplay { get; set; } = null!;
}

public class ArcadePlayer
{
    public ArcadePlayer()
    {
        ArcadeReplayPlayers = new HashSet<ArcadeReplayPlayer>();
    }

    [Key]
    public int ArcadePlayerId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public int RealmId { get; set; }
    public int ProfileId { get; set; }
    public ICollection<ArcadeReplayPlayer> ArcadeReplayPlayers { get; set; }
}
