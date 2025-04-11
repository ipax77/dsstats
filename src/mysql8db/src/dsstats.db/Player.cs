
using System.ComponentModel.DataAnnotations;

namespace dsstats.db;

public sealed class Player
{
    public int PlayerId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; } = null!;
    public int ToonId { get; set; }
    public int RegionId { get; set; }
    public int RealmId { get; set; }
    public int GlobalRating { get; set; } = 1000;
    public ICollection<ReplayPlayer> ReplayPlayers { get; set; } = [];
    public ICollection<PlayerRating> PlayerRatings { get; set; } = [];
}

