using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace dsstats.db;

public partial class ArcadePlayer
{
    public ArcadePlayer()
    {
        ArcadeReplayPlayers = new HashSet<ArcadeReplayPlayer>();
        ArcadePlayerRatings = new HashSet<ArcadePlayerRating>();
    }

    public int ArcadePlayerId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; } = null!;

    public int RegionId { get; set; }

    public int RealmId { get; set; }

    public int ProfileId { get; set; }
    public virtual ICollection<ArcadePlayerRating> ArcadePlayerRatings { get; set; }
    public virtual ICollection<ArcadeReplayPlayer> ArcadeReplayPlayers { get; set; }
}
