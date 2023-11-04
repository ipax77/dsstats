using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class ArcadePlayerRatingChange
{
    public int ArcadePlayerRatingChangeId { get; set; }

    public float Change24h { get; set; }

    public float Change10d { get; set; }

    public float Change30d { get; set; }

    public int ArcadePlayerRatingId { get; set; }

    public virtual ArcadePlayerRating ArcadePlayerRating { get; set; } = null!;
}
