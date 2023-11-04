using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class PlayerRatingChange
{
    public int PlayerRatingChangeId { get; set; }

    public float Change24h { get; set; }

    public float Change10d { get; set; }

    public float Change30d { get; set; }

    public int PlayerRatingId { get; set; }

    public virtual PlayerRating PlayerRating { get; set; } = null!;
}
