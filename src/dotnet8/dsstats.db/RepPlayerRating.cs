using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class RepPlayerRating
{
    public int RepPlayerRatingId { get; set; }

    public int GamePos { get; set; }

    public float Rating { get; set; }

    public float RatingChange { get; set; }

    public int Games { get; set; }

    public float Consistency { get; set; }

    public float Confidence { get; set; }

    public int ReplayPlayerId { get; set; }
    public virtual ReplayPlayer? ReplayPlayer { get; set; }
    public int ReplayRatingInfoId { get; set; }
    public virtual ReplayRating? ReplayRatingInfo { get; set; }
}
