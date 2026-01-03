using System.ComponentModel.DataAnnotations;

namespace dsstats.db;

public class Player
{
    public int PlayerId { get; set; }
    [MaxLength(20)]
    public string Name { get; set; } = string.Empty;
    public ToonId ToonId { get; set; } = null!;
    public ICollection<ReplayPlayer> ReplayPlayers { get; set; } = [];
    public ICollection<PlayerRating> Ratings { get; set; } = [];
}
