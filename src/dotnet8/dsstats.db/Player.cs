using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class Player
{
    public Player()
    {
        NoUploadResults = new HashSet<NoUploadResult>();
        ReplayPlayers = new HashSet<ReplayPlayer>();
        PlayerRatings = new HashSet<PlayerRating>();
    }

    public int PlayerId { get; set; }

    public string Name { get; set; } = null!;

    public int ToonId { get; set; }

    public int RegionId { get; set; }

    public int DisconnectCount { get; set; }

    public int RageQuitCount { get; set; }
    public int ArcadeDefeatsSinceLastUpload { get; set; }

    public int NotUploadCount { get; set; }

    public int? UploaderId { get; set; }

    public int RealmId { get; set; }
    public virtual ICollection<NoUploadResult> NoUploadResults { get; set; }

    public virtual ICollection<ReplayPlayer> ReplayPlayers { get; set; }
    public virtual ICollection<PlayerRating> PlayerRatings { get; set; }

    public virtual Uploader? Uploader { get; set; }
}
